using System.ComponentModel.DataAnnotations;

namespace VertexLearn.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int StudentCount { get; set; }
        public int AdminCount { get; set; }
        public int CourseCount { get; set; }
        public int ResourceCount { get; set; }
        public int QuizCount { get; set; }
        public int PostCount { get; set; }
        public int UnreadMessages { get; set; }
        public List<User> RecentUsers { get; set; } = new();
        public List<EnrollmentRecord> RecentEnrollments { get; set; } = new();
        public List<QuizAttempt> RecentAttempts { get; set; } = new();
    }

    /// <summary>Admin form for creating / editing a user account.</summary>
    public class UserFormViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Enter the user's full name.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2–100 characters.")]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter an email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose a role.")]
        [RegularExpression("^(Student|Admin)$", ErrorMessage = "Role must be Student or Admin.")]
        public string Role { get; set; } = Roles.Student;

        // Required when creating; optional when editing (blank = keep current password)
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Password needs an uppercase letter, a lowercase letter and a number.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        public bool IsNew => UserId == 0;
    }
}
