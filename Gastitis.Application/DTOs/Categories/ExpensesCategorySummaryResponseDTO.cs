namespace Gastitis.Application.DTOs
{
    public class ExpensesCategorySummaryResponseDTO
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal TotalAmount { get; set; }
        public int ExpenseCount { get; set; }
    }
}
