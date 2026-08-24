using Gastitis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gastitis.Infrastructure.Persistence
{
	public class GastitisDBContext : DbContext
	{
		public GastitisDBContext(DbContextOptions<GastitisDBContext> options) : base(options)
		{

		}

		public DbSet<ExpenseEntity> Expenses { get; set; }
		public DbSet<CategoryEntity> Categories { get; set; }
	}
}
