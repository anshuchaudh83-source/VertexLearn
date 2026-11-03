using System.ComponentModel.DataAnnotations;

namespace VertexLearn.Models
{
    public class Course
    {
        public int CourseId { get; set; }

        [Display(Name = "Category")]
        [Range(1, int.MaxValue, ErrorMessage = "Choose a category.")]
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a course title.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Title must be 3–150 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a course description.")]
        [StringLength(1000, MinimumLength = 20, ErrorMessage = "Description must be 20–1000 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the instructor's name.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Instructor name must be 2–100 characters.")]
        public string Instructor { get; set; } = string.Empty;

        [Display(Name = "Number of lessons")]
        [Range(1, 200, ErrorMessage = "Lessons must be between 1 and 200.")]
        public int LessonCount { get; set; } = 10;

        [Required(ErrorMessage = "Choose a level.")]
        [RegularExpression("^(Beginner|Intermediate|Advanced)$", ErrorMessage = "Choose Beginner, Intermediate or Advanced.")]
        public string Level { get; set; } = "Beginner";

        [Display(Name = "Cover image path or URL")]
        [StringLength(300)]
        [RegularExpression(@"^(https?://|/).*", ErrorMessage = "Use a site path starting with / or a full http(s) URL.")]
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        // Aggregates filled by queries (not stored columns)
        public int EnrollmentCount { get; set; }
        public int ResourceCount { get; set; }
        public int QuizCount { get; set; }

        public string CoverImage => string.IsNullOrWhiteSpace(ImageUrl) ? "/images/courses/default.svg" : ImageUrl!;
    }
}
