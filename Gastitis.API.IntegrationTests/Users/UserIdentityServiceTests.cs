using Gastitis.API.IntegrationTests.Infrastructure;
using Gastitis.Application.Interfaces.Services;
using Gastitis.Domain.Entities;
using Gastitis.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gastitis.API.IntegrationTests.Users
{
	public sealed class UserIdentityServiceTests : IClassFixture<IntegrationTestFixture>
	{
		private readonly IntegrationTestFixture _fixture;

		public UserIdentityServiceTests(IntegrationTestFixture fixture)
		{
			_fixture = fixture;
		}

		[Fact]
		public async Task GetOrCreateUserIdAsync_WithNewIdentity_PersistsAndReturnsId()
		{
			var testIssuer = TestTokenIssuer.Issuer;
			var uniqueSubject = Guid.NewGuid().ToString();
			int userIdFound;

			await using(var writeScope = _fixture.Factory.Services.CreateAsyncScope())
			{
				var userIdentityService = writeScope.ServiceProvider.GetRequiredService<IUserIdentityService>();
				userIdFound = await userIdentityService.GetOrCreateUserIdAsync(testIssuer, uniqueSubject);
				Assert.True(userIdFound > 0);
			}

			await using var scope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var foundUser = await dbContext.Users
				.AsNoTracking()
				.SingleAsync(u => u.Id == userIdFound);

			Assert.Equal(testIssuer, foundUser.Issuer);
			Assert.Equal(uniqueSubject, foundUser.Subject);
		}

		[Fact]
		public async Task GetOrCreateUserIdAsync_WithExistingIdentity_ReturnsExistingId()
		{
			var testIssuer = TestTokenIssuer.Issuer;
			var uniqueSubject = Guid.NewGuid().ToString();
			int existingUserId;

			await using (var seedScope = _fixture.Factory.Services.CreateAsyncScope())
			{
				var dbContextForSeeding = seedScope.ServiceProvider.GetRequiredService<GastitisDBContext>();
				var newUser = new UserEntity(testIssuer, uniqueSubject);
				dbContextForSeeding.Users.Add(newUser);
				await dbContextForSeeding.SaveChangesAsync();
				existingUserId = newUser.Id;
				Assert.True(existingUserId > 0);
			}

			await using (var serviceCheckScope = _fixture.Factory.Services.CreateAsyncScope())
			{
				var identityService = serviceCheckScope.ServiceProvider.GetRequiredService<IUserIdentityService>();
				var foundExistentId = await identityService.GetOrCreateUserIdAsync(testIssuer, uniqueSubject);
				Assert.Equal(existingUserId, foundExistentId);
			}

			await using var readScope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = readScope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var foundUser = await dbContext.Users
				.AsNoTracking()
				.SingleAsync(u => 
					u.Issuer == testIssuer 
					&& u.Subject == uniqueSubject
				);

			Assert.Equal(existingUserId, foundUser.Id);
		}

		[Fact]
		public async Task GetOrCreateUserIdAsync_WithSameSubjectDifferentIssuers_ReturnsDifferentId()
		{
			var seededIssuer = TestTokenIssuer.Issuer;
			var serviceCallIssuer = "https://issuer.service.test";
			var uniqueSubject = Guid.NewGuid().ToString();
			int seededUserId;
			int serviceUserId;

			await using (var seedScope = _fixture.Factory.Services.CreateAsyncScope())
			{
				var dbContextForSeeding = seedScope.ServiceProvider.GetRequiredService<GastitisDBContext>();
				var newUser = new UserEntity(seededIssuer, uniqueSubject);
				dbContextForSeeding.Users.Add(newUser);
				await dbContextForSeeding.SaveChangesAsync();
				seededUserId = newUser.Id;
				Assert.True(seededUserId > 0);
			}

			await using (var serviceCheckScope = _fixture.Factory.Services.CreateAsyncScope())
			{
				var identityService = serviceCheckScope.ServiceProvider.GetRequiredService<IUserIdentityService>();
				serviceUserId = await identityService.GetOrCreateUserIdAsync(serviceCallIssuer, uniqueSubject);
				Assert.True(serviceUserId > 0);
				Assert.NotEqual(seededUserId, serviceUserId);
			}

			await using var readScope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = readScope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var foundUsers = await dbContext.Users
				.AsNoTracking()
				.Where(u => u.Subject == uniqueSubject)
				.ToListAsync();

			Assert.Equal(2, foundUsers.Count);
			Assert.Single(foundUsers, u => u.Issuer == seededIssuer && u.Id == seededUserId);
			Assert.Single(foundUsers, u => u.Issuer == serviceCallIssuer && u.Id == serviceUserId);
		}

		[Fact]
		public async Task GetOrCreateUserIdAsync_WithConcurrentCalls_ReturnsSameId()
		{
			var issuer = TestTokenIssuer.Issuer;
			var subject = Guid.NewGuid().ToString();

			async Task<int> ResolveUserIdAsync()
			{
				await using var scope = _fixture.Factory.Services.CreateAsyncScope();
				var service = scope.ServiceProvider.GetRequiredService<IUserIdentityService>();
				var result = await service.GetOrCreateUserIdAsync(issuer, subject);
				return result;
			}

			var resolveFirstTask = ResolveUserIdAsync();
			var resolveSecondTask = ResolveUserIdAsync();

			var idsFound = await Task.WhenAll(resolveFirstTask, resolveSecondTask);

			var firstId = idsFound[0];
			var secondId = idsFound[1];

			Assert.True(firstId > 0);
			Assert.Equal(firstId, secondId);

			await using var readScope = _fixture.Factory.Services.CreateAsyncScope();
			var readDbContext = readScope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var foundUser = await readDbContext.Users.AsNoTracking().SingleAsync(u => u.Issuer == issuer && u.Subject == subject);

			Assert.Equal(firstId, foundUser.Id);

		}

	}
}
