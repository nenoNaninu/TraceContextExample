using Microsoft.AspNetCore.Mvc;

namespace ServiceB.Controllers;

[ApiController]
public class SecondController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SecondController> _logger;

    public SecondController(IHttpClientFactory httpClientFactory, ILogger<SecondController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("/second")]
    public async Task<string> Get()
    {
        _logger.LogInformation("SecondController.Get()");

        var client = _httpClientFactory.CreateClient("Http2Client");

        var response = await client.GetAsync("http://localhost:5002/third");
        var str = await response.Content.ReadAsStringAsync();

        return $"second__{str}";
    }
}
