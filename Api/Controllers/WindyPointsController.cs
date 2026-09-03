using Api.SQLService;
using Microsoft.AspNetCore.Mvc;
using Model.WindyPoints;


namespace Api.Controllers;


[ApiController]
[Route("api/[controller]")]
public class WindyPointsController : ControllerBase
{
    private readonly IWindyPointsRepository _windyPointsRepository;
    private readonly ILogger<WindyPointsController> _logger;

    public WindyPointsController(IWindyPointsRepository windyPointsRepository, ILogger<WindyPointsController> logger)
    {
        _windyPointsRepository = windyPointsRepository;
        _logger = logger;
    }

    [HttpGet("CreditCard/{cardId}")]
    public async Task<ActionResult<CreditCard>> GetCreditCardById(int cardId)
    {
        try
        {
            var card = await _windyPointsRepository.GetCreditCardByIdAsync(cardId);

            if (card == null)
                return NotFound();

            return Ok(card);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("CreditCard/search")]
    public async Task<ActionResult<IEnumerable<CreditCard>>> SearchCreditCards(string? q = null, int page = 1, int pageSize = 10)
    {
        try
        {
            var cards = await _windyPointsRepository.SearchCreditCardsAsync(q, page, pageSize);

            _logger.LogInformation($"Found {cards.Count()} credit cards for query '{q}'");
            return Ok(cards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("CreditCard/{cardId}/cashbackrates")]
    public async Task<ActionResult<IEnumerable<CashbackRate>>> GetCashbackRatesForCard(int cardId)
    {
        try
        {
            var rates = await _windyPointsRepository.GetCashbackRatesForCardAsync(cardId);
            return Ok(rates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("CreditCard/{cardId}/pointrates")]
    public async Task<ActionResult<IEnumerable<PointRate>>> GetPointRatesForCard(int cardId)
    {
        try
        {
            var rates = await _windyPointsRepository.GetPointRatesForCardAsync(cardId);
            return Ok(rates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("Issuers")]
    public async Task<ActionResult<IEnumerable<Issuer>>> GetAllIssuers()
    {
        try
        {
            return Ok(await _windyPointsRepository.GetAllIssuersAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("Networks")]
    public async Task<ActionResult<IEnumerable<Network>>> GetAllNetworks()
    {
        try
        {
            return Ok(await _windyPointsRepository.GetAllNetworksAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("Merchants")]
    public async Task<ActionResult<IEnumerable<Merchant>>> GetAllMerchants()
    {
        try
        {
            return Ok(await _windyPointsRepository.GetAllMerchantsAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("SpendCategories")]
    public async Task<ActionResult<IEnumerable<SpendCategory>>> GetAllSpendCategories()
    {
        try
        {
            return Ok(await _windyPointsRepository.GetAllSpendCategoriesAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }
}
