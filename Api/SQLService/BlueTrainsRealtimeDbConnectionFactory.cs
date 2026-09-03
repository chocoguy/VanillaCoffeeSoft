namespace Api.SQLService;

public interface IBlueTrainsRealtimeDbConnectionFactory : IDbConnectionFactory;

public class BlueTrainsRealtimeDbConnectionFactory(IConfiguration configuration)
    : SqliteConnectionFactory(configuration, "BlueTrainsRealtime"), IBlueTrainsRealtimeDbConnectionFactory;
