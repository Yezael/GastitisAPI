
using Gastitis.Domain.Enums;

namespace Gastitis.Domain.Entities
{
	public class CategoryEntity
	{
		public int Id { get; set; }
		public required string Name { get; set; }
		public SystemCategoryCode? SystemCode { get; set; }
		List<ExpenseEntity>? Expenses { get; set; }
		public int? OwnerUserId { get; set; }
		public UserEntity? OwnerUser { get; set; }
	}
}