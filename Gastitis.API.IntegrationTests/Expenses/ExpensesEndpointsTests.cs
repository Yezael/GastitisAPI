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
