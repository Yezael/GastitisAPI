using Gastitis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;

namespace Gastitis.API.IntegrationTests.Infrastructure
{
	public sealed class GastitisApiFactory : WebApplicationFactory<Program>
	{
		private readonly string _connectionString;

		public GastitisApiFactory(string connectionString)
		{
			_connectionString = connectionString;
		}

		protected override IHost CreateHost(IHostBuilder builder)
		{
			builder.ConfigureHostConfiguration(configuration =>
			{
				configuration.AddInMemoryCollection(
					new Dictionary<string,string?>
					{
						["Authentication:Authority"] = "https://identity.example.test",
						["Authentication:Audience"] = "gastitis-api"
					}
					);
			});

			return base.CreateHost(builder);
		}

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			builder.UseEnvironment("Testing");
			builder.ConfigureServices(
				services =>
				{
					services.RemoveAll<GastitisDBContext>();
					services.RemoveAll<DbContextOptions<GastitisDBContext>>();

					services.AddDbContext<GastitisDBContext>(
						options =>
						{
						options.UseNpgsql(_connectionString);
						}
						);
				}
				);

			builder.ConfigureTestServices(services =>
			{
				services.AddAuthentication(options =>
				{
					options.DefaultScheme = TestAuthenticationHandler.SchemeName;
					options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
					options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
					options.DefaultForbidScheme = TestAuthenticationHandler.SchemeName;
				})
				.AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, options => { });
			});
		}
	}
}
