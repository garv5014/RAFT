using System.Net;
using Raft_Library.Gateway.shared;
using Raft_Library.Models;

namespace Raft_Shop.Client;

public class GatewayService : IGatewayClient
{
    private HttpClient _client;

    public GatewayService(HttpClient client, GatewayApiOptions apiOptions)
    {
        _client = client;
        _client.BaseAddress = new Uri($"http://{apiOptions.ServiceName}:{apiOptions.ServicePort}");
    }

    public async Task<HttpResponseMessage> CompareAndSwap(CompareAndSwapRequest req)
    {
        var response = await _client.PostAsJsonAsync("api/Gateway/CompareAndSwap", req);
        return response;
    }

    public async Task<VersionedValue<string>> EventualGet(string key)
    {
        var response = await _client.GetFromJsonAsync<VersionedValue<string>>(
            $"api/Gateway/EventualGet?key={key}"
        );
        return response;
    }

    public async Task<VersionedValue<string>> StrongGet(string key)
    {
        var response = await _client.GetAsync($"api/Gateway/StrogGet?key={key}");

        if (response.StatusCode != HttpStatusCode.OK)
        {
            Console.WriteLine($"StrongGet failed {response.StatusCode} ");
            return null;
        }
        return await response.Content.ReadFromJsonAsync<VersionedValue<string>>();
    }
}
