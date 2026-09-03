namespace Api.SQLService;

public interface IAniTrakDbConnectionFactory : IDbConnectionFactory;

public class AniTrakDbConnectionFactory(IConfiguration configuration)
    : SqliteConnectionFactory(configuration, "DefaultConnection"), IAniTrakDbConnectionFactory;
