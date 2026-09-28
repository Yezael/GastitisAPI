using Gastitis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gastitis.API.IntegrationTests.Infrastructure
{
	public sealed class GastitisApiFactory : WebApplicationFactory<Program>
	{
		private readonly string _connectionString;

		public GastitisApiFactory(string connectionString)
		{
			_connectionString = connectionString;
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
		}
	}
}
