using System.Data.Common;
using VertexLearn.Models;

namespace VertexLearn.Data.Repositories
{
    public class DiscussionRepository
    {
        private readonly Db _db;
        public DiscussionRepository(Db db) => _db = db;

        private static DiscussionPost MapPost(DbDataReader r) => new()
        {
            PostId = r.Int("PostId"),
            CourseId = r.Int("CourseId"),
            CourseTitle = r.Str("CourseTitle"),
            UserId = r.Int("UserId"),
            AuthorName = r.Str("AuthorName"),
            AuthorRole = r.Str("AuthorRole"),
            Title = r.Str("Title"),
            Body = r.Str("Body"),
            CreatedAt = r.Date("CreatedAt"),
            UpdatedAt = r.DateOrNull("UpdatedAt"),
            ReplyCount = r.IntOrZero("ReplyCount")
        };

        private const string PostSelect = @"
            SELECT p.PostId, p.CourseId, c.Title AS CourseTitle, p.UserId, u.FullName AS AuthorName, u.Role AS AuthorRole,
                   p.Title, p.Body, p.CreatedAt, p.UpdatedAt,
                   (SELECT COUNT(*) FROM DiscussionReplies r WHERE r.PostId = p.PostId) AS ReplyCount
            FROM DiscussionPosts p
            INNER JOIN Users u   ON u.UserId = p.UserId
            INNER JOIN Courses c ON c.CourseId = p.CourseId";

        public Task<List<DiscussionPost>> GetByCourseAsync(int courseId, int? top = null) =>
            _db.QueryAsync(PostSelect.Replace("SELECT p.PostId", "SELECT TOP (@Top) p.PostId") +
                " WHERE p.CourseId = @CourseId ORDER BY p.CreatedAt DESC",
                MapPost, ("@CourseId", courseId), ("@Top", top ?? int.MaxValue));

        public Task<List<DiscussionPost>> GetAllAsync(string? search = null) =>
            _db.QueryAsync(PostSelect + @"
                WHERE (@Search IS NULL OR p.Title LIKE '%' + @Search + '%' OR p.Body LIKE '%' + @Search + '%')
                ORDER BY p.CreatedAt DESC", MapPost,
                ("@Search", string.IsNullOrWhiteSpace(search) ? null : search.Trim()));

        public Task<DiscussionPost?> GetPostAsync(int id) =>
            _db.QuerySingleAsync(PostSelect + " WHERE p.PostId = @Id", MapPost, ("@Id", id));

        public Task<int> CountAsync() => _db.ScalarAsync<int>("SELECT COUNT(*) FROM DiscussionPosts");

        public Task<int> CountByUserAsync(int userId) =>
            _db.ScalarAsync<int>("SELECT COUNT(*) FROM DiscussionPosts WHERE UserId = @U", ("@U", userId));

        public Task<int> CountByCourseAsync(int courseId) =>
            _db.ScalarAsync<int>("SELECT COUNT(*) FROM DiscussionPosts WHERE CourseId = @C", ("@C", courseId));

        public Task<int> CreatePostAsync(DiscussionPost p) =>
            _db.ScalarAsync<int>(@"
                INSERT INTO DiscussionPosts (CourseId, UserId, Title, Body) VALUES (@CourseId, @UserId, @Title, @Body);
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                ("@CourseId", p.CourseId), ("@UserId", p.UserId), ("@Title", p.Title.Trim()), ("@Body", p.Body.Trim()));

        public Task<int> UpdatePostAsync(DiscussionPost p) =>
            _db.ExecuteAsync(@"
                UPDATE DiscussionPosts SET Title = @Title, Body = @Body, UpdatedAt = SYSDATETIME()
                WHERE PostId = @Id",
                ("@Title", p.Title.Trim()), ("@Body", p.Body.Trim()), ("@Id", p.PostId));

        public Task<int> DeletePostAsync(int id) =>
            _db.ExecuteAsync("DELETE FROM DiscussionPosts WHERE PostId = @Id", ("@Id", id)); // replies cascade

        /* ---------------- Replies ---------------- */

        public Task<List<DiscussionReply>> GetRepliesAsync(int postId) =>
            _db.QueryAsync(@"
                SELECT r.ReplyId, r.PostId, r.UserId, u.FullName AS AuthorName, u.Role AS AuthorRole, r.Body, r.CreatedAt
                FROM DiscussionReplies r INNER JOIN Users u ON u.UserId = r.UserId
                WHERE r.PostId = @PostId ORDER BY r.CreatedAt",
                r => new DiscussionReply
                {
                    ReplyId = r.Int("ReplyId"),
                    PostId = r.Int("PostId"),
                    UserId = r.Int("UserId"),
                    AuthorName = r.Str("AuthorName"),
                    AuthorRole = r.Str("AuthorRole"),
                    Body = r.Str("Body"),
                    CreatedAt = r.Date("CreatedAt")
                }, ("@PostId", postId));

        public Task<DiscussionReply?> GetReplyAsync(int id) =>
            _db.QuerySingleAsync("SELECT ReplyId, PostId, UserId, Body, CreatedAt FROM DiscussionReplies WHERE ReplyId = @Id",
                r => new DiscussionReply
                {
                    ReplyId = r.Int("ReplyId"),
                    PostId = r.Int("PostId"),
                    UserId = r.Int("UserId"),
                    Body = r.Str("Body"),
                    CreatedAt = r.Date("CreatedAt")
                }, ("@Id", id));

        public Task<int> CreateReplyAsync(DiscussionReply r) =>
            _db.ExecuteAsync("INSERT INTO DiscussionReplies (PostId, UserId, Body) VALUES (@PostId, @UserId, @Body)",
                ("@PostId", r.PostId), ("@UserId", r.UserId), ("@Body", r.Body.Trim()));

        public Task<int> DeleteReplyAsync(int id) =>
            _db.ExecuteAsync("DELETE FROM DiscussionReplies WHERE ReplyId = @Id", ("@Id", id));
    }
}
