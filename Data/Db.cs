using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace VertexLearn.Data
{
    /// <summary>
    /// Thin ADO.NET helper around SQL Server. Every query in the app is written
    /// as plain, parameterised SQL (no string concatenation → no SQL injection).
    /// Parameters are passed as ("@Name", value) tuples.
    /// </summary>
    public class Db
    {
        private readonly string _connectionString;

        public Db(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("VertexLearnDb")
                ?? throw new InvalidOperationException("Connection string 'VertexLearnDb' is missing from appsettings.json.");
        }

        public async Task<DbConnection> OpenAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        /// <summary>SELECT many rows and map each one to an object.</summary>
        public async Task<List<T>> QueryAsync<T>(string sql, Func<DbDataReader, T> map, params (string Name, object? Value)[] parameters)
        {
            await using var connection = await OpenAsync();
            await using var command = CreateCommand(connection, sql, parameters);
            await using var reader = await command.ExecuteReaderAsync();

            var results = new List<T>();
            while (await reader.ReadAsync())
            {
                results.Add(map(reader));
            }
            return results;
        }

        /// <summary>SELECT a single row (or null when nothing matches).</summary>
        public async Task<T?> QuerySingleAsync<T>(string sql, Func<DbDataReader, T> map, params (string Name, object? Value)[] parameters) where T : class
        {
            var rows = await QueryAsync(sql, map, parameters);
            return rows.FirstOrDefault();
        }

        /// <summary>INSERT / UPDATE / DELETE — returns the number of affected rows.</summary>
        public async Task<int> ExecuteAsync(string sql, params (string Name, object? Value)[] parameters)
        {
            await using var connection = await OpenAsync();
            await using var command = CreateCommand(connection, sql, parameters);
            return await command.ExecuteNonQueryAsync();
        }

        /// <summary>Returns the first column of the first row (COUNT, SCOPE_IDENTITY, etc.).</summary>
        public async Task<T> ScalarAsync<T>(string sql, params (string Name, object? Value)[] parameters)
        {
            await using var connection = await OpenAsync();
            await using var command = CreateCommand(connection, sql, parameters);
            var value = await command.ExecuteScalarAsync();
            if (value == null || value == DBNull.Value) return default!;
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public static DbCommand CreateCommand(DbConnection connection, string sql, (string Name, object? Value)[] parameters, DbTransaction? transaction = null)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;
            foreach (var (name, value) in parameters)
            {
                var p = command.CreateParameter();
                p.ParameterName = name;
                p.Value = value ?? DBNull.Value;
                command.Parameters.Add(p);
            }
            return command;
        }
    }
}
