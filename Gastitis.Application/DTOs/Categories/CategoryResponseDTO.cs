namespace Gastitis.Application.DTOs.Categories
{
    public class CategoryResponseDTO
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsSystemCategory { get; set; }
    }
}
