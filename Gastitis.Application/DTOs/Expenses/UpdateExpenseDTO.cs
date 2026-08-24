namespace Gastitis.Application.DTOs
{
    public class UpdateExpenseDTO
    {
        public decimal Value { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
    }
}
