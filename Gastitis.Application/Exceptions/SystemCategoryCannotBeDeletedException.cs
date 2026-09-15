namespace Gastitis.Application.Exceptions
{
	public class SystemCategoryCannotBeDeletedException : Exception
    {
        public SystemCategoryCannotBeDeletedException(int categoryID) : base($"System category with ID {categoryID} cannot be deleted") { }
    }
}