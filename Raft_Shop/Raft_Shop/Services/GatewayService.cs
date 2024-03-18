using Raft_Library.Gateway.shared;
using Raft_Library.Models;
using Raft_Shop.Options;

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
        throw new NotImplementedException();
    }

    public async Task<VersionedValue<string>> StrongGet(string key)
    {
        throw new NotImplementedException();
    }
}
