using Gastitis.Domain.Entities;
using Gastitis.Domain.Enums;
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

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<CategoryEntity>
				(
				category =>
				{
					category.Property(c => c.SystemCode)
						.HasConversion<string>();

					category.HasIndex(c => c.SystemCode)
						.IsUnique()
						.HasFilter("\"SystemCode\" IS NOT NULL");

					category.HasData(
						new CategoryEntity
						{
							Id = -1,
							Name = "NONE",
							SystemCode = SystemCategoryCode.None
						},
						new CategoryEntity
						{
							Id = -2,
							Name = "Utilities",
							SystemCode = SystemCategoryCode.Utilities
						},
						new CategoryEntity
						{
							Id = -3,
							Name = "Rent",
							SystemCode = SystemCategoryCode.Rent
						},
						new CategoryEntity
						{
							Id = -4,
							Name = "Transportation",
							SystemCode = SystemCategoryCode.Transportation
						},
						new CategoryEntity
						{
							Id = -5,
							Name = "Groceries",
							SystemCode = SystemCategoryCode.Groceries
						},
						new CategoryEntity
						{
							Id = -6,
							Name = "Subscriptions",
							SystemCode = SystemCategoryCode.Subscriptions
						},
						new CategoryEntity
						{
							Id = -7,
							Name = "Hangouts",
							SystemCode = SystemCategoryCode.Hangouts
						}
					);
				}
				);

			modelBuilder.Entity<ExpenseEntity>
				(
				expense =>
				{
					expense.HasOne(e => e.Category)
						.WithMany()
						.HasForeignKey(e => e.CategoryId)
						.OnDelete(DeleteBehavior.Restrict);
					
				}
				);
		}
	}
}
