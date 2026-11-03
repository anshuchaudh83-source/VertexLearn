using System.Data.Common;
using VertexLearn.Models;

namespace VertexLearn.Data.Repositories
{
    public class CourseRepository
    {
        private readonly Db _db;
        public CourseRepository(Db db) => _db = db;

        // Shared SELECT with the category name and live counts
        private const string BaseSelect = @"
            SELECT co.CourseId, co.CategoryId, cat.Name AS CategoryName, co.Title, co.Description,
                   co.Instructor, co.LessonCount, co.Level, co.ImageUrl, co.CreatedAt,
                   (SELECT COUNT(*) FROM Enrollments e WHERE e.CourseId = co.CourseId) AS EnrollmentCount,
                   (SELECT COUNT(*) FROM Resources r   WHERE r.CourseId = co.CourseId) AS ResourceCount,
                   (SELECT COUNT(*) FROM Quizzes q     WHERE q.CourseId = co.CourseId) AS QuizCount
            FROM Courses co
            INNER JOIN Categories cat ON cat.CategoryId = co.CategoryId";

        internal static Course Map(DbDataReader r) => new()
        {
            CourseId = r.Int("CourseId"),
            CategoryId = r.Int("CategoryId"),
            CategoryName = r.Str("CategoryName"),
            Title = r.Str("Title"),
            Description = r.Str("Description"),
            Instructor = r.Str("Instructor"),
            LessonCount = r.Int("LessonCount"),
            Level = r.Str("Level"),
            ImageUrl = r.StrOrNull("ImageUrl"),
            CreatedAt = r.Date("CreatedAt"),
            EnrollmentCount = r.IntOrZero("EnrollmentCount"),
            ResourceCount = r.IntOrZero("ResourceCount"),
            QuizCount = r.IntOrZero("QuizCount")
        };

        public Task<List<Course>> GetAllAsync(int? categoryId = null, string? search = null) =>
            _db.QueryAsync(BaseSelect + @"
                WHERE (@CategoryId IS NULL OR co.CategoryId = @CategoryId)
                  AND (@Search IS NULL OR co.Title LIKE '%' + @Search + '%'
                       OR co.Description LIKE '%' + @Search + '%' OR co.Instructor LIKE '%' + @Search + '%')
                ORDER BY co.Title",
                Map, ("@CategoryId", categoryId),
                     ("@Search", string.IsNullOrWhiteSpace(search) ? null : search.Trim()));

        // Most-enrolled courses first
        public Task<List<Course>> GetFeaturedAsync(int count) =>
            _db.QueryAsync($"SELECT TOP (@Count) * FROM ({BaseSelect}) x ORDER BY x.EnrollmentCount DESC, x.Title",
                Map, ("@Count", count));

        public Task<Course?> GetByIdAsync(int id) =>
            _db.QuerySingleAsync(BaseSelect + " WHERE co.CourseId = @Id", Map, ("@Id", id));

        public Task<int> CountAsync() => _db.ScalarAsync<int>("SELECT COUNT(*) FROM Courses");

        public Task<int> CreateAsync(Course c) =>
            _db.ScalarAsync<int>(@"
                INSERT INTO Courses (CategoryId, Title, Description, Instructor, LessonCount, Level, ImageUrl)
                VALUES (@CategoryId, @Title, @Description, @Instructor, @LessonCount, @Level, @ImageUrl);
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                ("@CategoryId", c.CategoryId), ("@Title", c.Title.Trim()), ("@Description", c.Description.Trim()),
                ("@Instructor", c.Instructor.Trim()), ("@LessonCount", c.LessonCount), ("@Level", c.Level),
                ("@ImageUrl", string.IsNullOrWhiteSpace(c.ImageUrl) ? null : c.ImageUrl.Trim()));

        public Task<int> UpdateAsync(Course c) =>
            _db.ExecuteAsync(@"
                UPDATE Courses
                SET CategoryId = @CategoryId, Title = @Title, Description = @Description,
                    Instructor = @Instructor, LessonCount = @LessonCount, Level = @Level, ImageUrl = @ImageUrl
                WHERE CourseId = @Id",
                ("@CategoryId", c.CategoryId), ("@Title", c.Title.Trim()), ("@Description", c.Description.Trim()),
                ("@Instructor", c.Instructor.Trim()), ("@LessonCount", c.LessonCount), ("@Level", c.Level),
                ("@ImageUrl", string.IsNullOrWhiteSpace(c.ImageUrl) ? null : c.ImageUrl.Trim()), ("@Id", c.CourseId));

        // Resources, quizzes, questions, attempts, enrolments and posts cascade automatically.
        // AttemptAnswers → Questions is NO ACTION, so those rows are removed first.
        public async Task<int> DeleteAsync(int id)
        {
            await using var connection = await _db.OpenAsync();
            await using var tx = await connection.BeginTransactionAsync();
            var p = new (string, object?)[] { ("@Id", id) };

            await Db.CreateCommand(connection, @"
                DELETE aa FROM AttemptAnswers aa
                INNER JOIN Questions qu ON qu.QuestionId = aa.QuestionId
                INNER JOIN Quizzes q    ON q.QuizId = qu.QuizId
                WHERE q.CourseId = @Id", p, tx).ExecuteNonQueryAsync();
            var rows = await Db.CreateCommand(connection, "DELETE FROM Courses WHERE CourseId = @Id", p, tx).ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return rows;
        }
    }
}
