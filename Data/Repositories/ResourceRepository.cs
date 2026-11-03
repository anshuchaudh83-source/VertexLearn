using System.Data.Common;
using VertexLearn.Models;

namespace VertexLearn.Data.Repositories
{
    public class ResourceRepository
    {
        private readonly Db _db;
        public ResourceRepository(Db db) => _db = db;

        private static Resource Map(DbDataReader r) => new()
        {
            ResourceId = r.Int("ResourceId"),
            CourseId = r.Int("CourseId"),
            CourseTitle = r.Str("CourseTitle"),
            Title = r.Str("Title"),
            Type = r.Str("Type"),
            Url = r.Str("Url"),
            Description = r.StrOrNull("Description"),
            SortOrder = r.Int("SortOrder")
        };

        private const string BaseSelect = @"
            SELECT r.ResourceId, r.CourseId, c.Title AS CourseTitle, r.Title, r.Type, r.Url, r.Description, r.SortOrder
            FROM Resources r INNER JOIN Courses c ON c.CourseId = r.CourseId";

        public Task<List<Resource>> GetAllAsync(int? courseId = null) =>
            _db.QueryAsync(BaseSelect + @"
                WHERE (@CourseId IS NULL OR r.CourseId = @CourseId)
                ORDER BY c.Title, r.SortOrder, r.Title", Map, ("@CourseId", courseId));

        public Task<List<Resource>> GetByCourseAsync(int courseId) =>
            _db.QueryAsync(BaseSelect + " WHERE r.CourseId = @CourseId ORDER BY r.SortOrder, r.Title",
                Map, ("@CourseId", courseId));

        public Task<Resource?> GetByIdAsync(int id) =>
            _db.QuerySingleAsync(BaseSelect + " WHERE r.ResourceId = @Id", Map, ("@Id", id));

        public Task<int> CountAsync() => _db.ScalarAsync<int>("SELECT COUNT(*) FROM Resources");

        public Task<int> CreateAsync(Resource r) =>
            _db.ExecuteAsync(@"
                INSERT INTO Resources (CourseId, Title, Type, Url, Description, SortOrder)
                VALUES (@CourseId, @Title, @Type, @Url, @Description, @SortOrder)",
                ("@CourseId", r.CourseId), ("@Title", r.Title.Trim()), ("@Type", r.Type),
                ("@Url", r.Url.Trim()), ("@Description", r.Description), ("@SortOrder", r.SortOrder));

        public Task<int> UpdateAsync(Resource r) =>
            _db.ExecuteAsync(@"
                UPDATE Resources
                SET CourseId = @CourseId, Title = @Title, Type = @Type, Url = @Url,
                    Description = @Description, SortOrder = @SortOrder
                WHERE ResourceId = @Id",
                ("@CourseId", r.CourseId), ("@Title", r.Title.Trim()), ("@Type", r.Type), ("@Url", r.Url.Trim()),
                ("@Description", r.Description), ("@SortOrder", r.SortOrder), ("@Id", r.ResourceId));

        public Task<int> DeleteAsync(int id) =>
            _db.ExecuteAsync("DELETE FROM Resources WHERE ResourceId = @Id", ("@Id", id));
    }
}
