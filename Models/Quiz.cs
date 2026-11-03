using System.ComponentModel.DataAnnotations;

namespace VertexLearn.Models
{
    public class Quiz
    {
        public int QuizId { get; set; }

        [Display(Name = "Course")]
        [Range(1, int.MaxValue, ErrorMessage = "Choose a course.")]
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a quiz title.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Title must be 3–150 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description can be at most 500 characters.")]
        public string? Description { get; set; }

        [Display(Name = "Pass mark (%)")]
        [Range(1, 100, ErrorMessage = "Pass mark must be between 1 and 100.")]
        public int PassPercent { get; set; } = 50;

        public int QuestionCount { get; set; }
        public int AttemptCount { get; set; }

        // Per-student figures (used on the Learn page)
        public int? BestPercent { get; set; }
        public int MyAttempts { get; set; }
    }
}
