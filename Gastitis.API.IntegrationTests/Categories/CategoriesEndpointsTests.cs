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

			await using (var createTestCategoryScope = _fixture.Factory.Services.CreateAsyncScope())
			{
				var dbContext = createTestCategoryScope.ServiceProvider.GetRequiredService<GastitisDBContext>();

				var testCategory = new CategoryEntity
				{
					Name = "Test Category",
					SystemCode = null
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
					SystemCode = null
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
