using Microsoft.Data.Sqlite;
using System.Data;

namespace Api.SQLService;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public abstract class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    protected SqliteConnectionFactory(IConfiguration configuration, string connectionStringName)
    {
        _connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' not set");
    }

    public IDbConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }
}