using System.Net;
using System.Net.Http.Json;
using Raft_Library.Gateway.shared;
using Raft_Library.Models;

namespace Raft_Library.Shop.shared.Services;

public class GatewayService : IGatewayClient
{
    private HttpClient _client;

    public GatewayService(HttpClient client)
    {
        _client = client;
    }

    public async Task<HttpResponseMessage> CompareAndSwap(CompareAndSwapRequest req)
    {
        var response = await _client.PostAsJsonAsync("api/Gateway/CompareAndSwap", req);
        return response;
    }

    public async Task<VersionedValue<string>> EventualGet(string key)
    {
        var response = await _client.GetAsync($"api/Gateway/EventualGet?key={key}");

        if (response.StatusCode != HttpStatusCode.OK)
        {
            Console.WriteLine($"EventualGet failed {response.StatusCode} ");
            return null;
        }
        return await response.Content.ReadFromJsonAsync<VersionedValue<string>>();
    }

    public async Task<VersionedValue<string>> StrongGet(string key)
    {
        Console.WriteLine($"StrongGet {_client.BaseAddress}");
        var response = await _client.GetAsync($"api/Gateway/StrongGet?key={key}");

        if (response.StatusCode != HttpStatusCode.OK)
        {
            Console.WriteLine($"StrongGet failed {response.StatusCode} ");
            return null;
        }
        return await response.Content.ReadFromJsonAsync<VersionedValue<string>>();
    }
}
