using System;

namespace Gastitis.Domain.Entities
{
	public class ExpenseEntity
	{
		public int Id { get; set; }
		public decimal Value { get; set; }
		public required string Description { get; set; }

		public int OwnerUserId { get; set; }
		public UserEntity? OwnerUser { get; set; }

		public int CategoryId { get; set; }
		public CategoryEntity? Category { get; set; }
		public DateTime Date { get; set; }
	}
}
