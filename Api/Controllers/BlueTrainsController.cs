using Api.SQLService;
using Microsoft.AspNetCore.Mvc;
using Model.BlueTrains;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlueTrainsController : ControllerBase
{
    private readonly IBlueTrainsRepository _blueTrainsRepository;
    private readonly ILogger<BlueTrainsController> _logger;

    public BlueTrainsController(IBlueTrainsRepository blueTrainsRepository, ILogger<BlueTrainsController> logger)
    {
        _blueTrainsRepository = blueTrainsRepository;
        _logger = logger;
    }
    
}