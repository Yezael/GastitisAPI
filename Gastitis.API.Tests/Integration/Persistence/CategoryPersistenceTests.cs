using Gastitis.API.Tests.Infrastructure;
using Gastitis.Application.Interfaces.Services;
using Gastitis.Domain.Constants;
using Gastitis.Domain.Entities;
using Gastitis.Domain.Enums;
using Gastitis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;


namespace Gastitis.API.Tests.Integration.Persistence
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
			Assert.Equal(DatabaseConstants.CheckCategoryConstraintOwnership, postgresException.ConstraintName);
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
			Assert.Equal(DatabaseConstants.CheckCategoryConstraintOwnership, postgresException.ConstraintName);
		}

		[Theory]
		[InlineData("Duplicate Category")]
		[InlineData("duplicate category")]
		[InlineData("  DUPLICATE Category  ")]
		public async Task SaveCategory_SameNameAndOwner_ThrowsIndexViolation(string duplicateName)
		{
			int userId;
			await using (var seedScope = _fixture.Factory.Services.CreateAsyncScope())
			{
				var userProviderService = seedScope.ServiceProvider.GetRequiredService<IUserIdentityService>();
				userId = await userProviderService.GetOrCreateUserIdAsync(TestAuthenticationHandler.TestIssuer, TestAuthenticationHandler.UserSubject);

				var seedDBContext = seedScope.ServiceProvider.GetRequiredService<GastitisDBContext>();

				var category1 = new CategoryEntity()
				{
					Name = "Duplicate Category",
					OwnerUserId = userId,
					SystemCode = null
				};
				seedDBContext.Categories.Add(category1);
				await seedDBContext.SaveChangesAsync();
			}

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var duplicateCategory = new CategoryEntity()
			{
				Name = duplicateName,
				OwnerUserId = userId,
				SystemCode = null
			};

			dbContext.Categories.Add(duplicateCategory);
			var exception = await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());

			var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
			Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
			Assert.Equal(DatabaseConstants.UniqueCategoryNamePerOwnerIndex, postgresException.ConstraintName);
		}

		[Fact]
		public async Task SaveCategory_SameNameDifferentOwner_Succeeds()
		{
			int userId1;
			int userId2;
			var subject2 = "user2";

			await using (var seedScope = _fixture.Factory.Services.CreateAsyncScope())
			{
				var userProviderService = seedScope.ServiceProvider.GetRequiredService<IUserIdentityService>();
				userId1 = await userProviderService.GetOrCreateUserIdAsync(TestAuthenticationHandler.TestIssuer, TestAuthenticationHandler.UserSubject);
				userId2 = await userProviderService.GetOrCreateUserIdAsync(TestAuthenticationHandler.TestIssuer, subject2);

				var seedDBContext = seedScope.ServiceProvider.GetRequiredService<GastitisDBContext>();

				var category1 = new CategoryEntity()
				{
					Name = "Duplicate Category",
					OwnerUserId = userId1,
					SystemCode = null
				};
				seedDBContext.Categories.Add(category1);
				await seedDBContext.SaveChangesAsync();
			}

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var duplicateCategory = new CategoryEntity()
			{
				Name = "Duplicate Category",
				OwnerUserId = userId2,
				SystemCode = null
			};

			dbContext.Categories.Add(duplicateCategory);
			var rowsChanged = await dbContext.SaveChangesAsync();
			Assert.Equal(1, rowsChanged);

			var savedCategory = await dbContext.Categories.SingleOrDefaultAsync(c => c.Id == duplicateCategory.Id);
			Assert.NotNull(savedCategory);
			Assert.Equal(duplicateCategory.Name, savedCategory.Name);
			Assert.Equal(duplicateCategory.OwnerUserId, savedCategory.OwnerUserId);
		}
	}
}
