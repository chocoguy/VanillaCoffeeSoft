namespace Api.SQLService;

public interface IBlueTrainsRepository
{

}

public class BlueTrainsRepository : IBlueTrainsRepository
{
    private readonly IBlueTrainsStaticDbConnectionFactory _dbConnection;
    private readonly ILogger<BlueTrainsRepository> _logger;

    public BlueTrainsRepository(IBlueTrainsStaticDbConnectionFactory dbConnection,
        ILogger<BlueTrainsRepository> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }
}
