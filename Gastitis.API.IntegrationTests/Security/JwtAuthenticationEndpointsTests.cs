using Gastitis.API.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;


namespace Gastitis.API.IntegrationTests.Security
{
	public sealed class JwtAuthenticationEndpointsTests : IClassFixture<IntegrationTestFixture>
	{
		private readonly IntegrationTestFixture _fixture;

		public JwtAuthenticationEndpointsTests(IntegrationTestFixture fixture)
		{
			_fixture = fixture;
		}

		[Fact]
		public async Task GetCategories_WithoutToken_ReturnsBearerChallenge()
		{
			using var factory = CreateFactoryWithRealAuthentication(null);

			using var client = factory.CreateClient(
				new WebApplicationFactoryClientOptions
				{
					BaseAddress = new Uri("https://localhost"),
					AllowAutoRedirect = false
				});

			using var response = await client.GetAsync("/api/categories");

			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

			var challenge = Assert.Single(response.Headers.WwwAuthenticate);

			Assert.Equal(JwtBearerDefaults.AuthenticationScheme, challenge.Scheme);

			Assert.Null(challenge.Parameter);
		}

		[Fact]
		public async Task GetCategories_WithValidToken_ReturnsOk()
		{
			using var tokenIssuer = new TestTokenIssuer();

			var config = new OpenIdConnectConfiguration
			{
				Issuer = TestTokenIssuer.Issuer
			};
			config.SigningKeys.Add(tokenIssuer.PublicKey);

			void AddIssuerConfiguration(IServiceCollection services)
			{
				services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
					options =>
					{
						options.Configuration = config;
					});
			}

			using var factory = CreateFactoryWithRealAuthentication(AddIssuerConfiguration);

			using var client = factory.CreateClient(
				new WebApplicationFactoryClientOptions()
				{
					BaseAddress = new Uri("https://localhost"),
					AllowAutoRedirect = false
				});

			using var request = new HttpRequestMessage(HttpMethod.Get, "api/categories");
			request.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, tokenIssuer.CreateAccessToken());

			using var response = await client.SendAsync(request);

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		[Fact]
		public async Task GetCategories_WithInvalidAudience_ReturnsUnauthorized()
		{
			using var testTokenIssuer = new TestTokenIssuer();

			var config = new OpenIdConnectConfiguration
			{
				Issuer = TestTokenIssuer.Issuer
			};
			config.SigningKeys.Add(testTokenIssuer.PublicKey);

			void AddIssuerConfiguration(IServiceCollection services)
			{
				services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
					options =>
					{
						options.Configuration = config;
					});
			}

			using var factory = CreateFactoryWithRealAuthentication(AddIssuerConfiguration);

			using var client = factory.CreateClient(
				new WebApplicationFactoryClientOptions()
				{
					BaseAddress = new Uri("https://localhost"),
					AllowAutoRedirect = false
				});

			using var request = new HttpRequestMessage(HttpMethod.Get, "api/categories");
			request.Headers.Authorization = new AuthenticationHeaderValue(
				JwtBearerDefaults.AuthenticationScheme, 
				testTokenIssuer.CreateAccessToken("test-other-audience"));

			using var response = await client.SendAsync(request);

			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

			var challenge = Assert.Single(response.Headers.WwwAuthenticate);
			Assert.Equal(JwtBearerDefaults.AuthenticationScheme, challenge.Scheme);

