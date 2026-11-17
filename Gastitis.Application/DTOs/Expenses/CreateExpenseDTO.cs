namespace Gastitis.Application.DTOs.Expenses
{
    public class CreateExpenseDTO
    {
        public decimal Value { get; set; }
        public required string Description { get; set; }
        public int CategoryId { get; set; }
        public DateTime Date { get; set; }
    }
}
