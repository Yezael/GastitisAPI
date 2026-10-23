using Gastitis.API.IntegrationTests.Infrastructure;
using Gastitis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;


namespace Gastitis.API.IntegrationTests.Users
{
	public sealed class CurrentUserEndpointsTests : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
	{
		private readonly IntegrationTestFixture _fixture;
		private string _issuer = "http://test-issuer-unique";
		private string _subject = "test-user-unique";

		public CurrentUserEndpointsTests(IntegrationTestFixture fixture)
		{
			_fixture = fixture;
		}

		public async Task InitializeAsync()
		{
			await _fixture.ResetDatabaseAsync();
		}

		[Fact]
		public async Task GetCurrentUser_WithoutRequiredScope_ReturnsForbiddenAndNoProvisioning()
		{
			using var result = await SendGetCurrentUserRequest(omitScope: true);

			Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);

			await using var newScope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = newScope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var userExists = await dbContext.Users.AnyAsync(u => u.Issuer == _issuer && u.Subject == _subject);
			Assert.False(userExists);
		}

		[Fact]
		public async Task GetCurrentUser_WithNewIdentity_PersistsAndReturnsUserId()
		{
			using var result = await SendGetCurrentUserRequest();

			Assert.Equal(HttpStatusCode.OK, result.StatusCode);
			var expectedId = await result.Content.ReadFromJsonAsync<int>();
			Assert.True(expectedId > 0);

			await using var newScope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = newScope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var foundUser = await dbContext.Users.AsNoTracking().SingleAsync(u => u.Issuer == _issuer && u.Subject == _subject);
			Assert.Equal(foundUser.Id, expectedId);
		}

		[Fact]
		public async Task GetCurrentUser_WithRepeatedIdentity_ReturnsSameUserId()
		{
			using var result1 = await SendGetCurrentUserRequest();
			using var result2 = await SendGetCurrentUserRequest();

			Assert.Equal(HttpStatusCode.OK, result1.StatusCode);
			Assert.Equal(HttpStatusCode.OK, result2.StatusCode);

			var firstIdFound = await result1.Content.ReadFromJsonAsync<int>();
			var secondIdFound = await result2.Content.ReadFromJsonAsync<int>();
			Assert.True(firstIdFound > 0);
			Assert.Equal(firstIdFound, secondIdFound);

			await using var newScope = _fixture.Factory.Services.CreateAsyncScope();
			var dbContext = newScope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			var foundUser = await dbContext.Users.AsNoTracking().SingleAsync(u => u.Issuer == _issuer && u.Subject == _subject);
			Assert.Equal(foundUser.Id, secondIdFound);
		}

		private async Task<HttpResponseMessage> SendGetCurrentUserRequest(bool omitScope = false)
		{
			return await _fixture.SendGetCurrentUserRequest(_issuer, _subject, omitScope);
		}

		public Task DisposeAsync()
		{
			return Task.CompletedTask;
		}
	}
}
