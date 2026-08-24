using System;

namespace Gastitis.Domain.Entities
{
	public class ExpenseEntity
	{
		public int Id { get; set; }
		public decimal Value { get; set; }
		public string Description { get; set; }
		public int CategoryId { get; set; }
		public CategoryEntity? Category { get; set; }
		public DateTime Date { get; set; }
	}
}
