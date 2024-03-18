using Raft_Library.Gateway.shared;
using Raft_Library.Models;

namespace Raft_Shop;

public class GatewayService : IGatewayClient
{
    public Task CompareAndSwap(CompareAndSwapRequest req)
    {
        throw new NotImplementedException();
    }

    public Task<VersionedValue<string>> EventualGet(string key)
    {
        throw new NotImplementedException();
    }

    public Task<VersionedValue<string>> StrongGet(string key)
    {
        throw new NotImplementedException();
    }
}
