namespace Gastitis.Application.Exceptions
{
    public class CategoryNotFoundException : Exception
    {
        public CategoryNotFoundException(int categoryId) : base($"Category with ID {categoryId} not found.") { }
    }
}