using System.ComponentModel.DataAnnotations;

namespace VertexLearn.Models
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Enter a category name.")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "Category name must be 2–60 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Description can be at most 250 characters.")]
        public string? Description { get; set; }

        public int CourseCount { get; set; }
    }
}
