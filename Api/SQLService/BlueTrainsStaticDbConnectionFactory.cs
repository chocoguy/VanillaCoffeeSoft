namespace Api.SQLService;

public interface IBlueTrainsStaticDbConnectionFactory : IDbConnectionFactory;

public class BlueTrainsStaticDbConnectionFactory(IConfiguration configuration)
    : SqliteConnectionFactory(configuration, "BlueTrainsStatic"), IBlueTrainsStaticDbConnectionFactory;
