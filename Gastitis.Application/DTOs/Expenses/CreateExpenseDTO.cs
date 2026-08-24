using System;

namespace Gastitis.Application.DTOs
{
    public class CreateExpenseDTO
    {
        public decimal Value { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public DateTime date { get; set; }
    }
}
