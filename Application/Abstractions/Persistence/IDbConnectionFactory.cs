using System.Data;

namespace EnglishCenter.Application.Abstractions.Persistence;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

