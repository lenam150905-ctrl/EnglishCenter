using System.Data;

namespace EnglishCenter.Application.Abstractions.Persistence;

/// <summary>Điểm vào ADO.NET duy nhất cho các repository Dapper.</summary>
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
