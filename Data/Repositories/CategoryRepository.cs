using System.Data.Common;
using VertexLearn.Models;

namespace VertexLearn.Data.Repositories
{
    public class CategoryRepository
    {
        private readonly Db _db;
        public CategoryRepository(Db db) => _db = db;

        private static Category Map(DbDataReader r) => new()
        {
            CategoryId = r.Int("CategoryId"),
            Name = r.Str("Name"),
            Description = r.StrOrNull("Description"),
            CourseCount = r.IntOrZero("CourseCount")
        };

        public Task<List<Category>> GetAllAsync() =>
            _db.QueryAsync(@"
                SELECT c.CategoryId, c.Name, c.Description, COUNT(co.CourseId) AS CourseCount
                FROM Categories c
                LEFT JOIN Courses co ON co.CategoryId = c.CategoryId
                GROUP BY c.CategoryId, c.Name, c.Description
                ORDER BY c.Name", Map);

        public Task<Category?> GetByIdAsync(int id) =>
            _db.QuerySingleAsync(@"
                SELECT c.CategoryId, c.Name, c.Description,
                       (SELECT COUNT(*) FROM Courses WHERE CategoryId = c.CategoryId) AS CourseCount
                FROM Categories c WHERE c.CategoryId = @Id", Map, ("@Id", id));

        public async Task<bool> NameExistsAsync(string name, int excludeId = 0) =>
            await _db.ScalarAsync<int>("SELECT COUNT(*) FROM Categories WHERE Name = @Name AND CategoryId <> @Exclude",
                ("@Name", name.Trim()), ("@Exclude", excludeId)) > 0;

        public Task<int> CreateAsync(Category c) =>
            _db.ExecuteAsync("INSERT INTO Categories (Name, Description) VALUES (@Name, @Description)",
                ("@Name", c.Name.Trim()), ("@Description", c.Description));

        public Task<int> UpdateAsync(Category c) =>
            _db.ExecuteAsync("UPDATE Categories SET Name = @Name, Description = @Description WHERE CategoryId = @Id",
                ("@Name", c.Name.Trim()), ("@Description", c.Description), ("@Id", c.CategoryId));

        public Task<int> DeleteAsync(int id) =>
            _db.ExecuteAsync("DELETE FROM Categories WHERE CategoryId = @Id", ("@Id", id));
    }
}
