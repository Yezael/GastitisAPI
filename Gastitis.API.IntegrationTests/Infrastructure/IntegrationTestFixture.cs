using Gastitis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using System.Net.Http.Headers;

namespace Gastitis.API.IntegrationTests.Infrastructure
{
	public sealed class IntegrationTestFixture : IAsyncLifetime
	{
		private readonly PostgreSqlContainer _databaseContainer =
			new PostgreSqlBuilder("postgres:18-alpine")
			.WithDatabase("gastitis_integration_tests")
			.WithUsername("postgres")
			.WithPassword("postgres")
			.Build();

		public GastitisApiFactory Factory { get; private set; } = null!;
		public HttpClient Client { get; private set; } = null!;

		public async Task InitializeAsync()
		{
			await _databaseContainer.StartAsync();

			Factory = new GastitisApiFactory(_databaseContainer.GetConnectionString());

			await using var scope = Factory.Services.CreateAsyncScope();

			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			await dbContext.Database.MigrateAsync();

			Client = Factory.CreateClient(
				new WebApplicationFactoryClientOptions
				{
					BaseAddress = new Uri("https://localhost")
				});

			Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthenticationHandler.SchemeName);
		}

		public async Task ResetDatabaseAsync()
		{
			await using var scope = Factory.Services.CreateAsyncScope();

			var dbContext = scope.ServiceProvider.GetRequiredService<GastitisDBContext>();

			await using var transaction = await dbContext.Database.BeginTransactionAsync();

			await dbContext.Expenses.ExecuteDeleteAsync();
			await dbContext.Users.ExecuteDeleteAsync();
			await dbContext.Categories.Where(category => !category.SystemCode.HasValue).ExecuteDeleteAsync();

			await transaction.CommitAsync();
		}

		public async Task DisposeAsync()
		{
			Client.Dispose();
			Factory.Dispose();

			await _databaseContainer.DisposeAsync();
		}
	}
}
