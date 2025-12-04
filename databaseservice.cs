using Microsoft.Data.SqlClient;
using System.Data;

namespace VaultRoleGenerator.Services;

public interface IDatabaseService
{
    Task<List<string>> GetDatabaseListAsync(string connectionString);
    Task<bool> TestConnectionAsync(string connectionString);
}

public class DatabaseService : IDatabaseService
{
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(ILogger<DatabaseService> logger)
    {
        _logger = logger;
    }

    public async Task<List<string>> GetDatabaseListAsync(string connectionString)
    {
        var databases = new List<string>();
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Query to get all user databases (excluding system databases)
            var query = "query"

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                databases.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving database list");
            throw new InvalidOperationException("Failed to retrieve database list. Please check your connection string.", ex);
        }

        return databases;
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return false;
        }
    }
} 
