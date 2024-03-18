using Raft_Library.Models;

namespace Raft_Library.Gateway.shared;

public interface IGatewayClient
{
    Task<VersionedValue<string>> StrongGet(string key);
    Task<VersionedValue<string>> EventualGet(string key);
    Task CompareAndSwap(CompareAndSwapRequest req);
}
