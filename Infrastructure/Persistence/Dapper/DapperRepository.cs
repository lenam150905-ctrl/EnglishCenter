using System.Data;
using Dapper;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

/// <summary>Dapper ánh xạ dữ liệu; ADO.NET quản lý connection/transaction.</summary>
public abstract class DapperRepository(IDbConnectionFactory connectionFactory)
{
    protected IDbConnectionFactory ConnectionFactory => connectionFactory;

    protected IDbConnection CreateConnection() => connectionFactory.CreateConnection();

    protected async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    protected async Task<int> ExecuteAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    protected async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return result.AsList();
    }

    protected async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    protected async Task<IReadOnlyList<TReturn>> QueryAsync<T1, T2, TReturn>(
        string sql,
        Func<T1, T2, TReturn> map,
        object? parameters = null,
        string splitOn = "Id",
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var result = await connection.QueryAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken),
            map,
            splitOn);
        return result.AsList();
    }

    protected async Task<IReadOnlyList<TReturn>> QueryAsync<T1, T2, T3, TReturn>(
        string sql,
        Func<T1, T2, T3, TReturn> map,
        object? parameters = null,
        string splitOn = "Id",
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var result = await connection.QueryAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken),
            map,
            splitOn);
        return result.AsList();
    }

    protected async Task<IReadOnlyList<TReturn>> QueryAsync<T1, T2, T3, T4, TReturn>(
        string sql,
        Func<T1, T2, T3, T4, TReturn> map,
        object? parameters = null,
        string splitOn = "Id",
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var result = await connection.QueryAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken),
            map,
            splitOn);
        return result.AsList();
    }
}
