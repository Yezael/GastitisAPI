using System;
using System.Collections.Generic;
using System.Text;

namespace Gastitis.Application.DTOs
{
    public class ExpenseFilterDTO
    {
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? CategoryID { get; set; }
        public string? Keyword { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ExpenseSortingDTO
    {
        public SortDirection SortDirection { get; set; } = SortDirection.Desc;
        public SortBy SortBy { get; set; } = SortBy.Date;
    }

    public enum SortBy
    {
        Date = 0,
        Value = 1,
    }

    public enum SortDirection
    {
        Desc = 0,
        Asc = 1
    }
}
