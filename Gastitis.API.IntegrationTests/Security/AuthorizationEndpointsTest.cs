using Gastitis.API.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;

namespace Gastitis.API.IntegrationTests.Security
{
	public sealed class AuthorizationEndpointsTest : IClassFixture<IntegrationTestFixture>
	{
		private readonly IntegrationTestFixture _fixture;

		public AuthorizationEndpointsTest(IntegrationTestFixture fixture)
		{
			_fixture = fixture;
		}

		[Fact]
		public async Task GetCategories_WithoutAuthentication_ReturnsUnauthorized()
		{
			using var client = _fixture.Factory.CreateClient(
				new WebApplicationFactoryClientOptions
				{
					BaseAddress = new Uri("https://localhost"),
					AllowAutoRedirect = false
				});

			using var response = await client.GetAsync("/api/categories");
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}


		[Theory]
		[InlineData("Wrong scope Test")]
		[InlineData("gastitis.access.extra")]
		[InlineData("prefix.gastitis.access")]
		[InlineData("GASTITIS.ACCESS")]
		public async Task GetCategories_WithoutRequiredScope_ReturnsForbidden(string scope)
		{
			using var request = new HttpRequestMessage(HttpMethod.Get, "/api/categories");
			request.Headers.Add(TestAuthenticationHandler.ScopeHeaderName, scope);

			using var response = await _fixture.Client.SendAsync(request);

			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task GetCategories_WithoutScope_ReturnsForbidden()
		{
			using var request = new HttpRequestMessage(HttpMethod.Get, "/api/categories");
			request.Headers.Add(TestAuthenticationHandler.OmitScopeHeaderName, "true");

			using var response = await _fixture.Client.SendAsync(request);

			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Theory]
		[InlineData("gastitis.access")]
		[InlineData("gastitis.access profile")]
		[InlineData("openid gastitis.access profile")]
		[InlineData("openid profile gastitis.access")]
		public async Task GetCategories_WithRequiredScope_ReturnsOk(string scope)
		{
			using var request = new HttpRequestMessage(HttpMethod.Get, "/api/categories");
			request.Headers.Add(TestAuthenticationHandler.ScopeHeaderName, scope);

			using var response = await _fixture.Client.SendAsync(request);
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		[Fact]
		public async Task GetCategories_WithoutRequiredSub_ReturnsForbidden()
		{
			using var request = new HttpRequestMessage(HttpMethod.Get, "/api/categories");
			request.Headers.Add(TestAuthenticationHandler.OmitSubjectHeaderName, "true");

			using var response = await _fixture.Client.SendAsync(request);

			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}
	}
}
