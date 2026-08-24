using System;
using System.Collections.Generic;
using System.Text;

namespace Gastitis.Application.DTOs.Summaries
{
    public class ExpensesSummaryResponseDTO
    {
        public decimal TotalAmount   { get; set; }
        public int ExpensesCount { get; set; }
    }
}
