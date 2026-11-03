using System.Data.Common;
using VertexLearn.Models;

namespace VertexLearn.Data.Repositories
{
    public class QuizRepository
    {
        private readonly Db _db;
        public QuizRepository(Db db) => _db = db;

        /* ---------------- Quizzes ---------------- */

        private static Quiz MapQuiz(DbDataReader r) => new()
        {
            QuizId = r.Int("QuizId"),
            CourseId = r.Int("CourseId"),
            CourseTitle = r.Str("CourseTitle"),
            Title = r.Str("Title"),
            Description = r.StrOrNull("Description"),
            PassPercent = r.Int("PassPercent"),
            QuestionCount = r.IntOrZero("QuestionCount"),
            AttemptCount = r.IntOrZero("AttemptCount")
        };

        private const string QuizSelect = @"
            SELECT q.QuizId, q.CourseId, c.Title AS CourseTitle, q.Title, q.Description, q.PassPercent,
                   (SELECT COUNT(*) FROM Questions qu   WHERE qu.QuizId = q.QuizId) AS QuestionCount,
                   (SELECT COUNT(*) FROM QuizAttempts a WHERE a.QuizId = q.QuizId)  AS AttemptCount
            FROM Quizzes q INNER JOIN Courses c ON c.CourseId = q.CourseId";

