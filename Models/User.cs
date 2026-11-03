namespace VertexLearn.Models
{
    /// <summary>Row in the Users table.</summary>
    public class User
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = Roles.Student;
        public string? Bio { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public static class Roles
    {
        public const string Student = "Student";
        public const string Admin = "Admin";
        public static readonly string[] All = { Student, Admin };
    }
}
