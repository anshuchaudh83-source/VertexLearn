namespace VertexLearn.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Course> FeaturedCourses { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public int CourseCount { get; set; }
        public int StudentCount { get; set; }
        public int QuizCount { get; set; }
    }

    public class CourseCatalogViewModel
    {
        public List<Course> Courses { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public int? SelectedCategoryId { get; set; }
        public string? Search { get; set; }
        public HashSet<int> EnrolledCourseIds { get; set; } = new();
    }

    public class CourseDetailsViewModel
    {
        public Course Course { get; set; } = new();
        public List<Resource> Resources { get; set; } = new();
        public List<Quiz> Quizzes { get; set; } = new();
        public bool IsEnrolled { get; set; }
        public bool IsAdmin { get; set; }
        public int PostCount { get; set; }
    }

    public class LearnViewModel
    {
        public Course Course { get; set; } = new();
        public List<Resource> Resources { get; set; } = new();
        public List<Quiz> Quizzes { get; set; } = new();
        public List<DiscussionPost> RecentPosts { get; set; } = new();
    }

    public class StudentDashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public List<EnrolledCourse> Courses { get; set; } = new();
        public List<QuizAttempt> RecentAttempts { get; set; } = new();
        public int TotalAttempts { get; set; }
        public int AveragePercent { get; set; }
        public int PostCount { get; set; }
    }

    public class QuizTakeViewModel
    {
        public Quiz Quiz { get; set; } = new();
        public List<Question> Questions { get; set; } = new();
    }

    public class QuizResultViewModel
    {
        public QuizAttempt Attempt { get; set; } = new();
        public List<AttemptAnswer> Answers { get; set; } = new();
    }

    public class DiscussionIndexViewModel
    {
        public Course Course { get; set; } = new();
        public List<DiscussionPost> Posts { get; set; } = new();
        public DiscussionPost NewPost { get; set; } = new();
    }

    public class ThreadViewModel
    {
        public DiscussionPost Post { get; set; } = new();
        public List<DiscussionReply> Replies { get; set; } = new();
        public DiscussionReply NewReply { get; set; } = new();
    }
}
