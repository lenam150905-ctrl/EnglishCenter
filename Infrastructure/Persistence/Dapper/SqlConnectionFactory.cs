using System.Data;
using EnglishCenter.Application.Abstractions.Persistence;
using Microsoft.Data.SqlClient;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class SqlConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection chưa được cấu hình.");

        return new SqlConnection(connectionString);
    }
}
