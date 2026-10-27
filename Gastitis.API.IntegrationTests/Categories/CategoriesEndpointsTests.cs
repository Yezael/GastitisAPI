using Gastitis.API.IntegrationTests.Infrastructure;
using Gastitis.API.Services;
using Gastitis.Application.DTOs;
using Gastitis.Application.Interfaces.Services;
using Gastitis.Domain.Entities;
using Gastitis.Domain.Enums;
using Gastitis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Gastitis.API.IntegrationTests.Categories
{
	public sealed class CategoriesEndpointsTests : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
	{
		private readonly IntegrationTestFixture _fixture;

		public CategoriesEndpointsTests(IntegrationTestFixture fixture)
		{
			_fixture = fixture;
		}

		public async Task InitializeAsync()
		{
			await _fixture.ResetDatabaseAsync();
		}

		[Fact]
		public async Task CreateCategory_WithAuthenticatedUser_ReturnsCreated()
		{
			var subject = "test-unique-subject";

			var createDTO = new CreateCategoryDTO()
			{
				Name = "Test Category"
			};

			using var request = new HttpRequestMessage(HttpMethod.Post, "api/categories")
			{
				Content = JsonContent.Create(createDTO)
			};
			request.Headers.Add(TestAuthenticationHandler.SubjectHeaderName, subject);

			using var response = await _fixture.Client.SendAsync(request);
			Assert.Equal(HttpStatusCode.Created, response.StatusCode);

			var createdCategory = await response.Content.ReadFromJsonAsync<CategoryResponseDTO>();
			Assert.NotNull(createdCategory);

			var createdCategoryId = createdCategory.Id;

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();
			var ownerId = await dbContext.Users.
				Where(u => 
					u.Subject == subject 
					&& u.Issuer == TestAuthenticationHandler.TestIssuer)
				.Select(u => u.Id)
				.SingleAsync();

			var categoryFound = await dbContext.Categories.SingleAsync(c => c.Id == createdCategoryId);
			Assert.Equal(ownerId, categoryFound.OwnerUserId);
			Assert.Null(categoryFound.SystemCode);
		}


		[Fact]
		public async Task DeleteCategory_WhenCategoryDoesNotExist_ReturnsNotFound()
		{
			const int missingCategoryId = int.MaxValue;

			var response = await _fixture.Client.DeleteAsync($"/api/categories/{missingCategoryId}");

			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

			var problemDetail = await response.Content.ReadFromJsonAsync<ProblemDetails>();

			Assert.NotNull(problemDetail);

			Assert.Equal(StatusCodes.Status404NotFound, problemDetail.Status);

			Assert.Equal("Category not found.", problemDetail.Title);

			Assert.Equal($"Category with ID {missingCategoryId} not found.", problemDetail.Detail);
		}

		[Fact]
		public async Task GetCategories_ReturnsSeededSystemCategories()
		{
			var response = await _fixture.Client.GetAsync("/api/categories");

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponseDTO>>();

			Assert.NotNull(categories);

			var expectedNames = new[]
			{
				"NONE",
				"Utilities",
				"Rent",
				"Transportation",
				"Groceries",
				"Subscriptions",
				"Hangouts"
			};

			Assert.Equal(expectedNames.Length, categories.Count);

			foreach (var expectedName in expectedNames)
			{
				Assert.Contains(categories, category => category.Name == expectedName);
			}
		}

		[Theory]
		[InlineData(SystemCategoryCode.None)]
		[InlineData(SystemCategoryCode.Utilities)]
		[InlineData(SystemCategoryCode.Rent)]
		[InlineData(SystemCategoryCode.Transportation)]
		[InlineData(SystemCategoryCode.Groceries)]
		[InlineData(SystemCategoryCode.Subscriptions)]
		[InlineData(SystemCategoryCode.Hangouts)]
		public async Task DeleteCategory_WhenCategoryIsSystemCategory_ReturnConflict(SystemCategoryCode systemCategoryCode)
		{
			await using var scope = _fixture.Factory.Services.CreateAsyncScope();

			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var systemCategory = await dbContext.Categories.AsNoTracking().SingleAsync(category => category.SystemCode == systemCategoryCode);

			var response = await _fixture.Client.DeleteAsync($"/api/categories/{systemCategory.Id}");

			Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

			var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
			Assert.NotNull(problemDetails);

			Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);

			Assert.Equal("System category cannot be deleted.", problemDetails.Title);
			Assert.Equal($"System category with ID {systemCategory.Id} cannot be deleted", problemDetails.Detail);

			var categoryStillExists = await dbContext.Categories.AsNoTracking().AnyAsync(category => category.Id == systemCategory.Id);

			Assert.True(categoryStillExists);
		}

		[Fact]
		public async Task DeleteCategory_WhenUserCategoryHasNoExpenses_ReturnsNoContentAndDeletesCategory()
		{
			int categoryId;
			int currUserId;

			await using (var createTestCategoryScope = _fixture.Factory.Services.CreateAsyncScope())
			{
				var dbContext = createTestCategoryScope.ServiceProvider.GetRequiredService<GastitisDBContext>();
				var userProvider = createTestCategoryScope.ServiceProvider.GetRequiredService<IUserIdentityService>();
				currUserId = await userProvider.GetOrCreateUserIdAsync(TestAuthenticationHandler.TestIssuer, TestAuthenticationHandler.UserSubject);

				var testCategory = new CategoryEntity
				{
					Name = "Test Category",
					SystemCode = null,
					OwnerUserId = currUserId
				};

				dbContext.Categories.Add(testCategory);

				await dbContext.SaveChangesAsync();

				categoryId = testCategory.Id;
			}

			var deleteResponse = await _fixture.Client.DeleteAsync($"/api/categories/{categoryId}");

			Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

			var responseBody = await deleteResponse.Content.ReadAsStringAsync();
			Assert.Empty(responseBody);


			await using var checkDeletedCategoryScope = _fixture.Factory.Services.CreateAsyncScope();

			var dbContextForCheck = checkDeletedCategoryScope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var categoryStillExists = await dbContextForCheck.Categories.AsNoTracking().AnyAsync(x => x.Id == categoryId);
			Assert.False(categoryStillExists);
		}

		[Fact]
		public async Task GetCategory_WithDifferentUserId_ReturnsNotFound()
		{
			int systemCategoryId;
			string customSubject = "other-unique-subject";

			var seedingResult = await SeedCategory();
			var createdCategoryId = seedingResult.categoryId;
			var currentUserId = seedingResult.userId;

			using var otherUserRequest = new HttpRequestMessage(HttpMethod.Get, $"api/categories/{createdCategoryId}");
			otherUserRequest.Headers.Add(TestAuthenticationHandler.SubjectHeaderName, customSubject);

			using var response = await _fixture.Client.SendAsync(otherUserRequest);
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

			using var currentUserRequest = new HttpRequestMessage(HttpMethod.Get, $"api/categories/{createdCategoryId}");
			using var currUserResponse = await _fixture.Client.SendAsync(currentUserRequest);
			Assert.Equal(HttpStatusCode.OK, currUserResponse.StatusCode);

			var retrievedCategoryDTO = await currUserResponse.Content.ReadFromJsonAsync<CategoryResponseDTO>();
			Assert.NotNull(retrievedCategoryDTO);
			Assert.Equal(createdCategoryId, retrievedCategoryDTO.Id);

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();
			systemCategoryId = await dbContext.Categories.Where(c => c.SystemCode == SystemCategoryCode.Utilities).Select(c => c.Id).SingleAsync();

			var foundCategory = await dbContext.Categories.SingleAsync(c => c.Id == createdCategoryId);
			Assert.Null(foundCategory.SystemCode);
			Assert.Equal(currentUserId, foundCategory.OwnerUserId);

			using var otherUserSystemCategoryRequest = new HttpRequestMessage(HttpMethod.Get, $"api/categories/{systemCategoryId}");
			otherUserSystemCategoryRequest.Headers.Add(TestAuthenticationHandler.SubjectHeaderName, customSubject);

			using var otherUserSystemCategoryResponse = await _fixture.Client.SendAsync(otherUserSystemCategoryRequest);
			Assert.Equal(HttpStatusCode.OK, otherUserSystemCategoryResponse.StatusCode);

			var retrievedSystemCategoryDTO = await otherUserSystemCategoryResponse.Content.ReadFromJsonAsync<CategoryResponseDTO>();
			Assert.NotNull(retrievedSystemCategoryDTO);
			Assert.Equal(systemCategoryId, retrievedSystemCategoryDTO.Id);

			var foundSystemCategory = await dbContext.Categories.SingleAsync(c => c.Id == systemCategoryId);
			Assert.NotNull(foundSystemCategory.SystemCode);
			Assert.Null(foundSystemCategory.OwnerUserId);
		}

		[Fact]
		public async Task DeleteCategory_WithIncorrectOwner_ReturnsNotFoundAndPreservesCategoryAndExpenses()
		{
			string customSubject = "other-unique-subject";
			var seedingResult = await SeedCategory();
			var seededCategoryId = seedingResult.categoryId;
			var seededUserId = seedingResult.userId;
			int seededExpenseId;

			await using (var seedExpenseScope = _fixture.Factory.Services.CreateAsyncScope())
			{
				var seedDBContext = seedExpenseScope.ServiceProvider.GetRequiredService<GastitisDBContext>();
				var expense = new ExpenseEntity()
				{
					Value = 10,
					Description = "Test Expense",
					CategoryId = seededCategoryId,
					OwnerUserId = seededUserId,
					Date = new DateTime(1996, 03, 31, 4, 0, 0, DateTimeKind.Utc)
				};
				seedDBContext.Expenses.Add(expense);
				await seedDBContext.SaveChangesAsync();
				seededExpenseId = expense.Id;
			}

			using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/categories/{seededCategoryId}");
			request.Headers.Add(TestAuthenticationHandler.SubjectHeaderName, customSubject);

			using var response = await _fixture.Client.SendAsync(request);
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var categoryFound = await dbContext.Categories.SingleAsync(c => c.Id == seededCategoryId);
			Assert.Equal(seededUserId, categoryFound.OwnerUserId);

			var expenseFound = await dbContext.Expenses.SingleAsync(e => e.Id == seededExpenseId);
			Assert.Equal(seededUserId, expenseFound.OwnerUserId);
			Assert.Equal(seededCategoryId, expenseFound.CategoryId);
		}

		[Fact]
		public async Task GetCategories_WithOtherUserCategoriesExisting_ReturnsOwnedAndSystemOnly()
		{
			string customSubject = "other-unique-subject";

			var seedingResult1 = await SeedCategory();
			var createdCategoryId1 = seedingResult1.categoryId;
			var currentUserId = seedingResult1.userId;

			var seedingResult2 = await SeedCategory();
			var createdCategoryId2 = seedingResult2.categoryId;

			var otherSubjectSeedingResult = await SeedCategory(customSubject);
			var otherSubjectCategory = otherSubjectSeedingResult.categoryId;
			var otherSubjectUserId = otherSubjectSeedingResult.userId;

			using var requestCurrUserCategories = new HttpRequestMessage(HttpMethod.Get, "api/categories");
			using var response = await _fixture.Client.SendAsync(requestCurrUserCategories);

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();
			var retrievedCategories = await response.Content.ReadFromJsonAsync<List<CategoryResponseDTO>>();
			Assert.NotNull(retrievedCategories);

			var systemCategoriesIds = await dbContext.Categories.Where(c => c.SystemCode.HasValue).Select(c => c.Id).ToListAsync();

			var expectedCount = systemCategoriesIds.Count + 2;
			Assert.Equal(expectedCount, retrievedCategories.Count);

			Assert.Contains(retrievedCategories, c => c.Id == createdCategoryId1);
			Assert.Contains(retrievedCategories, c => c.Id == createdCategoryId2);
			Assert.DoesNotContain(retrievedCategories, c => c.Id == otherSubjectCategory);

			for (int i = 0; i < systemCategoriesIds.Count; i++)
			{
				Assert.Contains(retrievedCategories, c => c.Id == systemCategoriesIds[i]);
			}

		}

		private async Task<(int userId, int categoryId)> SeedCategory(string? customSubject = null)
		{
			await using var seedCategoryScope = _fixture.Factory.Services.CreateAsyncScope();
			var seedDBContext = seedCategoryScope.ServiceProvider.GetRequiredService<GastitisDBContext>();
			var userProvider = seedCategoryScope.ServiceProvider.GetRequiredService<IUserIdentityService>();
			var currentUserId = await userProvider.GetOrCreateUserIdAsync(TestAuthenticationHandler.TestIssuer, customSubject?? TestAuthenticationHandler.UserSubject);

			var category = new CategoryEntity()
			{
				Name = "Test Category",
				OwnerUserId = currentUserId
			};
			seedDBContext.Categories.Add(category);
			await seedDBContext.SaveChangesAsync();
			var createdCategoryId = category.Id;

			return (currentUserId, createdCategoryId);
		}

		[Fact]
		public async Task DeleteCategory_WhenUserCategoryHasExpenses_ReturnsNoContentAndReassignsExpensesToNone()
		{
			int testCategoryId;
			int noneCategoryId;
			int unrelatedCategoryId;
			int[] expenseIds;
			int unrelatedExpenseId;


			await using (var createCategoryScope = _fixture.Factory.Services.CreateAsyncScope())
			{
				var dbContext = createCategoryScope.ServiceProvider.GetRequiredService<GastitisDBContext>();

				var currUserService = createCategoryScope.ServiceProvider.GetRequiredService<IUserIdentityService>();
				var currUserId = await currUserService.GetOrCreateUserIdAsync(TestAuthenticationHandler.TestIssuer, TestAuthenticationHandler.UserSubject);
				Assert.NotEqual(0, currUserId);

				noneCategoryId = await dbContext.Categories
					.Where(category => category.SystemCode == SystemCategoryCode.None)
					.Select(category => category.Id)
					.SingleAsync();

				unrelatedCategoryId = await dbContext.Categories
					.Where(category => category.SystemCode == SystemCategoryCode.Groceries)
					.Select(x => x.Id)
					.SingleAsync();

				var testCategory = new CategoryEntity
				{
					Name = "Test Category",
					SystemCode = null,
					OwnerUserId = currUserId
				};

				dbContext.Categories.Add(testCategory);
				await dbContext.SaveChangesAsync();

				testCategoryId = testCategory.Id;

				var firstExpense = new ExpenseEntity
				{
					Value = 10m,
					Description = "First target expense",
					CategoryId = testCategoryId,
					Date = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc),
					OwnerUserId = currUserId
				};

				var secondExpense = new ExpenseEntity
				{
					Value = 20m,
					Description = "Second target expense",
					CategoryId = testCategoryId,
					Date = new DateTime(2026, 9, 28, 13, 0, 0, DateTimeKind.Utc),
					OwnerUserId = currUserId
				};

				var unrelatedExpense = new ExpenseEntity
				{
					Value = 30m,
					Description = "Unrelated expense",
					CategoryId = unrelatedCategoryId,
					Date = new DateTime(2026, 9, 28, 14, 0, 0, DateTimeKind.Utc),
					OwnerUserId = currUserId
				};

				dbContext.Expenses.AddRange(firstExpense, secondExpense, unrelatedExpense);
				await dbContext.SaveChangesAsync();

				expenseIds =
				[
					firstExpense.Id,
					secondExpense.Id
				];

				unrelatedExpenseId = unrelatedExpense.Id;
			}

			var deleteResponse = await _fixture.Client.DeleteAsync($"/api/categories/{testCategoryId}");

			Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

			var content = await deleteResponse.Content.ReadAsStringAsync();
			Assert.Empty(content);

			await using var assertScope = _fixture.Factory.Services.CreateAsyncScope();

			var assertDbContext = assertScope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var categoryStillExists = await assertDbContext.Categories.AsNoTracking().AnyAsync(x => x.Id == testCategoryId);

			var reassignedExpenses = await assertDbContext.Expenses.AsNoTracking()
				.Where(expense => expenseIds.Contains(expense.Id))
				.ToListAsync();

			var persistedUnrelatedExpense = await assertDbContext.Expenses.AsNoTracking().SingleAsync(x => unrelatedExpenseId == x.Id);

			Assert.False(categoryStillExists);

			Assert.Equal(expenseIds.Length, reassignedExpenses.Count);
			Assert.All(
				reassignedExpenses,
				expense => Assert.Equal(noneCategoryId, expense.CategoryId));


			Assert.Equal(unrelatedCategoryId, persistedUnrelatedExpense.CategoryId);
		}

		public Task DisposeAsync()
		{
			return Task.CompletedTask;
		}

	}
}
