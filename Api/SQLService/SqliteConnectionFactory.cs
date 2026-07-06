using Microsoft.Data.Sqlite;
using System.Data;

namespace Api.SQLService;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not set");
    }

    public IDbConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }
    
    
}