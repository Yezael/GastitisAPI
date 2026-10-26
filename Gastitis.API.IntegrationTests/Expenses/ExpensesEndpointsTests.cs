using Gastitis.API.IntegrationTests.Infrastructure;
using Gastitis.Application.DTOs;
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
			var categoryId = 0;

			await using (var getCategoryScope = _fixture.Factory.Services.CreateAsyncScope())
			{
				var getCategoryDBContext = getCategoryScope.ServiceProvider.GetRequiredService<GastitisDBContext>();
				categoryId = await getCategoryDBContext.Categories.Where(c => c.SystemCode == SystemCategoryCode.Utilities).Select(c => c.Id).SingleAsync();
				Assert.True(categoryId != 0);
			}

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
			(int categoryId, int expenseId) seedingResult = await SeedExpense();
			var expectedExpenseId = seedingResult.expenseId;


		}

		/// <summary> </summary>
		/// <returns>category Id, Seeded Expenses Id</returns>
		private async Task<(int, int)> SeedExpense()
		{
			var categoryId = 0;
			await using (var getCategoryScope = _fixture.Factory.Services.CreateAsyncScope())
			{
				var getCategoryDbContext = getCategoryScope.ServiceProvider.GetRequiredService<GastitisDBContext>();
				categoryId = await getCategoryDbContext.Categories.Where(c => c.SystemCode == SystemCategoryCode.Utilities).Select(c => c.Id).SingleAsync();
				Assert.NotEqual(0, categoryId);
			}

			var createExpenseDTO = new CreateExpenseDTO()
			{
				Value = 600,
				Description = "test",
				CategoryId = categoryId,
				date = new DateTime(1996, 03, 31, 4, 0, 0, DateTimeKind.Utc)
			};

			using var createExpenseRequest = new HttpRequestMessage(HttpMethod.Post, "api/expenses")
			{
				Content = JsonContent.Create(createExpenseDTO)
			};

			using var createExpenseResponse = await _fixture.Client.SendAsync(createExpenseRequest);
			Assert.Equal(HttpStatusCode.Created, createExpenseResponse.StatusCode);

			var createdExpense = await createExpenseResponse.Content.ReadFromJsonAsync<ExpenseResponseDTO>();
			Assert.NotNull(createdExpense);
			var expectedExpenseId = createdExpense.Id;

			return (categoryId, expectedExpenseId);
		}

		[Fact]
		public async Task DeleteExpense_WithCorrectOwner_ReturnsNoContentAndDeletesExpense()
		{
			(int categoryId, int expenseId) seedingResult = await SeedExpense();
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
			(int categoryId, int expenseId) seedingResult = await SeedExpense();
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
		public async Task UpdateExpense_WithIncorrectOwner_ReturnsNotFound()
		{

			var expectedUpdatedCategoryId = 0;
			var expectedUpdatedValue = 500;
			var expectedUpdatedDescription = "Updated Description";

			await using (var getCategoryScope = _fixture.Factory.Services.CreateAsyncScope())
			{
				var getCategoryDBContext = getCategoryScope.ServiceProvider.GetRequiredService<GastitisDBContext>();
				expectedUpdatedCategoryId = await getCategoryDBContext.Categories.Where(c => c.SystemCode == SystemCategoryCode.Groceries).Select(c => c.Id).SingleAsync();
				Assert.NotEqual(0, expectedUpdatedCategoryId);
			}

			(int categoryId, int expenseId) seedingResult = await SeedExpense();
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
			(int categoryId, int expenseId) seedingResult = await SeedExpense();
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
