namespace Api.SQLService;

public interface IWindyPointsDbConnectionFactory : IDbConnectionFactory;

public class WindyPointsDbConnectionFactory(IConfiguration configuration)
    : SqliteConnectionFactory(configuration, "WindyPoints"), IWindyPointsDbConnectionFactory;
