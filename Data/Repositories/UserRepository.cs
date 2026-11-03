using System.Data.Common;
using VertexLearn.Models;

namespace VertexLearn.Data.Repositories
{
    public class UserRepository
    {
        private readonly Db _db;
        public UserRepository(Db db) => _db = db;

        private static User Map(DbDataReader r) => new()
        {
            UserId = r.Int("UserId"),
            FullName = r.Str("FullName"),
            Email = r.Str("Email"),
            PasswordHash = r.Str("PasswordHash"),
            Role = r.Str("Role"),
            Bio = r.StrOrNull("Bio"),
            CreatedAt = r.Date("CreatedAt")
        };

        public Task<List<User>> GetAllAsync(string? search = null, string? role = null) =>
            _db.QueryAsync(@"
                SELECT UserId, FullName, Email, PasswordHash, Role, Bio, CreatedAt
                FROM Users
                WHERE (@Search IS NULL OR FullName LIKE '%' + @Search + '%' OR Email LIKE '%' + @Search + '%')
                  AND (@Role IS NULL OR Role = @Role)
                ORDER BY CreatedAt DESC",
                Map, ("@Search", string.IsNullOrWhiteSpace(search) ? null : search.Trim()),
                     ("@Role", string.IsNullOrWhiteSpace(role) ? null : role));

        public Task<List<User>> GetRecentAsync(int count) =>
            _db.QueryAsync(@"
                SELECT TOP (@Count) UserId, FullName, Email, PasswordHash, Role, Bio, CreatedAt
                FROM Users ORDER BY CreatedAt DESC", Map, ("@Count", count));

        public Task<User?> GetByIdAsync(int id) =>
            _db.QuerySingleAsync("SELECT * FROM Users WHERE UserId = @Id", Map, ("@Id", id));

        public Task<User?> GetByEmailAsync(string email) =>
            _db.QuerySingleAsync("SELECT * FROM Users WHERE Email = @Email", Map, ("@Email", email.Trim()));

        public async Task<bool> EmailExistsAsync(string email, int excludeUserId = 0) =>
            await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Users WHERE Email = @Email AND UserId <> @Exclude",
                ("@Email", email.Trim()), ("@Exclude", excludeUserId)) > 0;

        public Task<int> CountByRoleAsync(string role) =>
            _db.ScalarAsync<int>("SELECT COUNT(*) FROM Users WHERE Role = @Role", ("@Role", role));

        // INSERT — returns the new UserId
        public Task<int> CreateAsync(User user) =>
            _db.ScalarAsync<int>(@"
                INSERT INTO Users (FullName, Email, PasswordHash, Role, Bio)
                VALUES (@FullName, @Email, @PasswordHash, @Role, @Bio);
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                ("@FullName", user.FullName.Trim()), ("@Email", user.Email.Trim().ToLowerInvariant()),
                ("@PasswordHash", user.PasswordHash), ("@Role", user.Role), ("@Bio", user.Bio));

        // UPDATE profile / admin edit
        public Task<int> UpdateAsync(User user) =>
            _db.ExecuteAsync(@"
                UPDATE Users
                SET FullName = @FullName, Email = @Email, Role = @Role, Bio = @Bio
                WHERE UserId = @Id",
                ("@FullName", user.FullName.Trim()), ("@Email", user.Email.Trim().ToLowerInvariant()),
                ("@Role", user.Role), ("@Bio", user.Bio), ("@Id", user.UserId));

        public Task<int> UpdatePasswordAsync(int userId, string passwordHash) =>
            _db.ExecuteAsync("UPDATE Users SET PasswordHash = @Hash WHERE UserId = @Id",
                ("@Hash", passwordHash), ("@Id", userId));

        // DELETE — replies use NO ACTION, so remove them first inside one transaction.
        // Enrolments, attempts and posts are removed by ON DELETE CASCADE.
        public async Task<int> DeleteAsync(int userId)
        {
            await using var connection = await _db.OpenAsync();
            await using var tx = await connection.BeginTransactionAsync();
            var p = new (string, object?)[] { ("@Id", userId) };

            await Db.CreateCommand(connection, "DELETE FROM DiscussionReplies WHERE UserId = @Id", p, tx).ExecuteNonQueryAsync();
            var rows = await Db.CreateCommand(connection, "DELETE FROM Users WHERE UserId = @Id", p, tx).ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return rows;
        }
    }
}
