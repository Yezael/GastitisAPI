
namespace Gastitis.Domain.Entities
{
	public class CategoryEntity
	{
		public int Id { get; set; }
		public string Name { get; set; }
		List<ExpenseEntity>? Expenses { get; set; }
	}
}