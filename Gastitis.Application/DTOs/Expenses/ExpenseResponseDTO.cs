namespace Gastitis.Application.DTOs
{
    public class ExpenseResponseDTO
    {
        public int Id { get; set; }

        public decimal Value { get; set; }

        public string Description { get; set; }

        public int CategoryId { get; set; }

        public DateTime Date { get; set; }
    }
}
