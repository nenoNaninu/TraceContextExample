using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ServiceA.Controllers;

[ApiController]
public class FirstController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FirstController> _logger;

    public FirstController(IHttpClientFactory httpClientFactory, ILogger<FirstController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("/first")]
    public async Task<string> Get()
    {
        _logger.LogInformation("FirstController.Get()");

        var client = _httpClientFactory.CreateClient("Http2Client");

        var response = await client.GetAsync("http://localhost:5159/second");
        var str = await response.Content.ReadAsStringAsync();

        return $"first__{str}";
    }
}
