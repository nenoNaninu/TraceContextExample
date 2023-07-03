using Microsoft.AspNetCore.Mvc;

namespace ServiceC.Controllers;

[ApiController]
public class ThirdController : ControllerBase
{
    private readonly ILogger<ThirdController> _logger;

    public ThirdController(ILogger<ThirdController> logger)
    {
        _logger = logger;
    }

    [HttpGet("/third")]
    public string Get()
    {
        _logger.LogInformation("ThirdController.Get()");

        return "third";
    }
}
