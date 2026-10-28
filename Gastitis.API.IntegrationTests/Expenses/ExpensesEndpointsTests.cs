using Gastitis.API.IntegrationTests.Infrastructure;
using Gastitis.Application.DTOs;
using Gastitis.Application.DTOs.Summaries;
using Gastitis.Application.Interfaces.Services;
using Gastitis.Domain.Entities;
using Gastitis.Domain.Enums;
using Gastitis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Gastitis.API.IntegrationTests.Expenses
{
	public sealed class ExpensesEndpointsTests : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
	{
		private readonly IntegrationTestFixture _fixture;
		private string _issuer = "http://test-issuer-unique";
		private string _subject = "test-user-unique";


		public ExpensesEndpointsTests(IntegrationTestFixture fixture)
		{
			_fixture = fixture;
		}

		[Fact]
		public async Task CreateExpense_WithAuthenticatedUser_PersistsCurrentUserAsOwner()
		{
			var categoryId = await GetSystemCategoryId();

			using var request = new HttpRequestMessage(HttpMethod.Post, "api/expenses");
			request.Headers.Add(TestAuthenticationHandler.IssuerHeaderName, _issuer);
			request.Headers.Add(TestAuthenticationHandler.SubjectHeaderName, _subject);

			var createExpenseDTO = new CreateExpenseDTO()
			{
				Value = 500,
				Description = "Test expense",
				CategoryId = categoryId,
				date = new DateTime(2026, 9, 28, 14, 0, 0, DateTimeKind.Utc)
			};
			request.Content = JsonContent.Create(createExpenseDTO);

			using var response = await _fixture.Client.SendAsync(request);
			Assert.Equal(HttpStatusCode.Created, response.StatusCode);

			var expenseDTO = await response.Content.ReadFromJsonAsync<ExpenseResponseDTO>();
			Assert.NotNull(expenseDTO);

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var userFound = await dbContext.Users.AsNoTracking().SingleAsync(u => u.Issuer == _issuer && u.Subject == _subject);

			var ownerIdFound = await dbContext.Expenses.AsNoTracking().Where(e => e.Id == expenseDTO.Id).Select(e => e.OwnerUserId).SingleAsync();
			Assert.True(ownerIdFound > 0);
			Assert.Equal(userFound.Id, ownerIdFound);
		}

		[Fact]
		public async Task GetAll_WithCorrectUser_ReturnsOwnedExpensesOnly()
		{
			var seedingResult = await SeedExpense();
			var seededExpenseId1 = seedingResult.expenseId;
			var seedingResult2 = await SeedExpense();
			var seededExpenseId2 = seedingResult2.expenseId;
			var seedingResult3 = await SeedExpense();
			var seededExpenseId3 = seedingResult3.expenseId;

			await SeedExpense(customSubject: Guid.NewGuid().ToString());

			using var request = new HttpRequestMessage(HttpMethod.Get, "api/expenses?pageSize=3&page=1");
			using var response = await _fixture.Client.SendAsync(request);

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponseDTO<ExpenseResponseDTO>>();
			Assert.NotNull(pagedResponse);

			Assert.Equal(3, pagedResponse.Items.Count);
			Assert.Equal(3, pagedResponse.PageSize);
			Assert.Equal(3, pagedResponse.TotalCount);
			Assert.Equal(1, pagedResponse.Page);
			Assert.Equal(1, pagedResponse.TotalPages);

			Assert.Contains(pagedResponse.Items, e => e.Id == seededExpenseId1);
			Assert.Contains(pagedResponse.Items, e => e.Id == seededExpenseId2);
			Assert.Contains(pagedResponse.Items, e => e.Id == seededExpenseId3);
		}

		[Fact]
		public async Task GetExpensesSummary_WithOtherUsersExpenses_ReturnsOwnedExpensesSummary()
		{
			await SeedExpense(customValue: 10);
			await SeedExpense(customValue: 10);
			await SeedExpense(customValue: 10);

			await SeedExpense(customSubject: Guid.NewGuid().ToString());

			using var request = new HttpRequestMessage(HttpMethod.Get, "api/expenses/summary");
			using var response = await _fixture.Client.SendAsync(request);

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var summaryResponse = await response.Content.ReadFromJsonAsync<ExpensesSummaryResponseDTO>();
			Assert.NotNull(summaryResponse);

			Assert.Equal(30, summaryResponse.TotalAmount);
			Assert.Equal(3, summaryResponse.ExpensesCount);
		}

		[Fact]
		public async Task GetExpensesSummaryByCategory_WithOtherUsersExpenses_ReturnsOwnedCategoryExpensesSummary()
		{
			var seedingResult = await SeedExpense(customValue: 10, customSystemCategory: SystemCategoryCode.Utilities);
			var seededExpenseId1 = seedingResult.expenseId;
			var seedingResult2 = await SeedExpense(customValue: 10, customSystemCategory: SystemCategoryCode.Utilities);
			var seededExpenseId2 = seedingResult2.expenseId;
			var seedingResult3 = await SeedExpense(customValue: 10, customSystemCategory: SystemCategoryCode.Rent);
			var seededExpenseId3 = seedingResult3.expenseId;

			var utilitiesCategoryId = seedingResult.categoryId;
			var rentCategoryId = seedingResult3.categoryId;
			var otherUserSubjectId = Guid.NewGuid().ToString();

			await SeedExpense(customSubject: otherUserSubjectId, customSystemCategory: SystemCategoryCode.Utilities);
			await SeedExpense(customSubject: otherUserSubjectId, customSystemCategory: SystemCategoryCode.Groceries);

			using var request = new HttpRequestMessage(HttpMethod.Get, $"api/expenses/category-summary?page=1&pageSize=2");

			using var response = await _fixture.Client.SendAsync(request);

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponseDTO<ExpensesCategorySummaryResponseDTO>>();
			Assert.NotNull(pagedResponse);

			var expectedCategoriesIds = new[] { utilitiesCategoryId, rentCategoryId }.OrderBy(id => id).ToArray();

			Assert.Equal(expectedCategoriesIds, pagedResponse.Items.Select(e => e.CategoryId).ToArray());

			Assert.Equal(2, pagedResponse.TotalCount);
			Assert.Equal(2, pagedResponse.PageSize);
			Assert.Equal(1, pagedResponse.TotalPages);
			Assert.Equal(1, pagedResponse.Page);

			var utilitiesCategory = pagedResponse.Items.FirstOrDefault(summary => summary.CategoryId == utilitiesCategoryId);
			Assert.NotNull(utilitiesCategory);

			Assert.Equal(20, utilitiesCategory.TotalAmount);
			Assert.Equal(2, utilitiesCategory.ExpenseCount);

			var rentCategory = pagedResponse.Items.FirstOrDefault(summary => summary.CategoryId == rentCategoryId);
			Assert.NotNull(rentCategory);

			Assert.Equal(10, rentCategory.TotalAmount);
			Assert.Equal(1, rentCategory.ExpenseCount);
		}

		private async Task<(int customUserId, int customCategoryId)> SeedCategory(string? customSubjectId = null)
		{
			await using var seedCategoryScope = _fixture.Factory.Services.CreateAsyncScope();

			var userProviderService = seedCategoryScope.ServiceProvider.GetRequiredService<IUserIdentityService>();
			var customUserId = await userProviderService.GetOrCreateUserIdAsync(
				TestAuthenticationHandler.TestIssuer, 
				customSubjectId?? TestAuthenticationHandler.UserSubject
				);

			var customUserCategory = new CategoryEntity()
			{
				Name = "Custom user category",
				OwnerUserId = customUserId
			};

			var seedingDbContext = seedCategoryScope.ServiceProvider.GetRequiredService<GastitisDBContext>();
			seedingDbContext.Categories.Add(customUserCategory);

			await seedingDbContext.SaveChangesAsync();

			var customCategoryId = customUserCategory.Id;

			return (customUserId, customCategoryId);
		}

		private async Task<int> GetSystemCategoryId(SystemCategoryCode? customSystemCategory = null)
		{
			await using var getCategoryScope = _fixture.Factory.Services.CreateAsyncScope();
			var getCategoryDbContext = getCategoryScope.ServiceProvider.GetRequiredService<GastitisDBContext>();
			var selectedsystemCategory = customSystemCategory ?? SystemCategoryCode.Utilities;
			var categoryId = await getCategoryDbContext.Categories.Where(c => c.SystemCode == selectedsystemCategory).Select(c => c.Id).SingleAsync();

			Assert.NotEqual(0, categoryId);
			return categoryId;
		}

		private async Task<(int categoryId, int expenseId)> SeedExpense(
			string? customSubject = null, 
			int? customValue = null,
			string? customDescription = null,
			SystemCategoryCode? customSystemCategory = null)
		{
			var categoryId = await GetSystemCategoryId(customSystemCategory);

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();

			var userIdentityService = scope.ServiceProvider.GetRequiredService<IUserIdentityService>();
			var userId = await userIdentityService.GetOrCreateUserIdAsync(TestAuthenticationHandler.TestIssuer, customSubject?? TestAuthenticationHandler.UserSubject);
			Assert.NotEqual(0, userId);

			var expenseEntity = new ExpenseEntity()
			{
				Value = customValue ?? 600,
				Description = customDescription?? "test",
				CategoryId = categoryId,
				Date = new DateTime(1996, 03, 31, 4, 0, 0, DateTimeKind.Utc),
				OwnerUserId = userId
			};

			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();
			dbContext.Expenses.Add(expenseEntity);
			await dbContext.SaveChangesAsync();

			var expectedExpenseId = expenseEntity.Id;
			return (categoryId, expectedExpenseId);
		}

		[Fact]
		public async Task DeleteExpense_WithCorrectOwner_ReturnsNoContentAndDeletesExpense()
		{
			var seedingResult = await SeedExpense();
			var expectedExpenseId = seedingResult.expenseId;

			using var deleteExpenseRequest = new HttpRequestMessage(HttpMethod.Delete, $"api/expenses/{expectedExpenseId}");
			using var deleteExpenseResponse = await _fixture.Client.SendAsync(deleteExpenseRequest);

			Assert.Equal(HttpStatusCode.NoContent, deleteExpenseResponse.StatusCode);

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();
			var expenseFound = await dbContext.Expenses.SingleOrDefaultAsync(e => e.Id == expectedExpenseId);
			Assert.Null(expenseFound);
		}

		[Fact]
		public async Task DeleteExpense_WithIncorrectOwner_ReturnsNotFoundAndDoesNotDeletesExpense()
		{
			var seedingResult = await SeedExpense();
			var expectedExpenseId = seedingResult.expenseId;

			var otherSubject = Guid.NewGuid().ToString();

			using var deleteExpenseWithIncorrectSubjectRequest = new HttpRequestMessage(HttpMethod.Delete, $"api/expenses/{expectedExpenseId}");
			deleteExpenseWithIncorrectSubjectRequest.Headers.Add(TestAuthenticationHandler.SubjectHeaderName, otherSubject);

			using var deleteExpenseWithIncorrectSubjectResponse = await _fixture.Client.SendAsync(deleteExpenseWithIncorrectSubjectRequest);
			Assert.Equal(HttpStatusCode.NotFound, deleteExpenseWithIncorrectSubjectResponse.StatusCode);

			await using (var scopeForPersistanceCheck = _fixture.Factory.Services.CreateAsyncScope())
			{
				var dbContextPersistanceCheck = scopeForPersistanceCheck.ServiceProvider.GetRequiredService<GastitisDBContext>();
				var expensePersisted = await dbContextPersistanceCheck.Expenses.AnyAsync(e => e.Id == expectedExpenseId);
				Assert.True(expensePersisted);
			}


			using var deleteExpenseWithCorrectSubjectRequest = new HttpRequestMessage(HttpMethod.Delete, $"api/expenses/{expectedExpenseId}");
			using var deleteExpenseWithCorrectSubjectResponse = await _fixture.Client.SendAsync(deleteExpenseWithCorrectSubjectRequest);
			Assert.Equal(HttpStatusCode.NoContent, deleteExpenseWithCorrectSubjectResponse.StatusCode);

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();
			var expensePersisted2 = await dbContext.Expenses.AnyAsync(e => e.Id == expectedExpenseId);
			Assert.False(expensePersisted2);
		}

		[Fact]
		public async Task CreateExpense_WithCurrentUserCategory_ReturnsCreatedAndPersist()
		{
			var seedingCategoryResult = await SeedCategory();
			int customUserId = seedingCategoryResult.customUserId;
			int customCategoryId = seedingCategoryResult.customCategoryId;

			using var request = new HttpRequestMessage(HttpMethod.Post, "api/expenses")
			{
				Content = JsonContent.Create(
					new CreateExpenseDTO()
					{
						Value = 10,
						Description = "test",
						CategoryId = customCategoryId,
						date = new DateTime(1996, 03, 31, 4, 0, 0, 0, DateTimeKind.Utc)
					}
					)
			};

			using var response = await _fixture.Client.SendAsync(request);
			Assert.Equal(HttpStatusCode.Created, response.StatusCode);
			var expenseFromResponse = await response.Content.ReadFromJsonAsync<ExpenseResponseDTO>();
			Assert.NotNull(expenseFromResponse);

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();
			var expenseFromContext = await dbContext.Expenses.SingleAsync(e => e.Id == expenseFromResponse.Id);

			Assert.Equal(customUserId, expenseFromContext.OwnerUserId);
			Assert.Equal(customCategoryId, expenseFromContext.CategoryId);
		}


		[Fact]
		public async Task CreateExpense_WithAnotherUserCategory_ReturnsNotFoundAndDoesNotPersist()
		{
			string customSubjectId = Guid.NewGuid().ToString();
			var seedingCategoryResult = await SeedCategory(customSubjectId);
			int customCategoryId = seedingCategoryResult.customCategoryId;

			using var request = new HttpRequestMessage(HttpMethod.Post, "api/expenses")
			{
				Content = JsonContent.Create(new CreateExpenseDTO()
				{
					Description = "Test Expense",
					Value = 10,
					date = new DateTime(1996, 03, 31, 4, 0, 0, DateTimeKind.Utc),
					CategoryId = customCategoryId
				})
			};

			using var response = await _fixture.Client.SendAsync(request);

			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var expenseExist = await dbContext.Expenses.AnyAsync();
			Assert.False(expenseExist);
		}

		[Fact]
		public async Task UpdateExpense_WithOwnedCustomCategory_ReturnsOkAndPersistsChanges()
		{
			var seedingCategoryResult = await SeedCategory();
			int ownerUserId = seedingCategoryResult.customUserId;
			int ownerUserCategoryId = seedingCategoryResult.customCategoryId;

			var seedingExpenseResult = await SeedExpense(customValue: 10, customDescription: "original Description");
			var seededExpenseId = seedingExpenseResult.expenseId;

			using var request = new HttpRequestMessage(HttpMethod.Put, $"api/expenses/{seededExpenseId}")
			{
				Content = JsonContent.Create(
					new UpdateExpenseDTO()
					{
						Value = 500,
						CategoryId = ownerUserCategoryId,
						Description = "Test change",
					}
					)
			};

			using var response = await _fixture.Client.SendAsync(request);

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var expenseFromResponse = await response.Content.ReadFromJsonAsync<ExpenseResponseDTO>();
			Assert.NotNull(expenseFromResponse);
			Assert.Equal(seededExpenseId, expenseFromResponse.Id);
			Assert.Equal(ownerUserCategoryId, expenseFromResponse.CategoryId);
			Assert.Equal("Test change", expenseFromResponse.Description);
			Assert.Equal(500, expenseFromResponse.Value);

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();

			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();
			var expenseFound = await dbContext.Expenses.AsNoTracking().SingleAsync(e => e.Id == seededExpenseId);
			Assert.Equal(ownerUserCategoryId, expenseFound.CategoryId);
			Assert.Equal(ownerUserId, expenseFound.OwnerUserId);
			Assert.Equal("Test change", expenseFound.Description);
			Assert.Equal(500, expenseFound.Value);
		}


		[Fact]
		public async Task UpdateExpense_WithAnotherUserCategory_ReturnsNotFoundAndPreservesExpense()
		{
			string customSubjectId = Guid.NewGuid().ToString();
			var seedingCategoryResult = await SeedCategory(customSubjectId);
			int customUserCategoryId = seedingCategoryResult.customCategoryId;

			var seedingExpenseResult = await SeedExpense(customValue: 10, customDescription: "original Description");
			var seededExpenseId = seedingExpenseResult.expenseId;
			var seededCategoryId = seedingExpenseResult.categoryId;

			using var request = new HttpRequestMessage(HttpMethod.Put, $"api/expenses/{seededExpenseId}")
			{
				Content = JsonContent.Create(
					new UpdateExpenseDTO()
					{
						Value = 500,
						CategoryId = customUserCategoryId,
						Description = "Test change",
					}
					)
			};

			using var response = await _fixture.Client.SendAsync(request);

			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();

			var userIdentityService = scope.ServiceProvider.GetRequiredService<IUserIdentityService>();
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();
			var userId = await dbContext.Users.AsNoTracking()
				.Where
				(
					u => 
					u.Issuer == TestAuthenticationHandler.TestIssuer 
					&& u.Subject == TestAuthenticationHandler.UserSubject
				)
				.Select(u => u.Id)
				.SingleAsync();

			var expenseFound = await dbContext.Expenses.AsNoTracking().SingleAsync(e => e.Id == seededExpenseId);
			Assert.Equal(seededCategoryId, expenseFound.CategoryId);
			Assert.Equal(userId, expenseFound.OwnerUserId);
			Assert.Equal("original Description", expenseFound.Description);
			Assert.Equal(10, expenseFound.Value);
		}

		[Fact]
		public async Task UpdateExpense_WithIncorrectOwner_ReturnsNotFound()
		{

			var expectedUpdatedCategoryId = await GetSystemCategoryId(SystemCategoryCode.Groceries);
			var expectedUpdatedValue = 500;
			var expectedUpdatedDescription = "Updated Description";

			var seedingResult = await SeedExpense();
			var expectedExpenseId = seedingResult.expenseId;
			var categoryId = seedingResult.categoryId;


			var updateDTO = new UpdateExpenseDTO()
			{
				Value = expectedUpdatedValue,
				Description = expectedUpdatedDescription,
				CategoryId = expectedUpdatedCategoryId
			};

			using var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"api/expenses/{expectedExpenseId}")
			{
				Content = JsonContent.Create(updateDTO)
			};

			using var updateResponse = await _fixture.Client.SendAsync(updateRequest);
			Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

			var updatedExpense = await updateResponse.Content.ReadFromJsonAsync<ExpenseResponseDTO>();
			Assert.NotNull(updatedExpense);

			Assert.Equal(expectedExpenseId, updatedExpense.Id);
			Assert.Equal(expectedUpdatedDescription, updatedExpense.Description);
			Assert.Equal(expectedUpdatedValue, updatedExpense.Value);
			Assert.Equal(expectedUpdatedCategoryId, updatedExpense.CategoryId);


			var updateWithWrongUserDTO = new UpdateExpenseDTO()
			{
				Value = 300,
				Description = "Should not Update description",
				CategoryId = categoryId
			};
			using var updateWithIncorrectUserRequest = new HttpRequestMessage(HttpMethod.Put, $"api/expenses/{expectedExpenseId}")
			{
				Content = JsonContent.Create(updateWithWrongUserDTO)
			};

			updateWithIncorrectUserRequest.Headers.Add(TestAuthenticationHandler.SubjectHeaderName, Guid.NewGuid().ToString());

			using var updateWithIncorrectUserResponse = await _fixture.Client.SendAsync(updateWithIncorrectUserRequest);
			Assert.Equal(HttpStatusCode.NotFound, updateWithIncorrectUserResponse.StatusCode);

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();
			var finalExpenseData = await dbContext.Expenses.SingleAsync(e => e.Id == expectedExpenseId);
			var expectedUserId = await dbContext.Users.Where(
				u => u.Issuer == TestAuthenticationHandler.TestIssuer 
				&& u.Subject == TestAuthenticationHandler.UserSubject).Select(u => u.Id).SingleAsync();

			Assert.Equal(expectedExpenseId, finalExpenseData.Id);
			Assert.Equal(expectedUserId, finalExpenseData.OwnerUserId);
			Assert.Equal(expectedUpdatedDescription, finalExpenseData.Description);
			Assert.Equal(expectedUpdatedValue, finalExpenseData.Value);
			Assert.Equal(expectedUpdatedCategoryId, finalExpenseData.CategoryId);

		}

		[Fact]
		public async Task GetExpense_WithIncorrectOwner_ReturnsNotFound()
		{
			var seedingResult = await SeedExpense();
			var expectedExpenseId = seedingResult.expenseId;

			using var getWithCorrectSubjectRequest = new HttpRequestMessage(HttpMethod.Get, $"api/expenses/{expectedExpenseId}");
			using var getResponse = await _fixture.Client.SendAsync(getWithCorrectSubjectRequest);

			Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
			var expenseResponseWithCorrectUser = await getResponse.Content.ReadFromJsonAsync<ExpenseResponseDTO>();
			Assert.NotNull(expenseResponseWithCorrectUser);

			var expenseIdFoundWithCorrectUser = expenseResponseWithCorrectUser.Id;
			Assert.Equal(expectedExpenseId, expenseIdFoundWithCorrectUser);


			var otherSubject = Guid.NewGuid().ToString();

			using var getWithOtherRequest = new HttpRequestMessage(HttpMethod.Get, $"api/expenses/{expectedExpenseId}");
			getWithOtherRequest.Headers.Add(TestAuthenticationHandler.SubjectHeaderName, otherSubject);
			using var getWithOtherResponse = await _fixture.Client.SendAsync(getWithOtherRequest);

			Assert.Equal(HttpStatusCode.NotFound, getWithOtherResponse.StatusCode);
		}

		public Task DisposeAsync()
		{
			return Task.CompletedTask;
		}

		public async Task InitializeAsync()
		{
			await _fixture.ResetDatabaseAsync();
		}
	}
}
