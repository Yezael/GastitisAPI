using Gastitis.API.IntegrationTests.Infrastructure;
using Gastitis.Application.Interfaces.Services;
using Gastitis.Domain.Entities;
using Gastitis.Domain.Enums;
using Gastitis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;


namespace Gastitis.API.IntegrationTests.Persistence
{
	public sealed class CategoryPersistenceTests : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
	{
		private readonly IntegrationTestFixture _fixture;

		public CategoryPersistenceTests(IntegrationTestFixture fixture)
		{
			_fixture = fixture;
		}

		public Task DisposeAsync()
		{
			return Task.CompletedTask;
		}

		public async Task InitializeAsync()
		{
			await _fixture.ResetDatabaseAsync();
		}

		[Fact]
		public async Task SaveCategory_WithNoSystemCodeAndNoOwner_ThrowsCheckViolation()
		{
			var invalidCategory = new CategoryEntity()
			{
				Name = "Invalid Category",
				OwnerUserId = null,
				SystemCode = null
			};

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();
			dbContext.Categories.Add(invalidCategory);

			var exception = await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());

			var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
			Assert.Equal(PostgresErrorCodes.CheckViolation, postgresException.SqlState);
			Assert.Equal("CK_Categories_Ownership", postgresException.ConstraintName);
		}

		[Fact]
		public async Task SaveCategory_WithSystemCodeAndOwner_ThrowsCheckViolation()
		{
			await using var scope = _fixture.Factory.Services.CreateAsyncScope();
			var userProviderService = scope.ServiceProvider.GetRequiredService<IUserIdentityService>();
			var currUserId = await userProviderService.GetOrCreateUserIdAsync(TestAuthenticationHandler.TestIssuer, TestAuthenticationHandler.UserSubject);
			
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var systemCategory = await dbContext.Categories.SingleAsync(c => c.SystemCode == SystemCategoryCode.Utilities);
			systemCategory.OwnerUserId = currUserId;

			var exception = await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());

			var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
			Assert.Equal(PostgresErrorCodes.CheckViolation, postgresException.SqlState);
			Assert.Equal("CK_Categories_Ownership", postgresException.ConstraintName);
		}

	}
}
