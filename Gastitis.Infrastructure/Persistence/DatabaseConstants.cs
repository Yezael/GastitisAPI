using System;
namespace Gastitis.Infrastructure.Persistence
{
	public static class DatabaseConstants
	{
		public const string UniqueCategoryNamePerOwnerIndex = "IX_Categories_OwnerUserId_NormalizedName";
		public const string CheckCategoryConstraintOwnership = "CK_Categories_Ownership";
	}
}