			Assert.Null(challenge.Parameter);
		}

		[Fact]
		public async Task GetCategories_WithInvalidIssuer_ReturnsUnauthorized()
		{
			using var testTokenIssuer = new TestTokenIssuer();

			var config = new OpenIdConnectConfiguration
			{
				Issuer = TestTokenIssuer.Issuer
			};
			config.SigningKeys.Add(testTokenIssuer.PublicKey);

			void AddIssuerConfiguration(IServiceCollection services)
			{
				services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
					options =>
					{
						options.Configuration = config;
					});
			}

			using var factory = CreateFactoryWithRealAuthentication(AddIssuerConfiguration);

			using var client = factory.CreateClient(
				new WebApplicationFactoryClientOptions()
				{
					BaseAddress = new Uri("https://localhost"),
					AllowAutoRedirect = false
				});

			using var request = new HttpRequestMessage(HttpMethod.Get, "api/categories");
			request.Headers.Authorization = new AuthenticationHeaderValue(
				JwtBearerDefaults.AuthenticationScheme, 
				testTokenIssuer.CreateAccessToken(issuer: "https://wrong.issuer.test"));

			using var response = await client.SendAsync(request);

			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

			var challenge = Assert.Single(response.Headers.WwwAuthenticate);
			Assert.Equal(JwtBearerDefaults.AuthenticationScheme, challenge.Scheme);

			Assert.Null(challenge.Parameter);
		}

		[Fact]
		public async Task GetCategories_WithExpiredToken_ReturnsUnauthorized()
		{
			using var testTokenIssuer = new TestTokenIssuer();

			var config = new OpenIdConnectConfiguration
			{
				Issuer = TestTokenIssuer.Issuer
			};
			config.SigningKeys.Add(testTokenIssuer.PublicKey);

			void AddIssuerConfiguration(IServiceCollection services)
			{
				services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
					options =>
					{
						options.Configuration = config;
					});
			}

			using var factory = CreateFactoryWithRealAuthentication(AddIssuerConfiguration);

			using var client = factory.CreateClient(
				new WebApplicationFactoryClientOptions()
				{
					BaseAddress = new Uri("https://localhost"),
					AllowAutoRedirect = false
				});

			using var request = new HttpRequestMessage(HttpMethod.Get, "api/categories");
			request.Headers.Authorization = new AuthenticationHeaderValue(
				JwtBearerDefaults.AuthenticationScheme,
				testTokenIssuer.CreateAccessToken(issuedAt: DateTime.UtcNow.AddMinutes(-10)));

			using var response = await client.SendAsync(request);

			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

			var challenge = Assert.Single(response.Headers.WwwAuthenticate);
			Assert.Equal(JwtBearerDefaults.AuthenticationScheme, challenge.Scheme);

			Assert.Null(challenge.Parameter);
		}

		[Fact]
		public async Task GetCategories_WithNotYetValidToken_ReturnsUnauthorized()
		{
			using var testTokenIssuer = new TestTokenIssuer();

			var config = new OpenIdConnectConfiguration
			{
				Issuer = TestTokenIssuer.Issuer
			};
			config.SigningKeys.Add(testTokenIssuer.PublicKey);

			void AddIssuerConfiguration(IServiceCollection services)
			{
				services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
					options =>
					{
						options.Configuration = config;
					});
			}

			using var factory = CreateFactoryWithRealAuthentication(AddIssuerConfiguration);

			using var client = factory.CreateClient(
				new WebApplicationFactoryClientOptions()
				{
					BaseAddress = new Uri("https://localhost"),
					AllowAutoRedirect = false
				});

			using var request = new HttpRequestMessage(HttpMethod.Get, "api/categories");
			request.Headers.Authorization = new AuthenticationHeaderValue(
				JwtBearerDefaults.AuthenticationScheme,
				testTokenIssuer.CreateAccessToken(notBefore: DateTime.UtcNow.AddMinutes(10)));

			using var response = await client.SendAsync(request);

			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

			var challenge = Assert.Single(response.Headers.WwwAuthenticate);
			Assert.Equal(JwtBearerDefaults.AuthenticationScheme, challenge.Scheme);

			Assert.Null(challenge.Parameter);
		}

		[Fact]
		public async Task GetCategories_WithoutRequiredScope_ReturnsForbidden()
		{
			using var testTokenIssuer = new TestTokenIssuer();

			var config = new OpenIdConnectConfiguration
			{
				Issuer = TestTokenIssuer.Issuer
			};
			config.SigningKeys.Add(testTokenIssuer.PublicKey);

			void AddIssuerConfiguration(IServiceCollection services)
			{
				services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
					options =>
					{
						options.Configuration = config;
					});
			}

			using var factory = CreateFactoryWithRealAuthentication(AddIssuerConfiguration);

			using var client = factory.CreateClient(
				new WebApplicationFactoryClientOptions()
				{
					BaseAddress = new Uri("https://localhost"),
					AllowAutoRedirect = false
				});

			using var request = new HttpRequestMessage(HttpMethod.Get, "api/categories");
			request.Headers.Authorization = new AuthenticationHeaderValue(
				JwtBearerDefaults.AuthenticationScheme,
				testTokenIssuer.CreateAccessToken(scope: "test.wrong.scope"));

			using var response = await client.SendAsync(request);

			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
			Assert.Empty(response.Headers.WwwAuthenticate);
		}

		[Fact]
		public async Task GetCategories_WithUntrustedSigningKey_ReturnsUnauthorized()
		{
			using var trustedIssuer = new TestTokenIssuer();
			using var untrustedIssuer = new TestTokenIssuer();

			var config = new OpenIdConnectConfiguration
			{
				Issuer = TestTokenIssuer.Issuer
			};
			config.SigningKeys.Add(trustedIssuer.PublicKey);

			void AddIssuerConfiguration(IServiceCollection services)
			{
				services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
					options =>
					{
						options.Configuration = config;
					});
			}

			using var factory = CreateFactoryWithRealAuthentication(AddIssuerConfiguration);

			using var client = factory.CreateClient(
				new WebApplicationFactoryClientOptions()
				{
					BaseAddress = new Uri("https://localhost"),
					AllowAutoRedirect = false
				});


			using var request = new HttpRequestMessage(HttpMethod.Get, "api/categories");
			request.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, untrustedIssuer.CreateAccessToken());

			using var response = await client.SendAsync(request);

			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

			var challenge = Assert.Single(response.Headers.WwwAuthenticate);
			Assert.Equal(JwtBearerDefaults.AuthenticationScheme, challenge.Scheme);

			Assert.Null(challenge.Parameter);
		}

		private WebApplicationFactory<Program> CreateFactoryWithRealAuthentication(Action<IServiceCollection>? furtherServiceConfigurations)
		{
			var factory = _fixture.Factory.WithWebHostBuilder(builder =>
			{
				builder.ConfigureTestServices(services =>
				{
					services.AddAuthentication(options =>
					{
						options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
						options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
						options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
						options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
					});
					furtherServiceConfigurations?.Invoke(services);
				});
			});

			return factory;
		}
	}
}
