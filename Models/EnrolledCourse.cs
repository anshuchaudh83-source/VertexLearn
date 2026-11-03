namespace VertexLearn.Models
{
    /// <summary>A course the current student is enrolled in, plus their progress.</summary>
    public class EnrolledCourse
    {
        public Course Course { get; set; } = new();
        public DateTime EnrolledAt { get; set; }
        public int TotalQuizzes { get; set; }
        public int QuizzesPassed { get; set; }

        public int ProgressPercent => TotalQuizzes == 0 ? 0 : (int)Math.Round(QuizzesPassed * 100.0 / TotalQuizzes);
    }

    /// <summary>Row for the admin "recent enrolments" table.</summary>
    public class EnrollmentRecord
    {
        public int EnrollmentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime EnrolledAt { get; set; }
    }
}
