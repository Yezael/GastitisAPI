using Gastitis.Application.Interfaces.Services;
using Gastitis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gastitis.Infrastructure.Services
{
	public sealed class UserIdentityService : IUserIdentityService
	{
		private readonly IDbContextFactory<GastitisDBContext> _dbContextFactory;

		public UserIdentityService(IDbContextFactory<GastitisDBContext> dbContextFactory)
		{
			_dbContextFactory = dbContextFactory;
		}

		public async Task<int> GetOrCreateUserIdAsync(string issuer, string subject, CancellationToken cancellationToken = default)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
			ArgumentException.ThrowIfNullOrWhiteSpace(subject);

			await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

			var foundUser = await dbContext.Users.SingleOrDefaultAsync(u => u.Issuer == issuer && u.Subject == subject, cancellationToken);
			if(foundUser != null) return foundUser.Id;

			//Doing it this way to solve concurrent racing request from not resolving correctly.
			await dbContext.Database.ExecuteSqlInterpolatedAsync(
				$"""
				INSERT INTO "Users" ("Issuer", "Subject")
				VALUES ({issuer}, {subject})
				ON CONFLICT ("Issuer", "Subject") DO NOTHING;
				""",
				cancellationToken
				);

			var id = await dbContext.Users.Where(u => u.Issuer == issuer && u.Subject == subject).Select(u => u.Id).SingleAsync(cancellationToken);
			return id;
		}
	}
}