        public Task<List<Quiz>> GetAllAsync(int? courseId = null) =>
            _db.QueryAsync(QuizSelect + @"
                WHERE (@CourseId IS NULL OR q.CourseId = @CourseId)
                ORDER BY c.Title, q.Title", MapQuiz, ("@CourseId", courseId));

        public Task<Quiz?> GetByIdAsync(int id) =>
            _db.QuerySingleAsync(QuizSelect + " WHERE q.QuizId = @Id", MapQuiz, ("@Id", id));

        // Quizzes of a course together with the current student's best score
        public Task<List<Quiz>> GetForCourseAsync(int courseId, int userId) =>
            _db.QueryAsync(QuizSelect.Replace("FROM Quizzes q", @",
                   (SELECT MAX(a.Score * 100 / NULLIF(a.TotalQuestions, 0)) FROM QuizAttempts a
                    WHERE a.QuizId = q.QuizId AND a.UserId = @UserId) AS BestPercent,
                   (SELECT COUNT(*) FROM QuizAttempts a WHERE a.QuizId = q.QuizId AND a.UserId = @UserId) AS MyAttempts
            FROM Quizzes q") + " WHERE q.CourseId = @CourseId ORDER BY q.Title",
                r =>
                {
                    var quiz = MapQuiz(r);
                    quiz.BestPercent = r["BestPercent"] == DBNull.Value ? null : Convert.ToInt32(r["BestPercent"]);
                    quiz.MyAttempts = r.IntOrZero("MyAttempts");
                    return quiz;
                }, ("@CourseId", courseId), ("@UserId", userId));

        public Task<int> CountAsync() => _db.ScalarAsync<int>("SELECT COUNT(*) FROM Quizzes");

        public Task<int> CreateAsync(Quiz q) =>
            _db.ScalarAsync<int>(@"
                INSERT INTO Quizzes (CourseId, Title, Description, PassPercent)
                VALUES (@CourseId, @Title, @Description, @PassPercent);
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                ("@CourseId", q.CourseId), ("@Title", q.Title.Trim()), ("@Description", q.Description), ("@PassPercent", q.PassPercent));

        public Task<int> UpdateAsync(Quiz q) =>
            _db.ExecuteAsync(@"
                UPDATE Quizzes SET CourseId = @CourseId, Title = @Title, Description = @Description, PassPercent = @PassPercent
                WHERE QuizId = @Id",
                ("@CourseId", q.CourseId), ("@Title", q.Title.Trim()), ("@Description", q.Description),
                ("@PassPercent", q.PassPercent), ("@Id", q.QuizId));

        public async Task<int> DeleteAsync(int id)
        {
            await using var connection = await _db.OpenAsync();
            await using var tx = await connection.BeginTransactionAsync();
            var p = new (string, object?)[] { ("@Id", id) };

            await Db.CreateCommand(connection, @"
                DELETE aa FROM AttemptAnswers aa
                INNER JOIN Questions qu ON qu.QuestionId = aa.QuestionId
                WHERE qu.QuizId = @Id", p, tx).ExecuteNonQueryAsync();
            var rows = await Db.CreateCommand(connection, "DELETE FROM Quizzes WHERE QuizId = @Id", p, tx).ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return rows;
        }

        /* ---------------- Questions ---------------- */

        private static Question MapQuestion(DbDataReader r) => new()
        {
            QuestionId = r.Int("QuestionId"),
            QuizId = r.Int("QuizId"),
            QuestionText = r.Str("QuestionText"),
            OptionA = r.Str("OptionA"),
            OptionB = r.Str("OptionB"),
            OptionC = r.Str("OptionC"),
            OptionD = r.Str("OptionD"),
            CorrectOption = r.Str("CorrectOption")
        };

        public Task<List<Question>> GetQuestionsAsync(int quizId) =>
            _db.QueryAsync("SELECT * FROM Questions WHERE QuizId = @QuizId ORDER BY QuestionId",
                MapQuestion, ("@QuizId", quizId));

        public Task<Question?> GetQuestionAsync(int id) =>
            _db.QuerySingleAsync("SELECT * FROM Questions WHERE QuestionId = @Id", MapQuestion, ("@Id", id));

        public Task<int> CreateQuestionAsync(Question q) =>
            _db.ExecuteAsync(@"
                INSERT INTO Questions (QuizId, QuestionText, OptionA, OptionB, OptionC, OptionD, CorrectOption)
                VALUES (@QuizId, @Text, @A, @B, @C, @D, @Correct)",
                ("@QuizId", q.QuizId), ("@Text", q.QuestionText.Trim()), ("@A", q.OptionA.Trim()), ("@B", q.OptionB.Trim()),
                ("@C", q.OptionC.Trim()), ("@D", q.OptionD.Trim()), ("@Correct", q.CorrectOption));

        public Task<int> UpdateQuestionAsync(Question q) =>
            _db.ExecuteAsync(@"
                UPDATE Questions
                SET QuestionText = @Text, OptionA = @A, OptionB = @B, OptionC = @C, OptionD = @D, CorrectOption = @Correct
                WHERE QuestionId = @Id",
                ("@Text", q.QuestionText.Trim()), ("@A", q.OptionA.Trim()), ("@B", q.OptionB.Trim()),
                ("@C", q.OptionC.Trim()), ("@D", q.OptionD.Trim()), ("@Correct", q.CorrectOption), ("@Id", q.QuestionId));

        public async Task<int> DeleteQuestionAsync(int id)
        {
            await using var connection = await _db.OpenAsync();
            await using var tx = await connection.BeginTransactionAsync();
            var p = new (string, object?)[] { ("@Id", id) };

            await Db.CreateCommand(connection, "DELETE FROM AttemptAnswers WHERE QuestionId = @Id", p, tx).ExecuteNonQueryAsync();
            var rows = await Db.CreateCommand(connection, "DELETE FROM Questions WHERE QuestionId = @Id", p, tx).ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return rows;
        }

        /* ---------------- Attempts ---------------- */

        private static QuizAttempt MapAttempt(DbDataReader r) => new()
        {
            AttemptId = r.Int("AttemptId"),
            UserId = r.Int("UserId"),
            StudentName = r.Str("StudentName"),
            QuizId = r.Int("QuizId"),
            QuizTitle = r.Str("QuizTitle"),
            CourseId = r.Int("CourseId"),
            CourseTitle = r.Str("CourseTitle"),
            Score = r.Int("Score"),
            TotalQuestions = r.Int("TotalQuestions"),
            PassPercent = r.Int("PassPercent"),
            AttemptedAt = r.Date("AttemptedAt")
        };

        private const string AttemptSelect = @"
            SELECT a.AttemptId, a.UserId, u.FullName AS StudentName, a.QuizId, q.Title AS QuizTitle,
                   c.CourseId, c.Title AS CourseTitle, a.Score, a.TotalQuestions, q.PassPercent, a.AttemptedAt
            FROM QuizAttempts a
            INNER JOIN Users u   ON u.UserId = a.UserId
            INNER JOIN Quizzes q ON q.QuizId = a.QuizId
            INNER JOIN Courses c ON c.CourseId = q.CourseId";

        /// <summary>Saves the attempt and every answer in a single transaction; returns the AttemptId.</summary>
        public async Task<int> SaveAttemptAsync(int userId, int quizId, int score, int total, IEnumerable<(int QuestionId, string? Selected, bool IsCorrect)> answers)
        {
            await using var connection = await _db.OpenAsync();
            await using var tx = await connection.BeginTransactionAsync();

            var attemptId = Convert.ToInt32(await Db.CreateCommand(connection, @"
                INSERT INTO QuizAttempts (UserId, QuizId, Score, TotalQuestions)
                VALUES (@UserId, @QuizId, @Score, @Total);
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new (string, object?)[] { ("@UserId", userId), ("@QuizId", quizId), ("@Score", score), ("@Total", total) }, tx)
                .ExecuteScalarAsync());

            foreach (var a in answers)
            {
                await Db.CreateCommand(connection, @"
                    INSERT INTO AttemptAnswers (AttemptId, QuestionId, SelectedOption, IsCorrect)
                    VALUES (@AttemptId, @QuestionId, @Selected, @IsCorrect)",
                    new (string, object?)[] { ("@AttemptId", attemptId), ("@QuestionId", a.QuestionId),
                                              ("@Selected", a.Selected), ("@IsCorrect", a.IsCorrect) }, tx)
                    .ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return attemptId;
        }

        public Task<QuizAttempt?> GetAttemptAsync(int attemptId) =>
            _db.QuerySingleAsync(AttemptSelect + " WHERE a.AttemptId = @Id", MapAttempt, ("@Id", attemptId));

        public Task<List<AttemptAnswer>> GetAttemptAnswersAsync(int attemptId) =>
            _db.QueryAsync(@"
                SELECT qu.QuestionId, qu.QuestionText, qu.OptionA, qu.OptionB, qu.OptionC, qu.OptionD,
                       qu.CorrectOption, aa.SelectedOption, aa.IsCorrect
                FROM AttemptAnswers aa
                INNER JOIN Questions qu ON qu.QuestionId = aa.QuestionId
                WHERE aa.AttemptId = @Id
                ORDER BY qu.QuestionId",
                r => new AttemptAnswer
                {
                    QuestionId = r.Int("QuestionId"),
                    QuestionText = r.Str("QuestionText"),
                    OptionA = r.Str("OptionA"),
                    OptionB = r.Str("OptionB"),
                    OptionC = r.Str("OptionC"),
                    OptionD = r.Str("OptionD"),
                    CorrectOption = r.Str("CorrectOption"),
                    SelectedOption = r.StrOrNull("SelectedOption"),
                    IsCorrect = r.Bool("IsCorrect")
                }, ("@Id", attemptId));

        public Task<List<QuizAttempt>> GetAttemptsForUserAsync(int userId, int? top = null) =>
            _db.QueryAsync(AttemptSelect.Replace("SELECT a.AttemptId", "SELECT TOP (@Top) a.AttemptId") + @"
                WHERE a.UserId = @UserId ORDER BY a.AttemptedAt DESC",
                MapAttempt, ("@UserId", userId), ("@Top", top ?? int.MaxValue));

        public Task<List<QuizAttempt>> GetRecentAttemptsAsync(int count) =>
            _db.QueryAsync(AttemptSelect.Replace("SELECT a.AttemptId", "SELECT TOP (@Top) a.AttemptId") +
                " ORDER BY a.AttemptedAt DESC", MapAttempt, ("@Top", count));

        public async Task<int> GetAveragePercentAsync(int userId) =>
            await _db.ScalarAsync<int>(@"
                SELECT CAST(ROUND(AVG(Score * 100.0 / NULLIF(TotalQuestions, 0)), 0) AS INT)
                FROM QuizAttempts WHERE UserId = @UserId", ("@UserId", userId));

        public Task<int> DeleteAttemptAsync(int attemptId) =>
            _db.ExecuteAsync("DELETE FROM QuizAttempts WHERE AttemptId = @Id", ("@Id", attemptId));
    }
}
