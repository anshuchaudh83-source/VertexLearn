using System.Data.Common;
using VertexLearn.Models;

namespace VertexLearn.Data.Repositories
{
    public class ContactRepository
    {
        private readonly Db _db;
        public ContactRepository(Db db) => _db = db;

        private static ContactMessage Map(DbDataReader r) => new()
        {
            MessageId = r.Int("MessageId"),
            Name = r.Str("Name"),
            Email = r.Str("Email"),
            Subject = r.Str("Subject"),
            Message = r.Str("Message"),
            IsRead = r.Bool("IsRead"),
            CreatedAt = r.Date("CreatedAt")
        };

        public Task<List<ContactMessage>> GetAllAsync() =>
            _db.QueryAsync("SELECT * FROM ContactMessages ORDER BY IsRead, CreatedAt DESC", Map);

        public Task<int> CountUnreadAsync() =>
            _db.ScalarAsync<int>("SELECT COUNT(*) FROM ContactMessages WHERE IsRead = 0");

        public Task<int> CreateAsync(ContactMessage m) =>
            _db.ExecuteAsync(@"
                INSERT INTO ContactMessages (Name, Email, Subject, Message)
                VALUES (@Name, @Email, @Subject, @Message)",
                ("@Name", m.Name.Trim()), ("@Email", m.Email.Trim()), ("@Subject", m.Subject.Trim()), ("@Message", m.Message.Trim()));

        public Task<int> SetReadAsync(int id, bool isRead) =>
            _db.ExecuteAsync("UPDATE ContactMessages SET IsRead = @IsRead WHERE MessageId = @Id",
                ("@IsRead", isRead), ("@Id", id));

        public Task<int> DeleteAsync(int id) =>
            _db.ExecuteAsync("DELETE FROM ContactMessages WHERE MessageId = @Id", ("@Id", id));
    }
}
