using Microsoft.AspNetCore.Mvc;
using Raft_Gateway.Options;
using Raft_Library;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Raft_Gateway.controllers;

[Route("api/[controller]")]
[ApiController]
public class GatewayController : ControllerBase
{
    private readonly ILogger<GatewayController> _logger;
    private readonly ApiOptions _options;
    private readonly HttpClient client;
    private List<string> nodeAddresses = new List<string>();

    public GatewayController(
        ILogger<GatewayController> logger,
        ApiOptions options,
        HttpClient client
    )
    {
        _logger = logger;
        _options = options;
        this.client = client;
        InitializeNodeAddresses(options);
    }

    private void InitializeNodeAddresses(ApiOptions options) // Thanks Caleb
    {
        for (var i = 1; i <= options.NodeCount; i++)
        {
            nodeAddresses.Add($"http://{options.NodeServiceName}{i}:{options.NodeServicePort}");
        }
    }

    // GET api/<GatewayController>/5
    [HttpGet("{id}")]
    public async Task<string> Get(int id)
    {
        client.BaseAddress = new Uri(nodeAddresses[id]);

        var response = await client.GetAsync("api/Node");
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Request to node {id} successful", id);
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            _logger.LogError("Request to node {id} failed", id);
        }
        return "Request failed";
    }

    [HttpGet("StrongGet")]
    public async Task<ActionResult<VersionedValue<string>>> StrongGet([FromQuery] string key)
    {
        _logger.LogInformation("StrongGet called with key: {key}", key);
        var leaderAddress = FindLeaderAddress();
        _logger.LogInformation($"Leader address: {leaderAddress}/api/node/strongget?key={key}");
        var value = await client.GetFromJsonAsync<VersionedValue<string>>(
            $"{leaderAddress}/api/node/strongget?key={key}"
        );
        if (value == null)
        {
            return StatusCode(404, "Value not found in leader node.");
        }
        return value;
    }

    [HttpGet("EventualGet")]
    public async Task<ActionResult<VersionedValue<string>>> EventualGet([FromQuery] string key)
    {
        _logger.LogInformation("EventualGet called with key: {key}", key);
        var leaderAddress = FindLeaderAddress();
        var value = await client.GetFromJsonAsync<VersionedValue<string>>(
            $"{leaderAddress}/api/node/eventualget?key={key}"
        );
        if (value == null)
        {
            return StatusCode(404, "Value not found");
        }
        return value;
    }

    [HttpPost("CompareAndSwap")]
    public async Task<ActionResult> CompareAndSwap(CompareAndSwapRequest request)
    {
        _logger.LogInformation(
            "CompareAndSwap called with key: {key}, oldValue: {oldValue}, newValue: {newValue}",
            request.Key,
            request.OldValue,
            request.NewValue
        );
        var leaderAddress = FindLeaderAddress();
        var response = await client.PostAsJsonAsync(
            $"{leaderAddress}/api/Node/CompareAndSwap",
            request
        );
        response.EnsureSuccessStatusCode();
        return Ok();
    }

    private string FindLeaderAddress()
    {
        var address = GetRandomNodeAddress();

        var leaderId = 0;
        while (leaderId == 0)
        {
            leaderId = GetLeaderId(address).Result;
            if (leaderId == 0)
            {
                address = GetRandomNodeAddress();
            }
        }

        return nodeAddresses.First(a => a.Contains(leaderId.ToString()));
    }

    private async Task<int> GetLeaderId(string address, int maxRetries = 3)
    {
        var retryCount = 0;
        while (retryCount < maxRetries)
        {
            try
            {
                _logger.LogInformation(
                    $"Attempt {retryCount + 1}: Getting leader id from {address}/api/Node/whoisleader"
                );
                var response = await client.GetAsync($"{address}/api/Node/whoisleader");
                response.EnsureSuccessStatusCode();
                var leaderId = await response.Content.ReadAsStringAsync();
                return int.Parse(leaderId);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    $"Attempt {retryCount + 1}: Error getting leader id from {address}"
                );
                retryCount++;
                await Task.Delay(1000 * retryCount); // Wait 1, 2, 3 seconds between retries
            }
        }
        return 0;
    }

    private string GetRandomNodeAddress()
    {
        var random = new Random();
        int randomIndex = random.Next(0, nodeAddresses.Count);
        _logger.LogInformation(
            $"here are the number of node addresses {nodeAddresses.Count} here is the random {randomIndex}"
        );
        return nodeAddresses[randomIndex];
    }
}
