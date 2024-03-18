using Raft_Library.Gateway.shared;
using Raft_Library.Models;

namespace Raft_Shop.Client;

public class GatewayService : IGatewayClient
{
    private HttpClient _client;

    public GatewayService(HttpClient client)
    {
        _client = client;
    }

    public async Task CompareAndSwap(CompareAndSwapRequest req)
    {
        throw new NotImplementedException();
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
