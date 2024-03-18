using Raft_Library.Models;

namespace Raft_Node;

public interface IRaftNodeClient
{
    Task<VoteResponse> RequestVoteAsync(
        string address,
        int term,
        long lastLogIndex,
        int candidateId
    );
    Task<bool> RequestAppendEntriesAsync(string address, AppendEntriesRequest request);
    Task<bool> ConfirmLeaderAsync(string address, int leaderId);
    Task<bool> RequestAppendEntryAsync(
        string address,
        int currentTerm,
        string key,
        string value,
        int index
    );
}

public class RaftNodeClient : IRaftNodeClient
{
    private readonly HttpClient _client;
    private readonly ILogger<RaftNodeClient> _logger;

    public RaftNodeClient(HttpClient client, ILogger<RaftNodeClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<VoteResponse> RequestVoteAsync(
        string address,
        int term,
        long lastLogIndex,
        int candidateId
    )
    {
        var request = new VoteRequest
        {
            CandidateId = candidateId,
            Term = term,
            LastLogIndex = lastLogIndex
        };

        try
        {
            var response = await _client.PostAsJsonAsync(
                $"{address}/api/node/request-vote",
                request
            );
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<VoteResponse>()
                ?? throw new Exception("Vote response was null");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Vote request to {address} failed: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> RequestAppendEntriesAsync(string address, AppendEntriesRequest request)
    {
        try
        {
            var response = await _client.PostAsJsonAsync(
                $"{address}/api/node/append-entries",
                request
            );
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Append entries request to {address} failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ConfirmLeaderAsync(string address, int leaderId)
    {
        try
        {
            var leaderIdResponse = await _client.GetFromJsonAsync<int>(
                $"{address}/api/node/whoisleader"
            );
            return leaderId == leaderIdResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError($"leader request to {address} failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RequestAppendEntryAsync(
        string address,
        int term,
        string key,
        string value,
        int index
    )
    {
        var request = new AppendEntryRequest
        {
            Term = term,
            Key = key,
            Value = value,
            Version = index
        };

        try
        {
            var response = await _client.PostAsJsonAsync(
                $"{address}/api/node/append-entry",
                request
            );
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Append entry request to {address} failed: {ex.Message}");
            return false;
        }
    }
}
