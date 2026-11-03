using System.ComponentModel.DataAnnotations;

namespace VertexLearn.Models
{
    /// <summary>Multiple-choice question with four options (A–D).</summary>
    public class Question : IValidatableObject
    {
        public int QuestionId { get; set; }
        public int QuizId { get; set; }

        [Display(Name = "Question")]
        [Required(ErrorMessage = "Enter the question text.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Question must be 5–500 characters.")]
        public string QuestionText { get; set; } = string.Empty;

        [Display(Name = "Option A")]
        [Required(ErrorMessage = "Enter option A.")]
        [StringLength(200)]
        public string OptionA { get; set; } = string.Empty;

        [Display(Name = "Option B")]
        [Required(ErrorMessage = "Enter option B.")]
        [StringLength(200)]
        public string OptionB { get; set; } = string.Empty;

        [Display(Name = "Option C")]
        [Required(ErrorMessage = "Enter option C.")]
        [StringLength(200)]
        public string OptionC { get; set; } = string.Empty;

        [Display(Name = "Option D")]
        [Required(ErrorMessage = "Enter option D.")]
        [StringLength(200)]
        public string OptionD { get; set; } = string.Empty;

        [Display(Name = "Correct option")]
        [Required(ErrorMessage = "Choose the correct option.")]
        [RegularExpression("^[ABCD]$", ErrorMessage = "Correct option must be A, B, C or D.")]
        public string CorrectOption { get; set; } = "A";

        public string OptionText(string letter) => letter switch
        {
            "A" => OptionA,
            "B" => OptionB,
            "C" => OptionC,
            "D" => OptionD,
            _ => string.Empty
        };

        // Server-side business rule: the four options must all be different.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var options = new[] { OptionA, OptionB, OptionC, OptionD }
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => o.Trim().ToLowerInvariant())
                .ToList();

            if (options.Count == 4 && options.Distinct().Count() < 4)
            {
                yield return new ValidationResult("Each option must be different.", new[] { nameof(OptionA) });
            }
        }
    }
}
