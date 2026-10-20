using Gastitis.API.IntegrationTests.Infrastructure;
using Gastitis.Domain.Entities;
using Gastitis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;


namespace Gastitis.API.IntegrationTests.Persistence
{
	public sealed class UserPersistenceTests : IClassFixture<IntegrationTestFixture>
	{
		private readonly IntegrationTestFixture _fixture;

		public UserPersistenceTests(IntegrationTestFixture fixture)
		{
			_fixture = fixture;
		}

		[Fact]
		public async Task SaveUser_PersistsIdentityAndGeneratesId()
		{
			const string issuer = "https://identity.test.issuer";
			var subject = Guid.NewGuid().ToString("N");
			int userId;

			await using (var writeScope = _fixture.Factory.Services.CreateAsyncScope())
			{
				var dbContext = writeScope.ServiceProvider.GetRequiredService<GastitisDBContext>();

				var user = new UserEntity(issuer, subject);

				dbContext.Users.Add(user);
				await dbContext.SaveChangesAsync();

				userId = user.Id;
				Assert.True(userId > 0);
			}

			await using var readScope = _fixture.Factory.Services.CreateAsyncScope();
			var readDBContext = readScope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var persistedUser = await readDBContext.Users.AsNoTracking().SingleAsync(u => u.Id == userId);

			Assert.Equal(issuer, persistedUser.Issuer);
			Assert.Equal(subject, persistedUser.Subject);
		}


		[Fact]
		public async Task SaveUser_WithDuplicateIdentity_ThrowsUniqueViolation()
		{
			const string issuer = "https://identity.test.issuer";
			var subject = Guid.NewGuid().ToString("N");

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var user1 = new UserEntity(issuer, subject);
			dbContext.Users.Add(user1);
			await dbContext.SaveChangesAsync();

			var user2 = new UserEntity(issuer, subject);
			dbContext.Users.Add(user2);

			var exception = await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());

			var postgresException = Assert.IsType<PostgresException>(exception.InnerException);

			Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
			Assert.Equal("IX_Users_Issuer_Subject", postgresException.ConstraintName);
		}

		[Fact]
		public async Task SaveUser_PersistsUsersSameSubWithDifferentIss()
		{
			const string issuer1 = "https://identity.test.issuer";
			const string issuer2 = "https://identity.test2.issuer";
			var subject = Guid.NewGuid().ToString("N");

			await using(var writeScope = _fixture.Factory.Services.CreateAsyncScope())
			{
				var writeDbContext = writeScope.ServiceProvider.GetRequiredService<GastitisDBContext>();

				var user1 = new UserEntity(issuer1, subject);
				var user2 = new UserEntity(issuer2, subject);
				writeDbContext.Users.Add(user1);
				writeDbContext.Users.Add(user2);
				await writeDbContext.SaveChangesAsync();
			}

			await using var readScope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = readScope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var users = await dbContext.Users.AsNoTracking().Where(u => u.Subject == subject).ToListAsync();

			Assert.Equal(2, users.Count);
			Assert.Contains(users, user => user.Issuer == issuer1);
			Assert.Contains(users, user => user.Issuer == issuer2);
			Assert.NotEqual(users[0].Id, users[1].Id);
		}

	}
}
