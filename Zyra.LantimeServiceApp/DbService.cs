using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using Zyra.LantimeServiceApp.Interfaces;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DbService> _logger;

    public DbService(IConfiguration configuration, ILogger<DbService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private SqlConnection CreateConnection()
    {
        return new SqlConnection(_configuration.GetConnectionString("ExternalDb"));
    }

    public async Task<List<T>> ExecuteQueryAsync<T>(
        string storedProcedure,
        Func<SqlDataReader, T> map,
        SqlParameter[] parameters = null)
    {
        var result = new List<T>();

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(storedProcedure, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 120
            };

            if (parameters != null)
                command.Parameters.AddRange(parameters);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(map(reader)); // 🔥 mapping delegate
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB query failed");
            throw;
        }

        return result;
    }

    //public async Task<List<T>> ExecuteQueryAsync<T>(
    //string query,
    //Func<SqlDataReader, T> map)
    //{
    //    var result = new List<T>();

    //    try
    //    {
    //        using var connection = CreateConnection();
    //        await connection.OpenAsync();

    //        using var command = new SqlCommand(query, connection)
    //        {
    //            CommandType = CommandType.Text, // ✅ महत्वपूर्ण change
    //            CommandTimeout = 120
    //        };

    //        using var reader = await command.ExecuteReaderAsync();

    //        while (await reader.ReadAsync())
    //        {
    //            result.Add(map(reader));
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "DB query failed");
    //        throw;
    //    }

    //    return result;
    //}

    //public async Task<int> ExecuteNonQueryAsync(
    //    string storedProcedure,
    //    SqlParameter[] parameters = null)
    //{
    //    try
    //    {
    //        using var connection = CreateConnection();
    //        await connection.OpenAsync();

    //        using var command = new SqlCommand(storedProcedure, connection)
    //        {
    //            CommandType = CommandType.StoredProcedure,
    //            CommandTimeout = 120
    //        };

    //        if (parameters != null)
    //            command.Parameters.AddRange(parameters);

    //        return await command.ExecuteNonQueryAsync();
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "DB command failed");
    //        throw;
    //    }
    //}

    public async Task<List<T>> ExecuteQueryAsync<T>(string query, Func<SqlDataReader, T> map, SqlParameter[] parameters = null, CommandType commandType = CommandType.Text)
    {
        var result = new List<T>();

        using var connection = new SqlConnection(_configuration.GetConnectionString("ExternalDb"));
        await connection.OpenAsync();

        using var command = new SqlCommand(query, connection)
        {
            CommandType = commandType,
            CommandTimeout = 120
        };

        if (parameters != null)
            command.Parameters.AddRange(parameters);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(map(reader));
        }

        return result;
    }
}