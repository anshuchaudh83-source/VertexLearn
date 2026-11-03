using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace VertexLearn.Data
{
    /// <summary>
    /// Runs Database/VertexLearnDb.sql on start-up. The script is idempotent:
    /// it only creates the database / tables / sample rows that don't exist yet,
    /// so it is safe to run on every launch.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IConfiguration configuration, IWebHostEnvironment env, ILogger logger)
        {
            var connectionString = configuration.GetConnectionString("VertexLearnDb")!;

            var scriptPath = new[]
            {
                Path.Combine(env.ContentRootPath, "Database", "VertexLearnDb.sql"),
                Path.Combine(AppContext.BaseDirectory, "Database", "VertexLearnDb.sql")
            }.FirstOrDefault(File.Exists);

            if (scriptPath == null)
            {
                logger.LogWarning("Database script not found — skipping database initialisation.");
                return;
            }

            // Connect to 'master' first, because the VertexLearnDb database may not exist yet.
            var masterBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };

            try
            {
                var script = await File.ReadAllTextAsync(scriptPath);
                // SQL Server tools use "GO" to separate batches; ADO.NET must send each batch separately.
                var batches = Regex.Split(script, @"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                                   .Where(b => !string.IsNullOrWhiteSpace(b));

                await using var connection = new SqlConnection(masterBuilder.ConnectionString);
                await connection.OpenAsync();
                foreach (var batch in batches)
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText = batch;
                    command.CommandTimeout = 120;
                    await command.ExecuteNonQueryAsync();
                }
                logger.LogInformation("VertexLearnDb is ready.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not initialise the database. Check the 'VertexLearnDb' connection string and that SQL Server / LocalDB is running.");
                throw;
            }
        }
    }
}
