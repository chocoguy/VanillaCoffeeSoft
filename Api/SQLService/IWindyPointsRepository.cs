using Api.Helpers;
using Dapper;
using System.Data;
using Model.WindyPoints;
using System.Collections.Generic;

namespace Api.SQLService;

public interface IWindyPointsRepository
{
    Task<CreditCard?> GetCreditCardByIdAsync(int id);
    Task<IEnumerable<CreditCard>> SearchCreditCardsAsync(string? query = null, int page = 1, int pageSize = 10);
    Task<IEnumerable<CashbackRate>> GetCashbackRatesForCardAsync(int cardId);
    Task<IEnumerable<PointRate>> GetPointRatesForCardAsync(int cardId);
    Task<IEnumerable<Issuer>> GetAllIssuersAsync();
    Task<IEnumerable<Network>> GetAllNetworksAsync();
    Task<IEnumerable<Merchant>> GetAllMerchantsAsync();
    Task<IEnumerable<SpendCategory>> GetAllSpendCategoriesAsync();
}



public class WindyPointsRepository : IWindyPointsRepository
{
    private readonly IWindyPointsDbConnectionFactory _dbConnection;
    private readonly ILogger<WindyPointsRepository> _logger;

    public WindyPointsRepository(IWindyPointsDbConnectionFactory dbConnection, ILogger<WindyPointsRepository> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task<CreditCard?> GetCreditCardByIdAsync(int id)
    {
        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();

            const string sql = @"
SELECT
    c.*,
    n.NetworkId, n.Name, n.Picture, n.Icon, n.IconSF,
    i.IssuerId, i.Name, i.Picture
FROM WP_CreditCard c
         INNER JOIN WP_Network n ON c.NetworkKey = n.NetworkId
         INNER JOIN WP_Issuer i ON c.IssuerKey = i.IssuerId
WHERE c.CreditCardId = @Id;";

            return (await connection.QueryAsync<CreditCard, Network, Issuer, CreditCard>(
                sql,
                (card, network, issuer) =>
                {
                    card.Network = network;
                    card.Issuer = issuer;
                    return card;
                },
                new { Id = id },
                splitOn: "NetworkId,IssuerId"
            )).FirstOrDefault();
        }
        catch (Exception ex)
        {
            // Rethrow so the controller can tell a DB failure (500) apart from
            // a missing id (null → 404).
            _logger.LogError(ex, ex.Message);
            throw;
        }
    }

    public async Task<IEnumerable<CreditCard>> SearchCreditCardsAsync(string? query = null, int page = 1, int pageSize = 10)
    {
        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();

            var parameters = new DynamicParameters();
            parameters.Add("PageSize", pageSize);
            parameters.Add("Offset", (page - 1) * pageSize);

            string ftsJoin = "";
            string whereClause = "";
            string orderBy = "ORDER BY c.Name";

            var matchExpression = ToFtsMatchExpression(query);
            if (matchExpression != null)
            {
                ftsJoin = "INNER JOIN WP_CreditCardFTS f ON c.CreditCardId = f.rowid";
                whereClause = "WHERE f.WP_CreditCardFTS MATCH @SearchQuery";
                orderBy = "ORDER BY f.rank, c.Name";
                parameters.Add("SearchQuery", matchExpression);
            }

            var sql = $@"
SELECT
    c.*,
    n.NetworkId, n.Name, n.Picture, n.Icon, n.IconSF,
    i.IssuerId, i.Name, i.Picture
FROM WP_CreditCard c
         INNER JOIN WP_Network n ON c.NetworkKey = n.NetworkId
         INNER JOIN WP_Issuer i ON c.IssuerKey = i.IssuerId
         {ftsJoin}
{whereClause}
{orderBy}
LIMIT @PageSize OFFSET @Offset;";

            return await connection.QueryAsync<CreditCard, Network, Issuer, CreditCard>(
                sql,
                (card, network, issuer) =>
                {
                    card.Network = network;
                    card.Issuer = issuer;
                    return card;
                },
                parameters,
                splitOn: "NetworkId,IssuerId");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }
    }

    public async Task<IEnumerable<CashbackRate>> GetCashbackRatesForCardAsync(int cardId)
    {
        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();

            const string sql = @"
SELECT
    cr.*,
    sc.SpendCategoryId, sc.Name, sc.Icon, sc.IconSF
FROM WP_CashbackRate cr
         INNER JOIN WP_SpendCategory sc ON cr.SpendCategorykey = sc.SpendCategoryId
WHERE cr.CreditCardKey = @CardId
ORDER BY cr.CashbackMultiplier DESC, sc.Name;";

            return await connection.QueryAsync<CashbackRate, SpendCategory, CashbackRate>(
                sql,
                (rate, spendCategory) =>
                {
                    rate.SpendCategory = spendCategory;
                    return rate;
                },
                new { CardId = cardId },
                splitOn: "SpendCategoryId");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }
    }

    public async Task<IEnumerable<PointRate>> GetPointRatesForCardAsync(int cardId)
    {
        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();

            const string sql = @"
SELECT
    pr.*,
    sc.SpendCategoryId, sc.Name, sc.Icon, sc.IconSF
FROM WP_PointRate pr
         INNER JOIN WP_SpendCategory sc ON pr.SpendCategorykey = sc.SpendCategoryId
WHERE pr.CreditCardKey = @CardId
ORDER BY pr.EffectiveCashback DESC, sc.Name;";

            return await connection.QueryAsync<PointRate, SpendCategory, PointRate>(
                sql,
                (rate, spendCategory) =>
                {
                    rate.SpendCategory = spendCategory;
                    return rate;
                },
                new { CardId = cardId },
                splitOn: "SpendCategoryId");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }
    }

    public async Task<IEnumerable<Issuer>> GetAllIssuersAsync()
    {
        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();
            return await connection.QueryAsync<Issuer>("SELECT * FROM WP_Issuer ORDER BY Name;");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }
    }

    public async Task<IEnumerable<Network>> GetAllNetworksAsync()
    {
        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();
            return await connection.QueryAsync<Network>("SELECT * FROM WP_Network ORDER BY Name;");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }
    }

    public async Task<IEnumerable<Merchant>> GetAllMerchantsAsync()
    {
        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();

            const string sql = @"
SELECT
    m.MerchantId, m.Name,
    sc.SpendCategoryId, sc.Name, sc.Icon, sc.IconSF
FROM WP_Merchant m
         INNER JOIN WP_SpendCategory sc ON m.SpendCategoryKey = sc.SpendCategoryId
ORDER BY m.Name;";

            return await connection.QueryAsync<Merchant, SpendCategory, Merchant>(
                sql,
                (merchant, spendCategory) =>
                {
                    merchant.SpendCategory = spendCategory;
                    return merchant;
                },
                splitOn: "SpendCategoryId");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }
    }

    public async Task<IEnumerable<SpendCategory>> GetAllSpendCategoriesAsync()
    {
        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();
            return await connection.QueryAsync<SpendCategory>("SELECT * FROM WP_SpendCategory ORDER BY Name;");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }
    }

    // FTS5 treats ", :, -, AND, etc. as query syntax, so raw user input like
    // "Bilt Rewards - 2x" is a syntax error. Quoting each whitespace-separated
    // token (with embedded quotes doubled) makes the input match literally.
    private static string? ToFtsMatchExpression(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        var tokens = query
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => "\"" + t.Replace("\"", "\"\"") + "\"");

        var expression = string.Join(" ", tokens);
        return expression.Length == 0 ? null : expression;
    }
}
