using System;
using System.Collections.Generic;
using System.Text;

namespace Gastitis.Domain.Constants
{
	public static class ExpenseConstraints
	{
		public static readonly int ExpenseDescriptionMaxLength = 200;
		public static readonly int ExpenseFilterKeywordMaxLength = 100;
		public static readonly int ExpenseValuePrecision = 18;
		public static readonly int ExpenseValueScale = 2;
	}
}
