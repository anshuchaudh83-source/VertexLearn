using VertexLearn.Models;

namespace VertexLearn.Data.Repositories
{
    public class EnrollmentRepository
    {
        private readonly Db _db;
        public EnrollmentRepository(Db db) => _db = db;

        public async Task<bool> IsEnrolledAsync(int userId, int courseId) =>
            await _db.ScalarAsync<int>("SELECT COUNT(*) FROM Enrollments WHERE UserId = @U AND CourseId = @C",
                ("@U", userId), ("@C", courseId)) > 0;

        public async Task<HashSet<int>> GetEnrolledCourseIdsAsync(int userId) =>
            (await _db.QueryAsync("SELECT CourseId FROM Enrollments WHERE UserId = @U",
                r => r.Int("CourseId"), ("@U", userId))).ToHashSet();

        // INSERT only if not already enrolled (the UNIQUE constraint is a second safety net)
        public Task<int> EnrollAsync(int userId, int courseId) =>
            _db.ExecuteAsync(@"
                IF NOT EXISTS (SELECT 1 FROM Enrollments WHERE UserId = @U AND CourseId = @C)
                    INSERT INTO Enrollments (UserId, CourseId) VALUES (@U, @C)",
                ("@U", userId), ("@C", courseId));

        public Task<int> UnenrollAsync(int userId, int courseId) =>
            _db.ExecuteAsync("DELETE FROM Enrollments WHERE UserId = @U AND CourseId = @C",
                ("@U", userId), ("@C", courseId));

        // A student's courses with progress = quizzes passed (best attempt) / quizzes in course
        public Task<List<EnrolledCourse>> GetForUserAsync(int userId) =>
            _db.QueryAsync(@"
                SELECT co.CourseId, co.CategoryId, cat.Name AS CategoryName, co.Title, co.Description,
                       co.Instructor, co.LessonCount, co.Level, co.ImageUrl, co.CreatedAt,
                       0 AS EnrollmentCount, 0 AS ResourceCount,
                       (SELECT COUNT(*) FROM Quizzes q WHERE q.CourseId = co.CourseId) AS QuizCount,
                       e.EnrolledAt,
                       (SELECT COUNT(*) FROM Quizzes q
                        WHERE q.CourseId = co.CourseId
                          AND EXISTS (SELECT 1 FROM QuizAttempts a
                                      WHERE a.QuizId = q.QuizId AND a.UserId = @U
                                        AND a.TotalQuestions > 0
                                        AND a.Score * 100.0 / a.TotalQuestions >= q.PassPercent)) AS QuizzesPassed
                FROM Enrollments e
                INNER JOIN Courses co     ON co.CourseId = e.CourseId
                INNER JOIN Categories cat ON cat.CategoryId = co.CategoryId
                WHERE e.UserId = @U
                ORDER BY e.EnrolledAt DESC",
                r => new EnrolledCourse
                {
                    Course = CourseRepository.Map(r),
                    EnrolledAt = r.Date("EnrolledAt"),
                    TotalQuizzes = r.IntOrZero("QuizCount"),
                    QuizzesPassed = r.IntOrZero("QuizzesPassed")
                }, ("@U", userId));

        public Task<List<EnrollmentRecord>> GetRecentAsync(int count) =>
            _db.QueryAsync(@"
                SELECT TOP (@Count) e.EnrollmentId, u.FullName, c.Title, e.EnrolledAt
                FROM Enrollments e
                INNER JOIN Users u   ON u.UserId = e.UserId
                INNER JOIN Courses c ON c.CourseId = e.CourseId
                ORDER BY e.EnrolledAt DESC",
                r => new EnrollmentRecord
                {
                    EnrollmentId = r.Int("EnrollmentId"),
                    StudentName = r.Str("FullName"),
                    CourseTitle = r.Str("Title"),
                    EnrolledAt = r.Date("EnrolledAt")
                }, ("@Count", count));
    }
}
