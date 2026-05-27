using Microsoft.Data.Sqlite;

namespace ActivityTracker.Core.Data;

/// <summary>
/// Creates the AppUsage table and index on first run.
/// Idempotent — safe to call on every startup.
/// </summary>
public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string dbPath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    public string ConnectionString => _connectionString;

    /// <summary>
    /// Ensures the schema exists. Uses IF NOT EXISTS so it's safe to run repeatedly.
    /// </summary>
    public async Task InitializeAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        const string createTable = @"
            CREATE TABLE IF NOT EXISTS AppUsage (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                ProcessName     TEXT    NOT NULL,
                WindowTitle     TEXT,
                StartTime       TEXT    NOT NULL,
                DurationSeconds INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_processname ON AppUsage(ProcessName);
            CREATE INDEX IF NOT EXISTS idx_starttime  ON AppUsage(StartTime);
        ";

        await using var command = new SqliteCommand(createTable, connection);
        await command.ExecuteNonQueryAsync();
    }
}
