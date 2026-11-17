using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Time.Testing;

namespace Gastitis.API.Tests.Infrastructure
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
						["Authentication:Audience"] = "gastitis-api",
						["ConnectionStrings:GastitisDatabase"] = _connectionString
					}
					);
			});

			return base.CreateHost(builder);
		}

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			builder.UseEnvironment("Testing");
			builder.ConfigureTestServices(services =>
			{
				services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)));
				services.AddAuthentication(options =>
				{
					options.DefaultScheme = TestAuthenticationHandler.SchemeName;
					options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
					options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
					options.DefaultForbidScheme = TestAuthenticationHandler.SchemeName;
				})
				.AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, options => { });

				//Forcing the test controllers discovery to find all controllers in the given assembly, needed to be done this way, since the 
				//real host doesn't discover controllers in the test assembly (which is correct)
				services.AddControllers().AddApplicationPart(typeof(CurrentUserProbeController).Assembly);
			});
		}
	}
}
