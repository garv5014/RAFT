namespace Raft_Node;

public class RaftNodeService : BackgroundService
{
    private RaftNodeState State = RaftNodeState.Follower;

    public int Term;
    private Guid VotedFor { get; set; }
    public Guid Name { get; set; }

    private Random Random;

    private List<string> otherNodeAddresses { get; set; }

    private string FilePath;

    private int ElectionTimeout;

    private DateTime LastHeartbeat;

    private bool IsAlive;

    private int TimeFactor = 1;

    public Dictionary<string, (string value, int logIndex)> Log = new Dictionary<string, (string, int)>();
    private readonly HttpClient client;
    private readonly ILogger<RaftNodeService> logger;
    private readonly ApiOptions options;

    public Guid MostRecentLeader { get; set; }

    public RaftNodeService(HttpClient client, ILogger<RaftNodeService> logger, ApiOptions options)
    {
        otherNodeAddresses = new List<string>();
        for (int i = 1; i <= options.NodeCount; i++)
        {
            if (i == options.NodeIdentifier)
            {
                continue;
            }
            otherNodeAddresses.Add($"http://{options.NodeServiceName}{i}:{options.NodeServicePort}");
        }
        Term = 0;
        this.client = client;
        this.logger = logger;
        this.options = options;
        FilePath = $"{options.EntryLogPath}";
        this.Random = new Random();
        VotedFor = Guid.Empty;
        Name = Guid.NewGuid();
        State = RaftNodeState.Follower;
        IsAlive = false;
    }

    private void UpdateElectionTimer()
    {
        ElectionTimeout = Random.Next(150, 300) * TimeFactor;
        LastHeartbeat = DateTime.UtcNow;
    }

    public bool isHealthy()
    {
        return IsAlive;
    }

    public RaftNodeState GetState()
    {
        return State;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IsAlive = true;
        LastHeartbeat = DateTime.UtcNow;
        while (!stoppingToken.IsCancellationRequested)
        {
            if (State == RaftNodeState.Leader)
            {
                Task.Delay(100).Wait();
                SendHeartbeats();
            }
            else if (ElectionTimedOut())
            {
                WriteToLog("Election timed out.");
                StartElection();
            }
        }
    }

    public async void StartElection()
    {
        State = RaftNodeState.Candidate;
        Term++;
        VotedFor = Name;
        WriteToLog($"Node {Name} started election for term {Term}");
        UpdateElectionTimer();
        int votes = 1;
        foreach (var node in otherNodeAddresses)
        {
            var res = await ReceiveVoteRequestAsync(node);

            if (res != null && res.VotedId != options.NodeIdentifier)
            {
                if (res.VoteGranted)
                {
                    votes++;
                }
                else
                {
                    WriteToLog($"Node {options.NodeIdentifier} did not vote for {Name} in term {Term}");
                }
            }
        }
        if (votes >= Math.Floor((double)(otherNodeAddresses.Count / 2)))
        {
            State = RaftNodeState.Leader;
            MostRecentLeader = Name;
            WriteToLog($"Node {Name} became leader for term {Term}");
            SendHeartbeats();
        }
        else
        {
            votes = 0;
            State = RaftNodeState.Follower;
        }
    }

    private async Task<int> getIdFromNode(string node)
    {
        try
        {
            logger.LogInformation($"Getting id from node {node} base address {client.BaseAddress}");
            return int.Parse(await client.GetStringAsync($"{node}/api/node/identify"));
        }
        catch (Exception)
        {
            return -1;
        }
    }

    private void WriteToLog(string entry)
    {
        using (var stream = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.Write))
        using (var writer = new StreamWriter(stream))
        {
            writer.WriteLine($"{DateTime.UtcNow}:{entry}");
        }
    }

    private bool ElectionTimedOut()
    {
        return DateTime.UtcNow - LastHeartbeat > TimeSpan.FromMilliseconds(ElectionTimeout);
    }

    // send heartbeats to all other nodes
    private async void SendHeartbeats()
    {
        if (!IsAlive)
        {
            return;
        }

        // Example of deciding what entries to send - this will depend on your application logic
        List<(string key, string value)> entriesToSend = new List<(string key, string value)>();

        // Determine the entries to send based on the last log index acknowledged by each follower, etc.
        // This is simplified; in practice, you'd track what each follower has and send them what they need.

        foreach (var node in otherNodeAddresses)
        {
            var nodeId = await getIdFromNode(node);
            if (nodeId != options.NodeIdentifier)
            {
                try
                {
                    await client.PostAsJsonAsync($"{node}/api/node/appendEntries", new AppendEntriesRequest
                    {
                        Term = Term,
                        LeaderId = Name,
                        Entries = entriesToSend
                    });
                }
                catch (Exception)
                {
                }
                // If there are no new entries, this effectively acts as a heartbeat
            }
        }
    }

    public async Task<VoteResponse> ReceiveVoteRequestAsync(string nodeAddress)
    {
        logger.LogInformation($"Sending vote request to {nodeAddress}/api/node/receiveVoteRequest?term={Term}&voteForName={Name}");
        try
        {
            var res = await client.GetAsync($"{nodeAddress}/api/node/receiveVoteRequest?term={Term}&voteForName={Name}");
            if (res.IsSuccessStatusCode)
            {
                var voteResponse = await res.Content.ReadFromJsonAsync<VoteResponse>();
                if (voteResponse != null)
                {
                    return voteResponse;
                }
            }
        }
        catch (Exception)
        {

        }
        return null;
    }

    // Receive vote request return bool if vote is granted
    public bool ReceiveVoteRequest(int term, Guid candidateId)
    {
        if (!IsAlive)
        {
            return false;
        }

        if (term > Term || ((VotedFor == Guid.Empty || VotedFor == candidateId) && term == Term))
        {
            Term = term;
            VotedFor = candidateId;
            State = RaftNodeState.Follower;
            WriteToLog($"Voted for {candidateId} the term is {term}");
            return true;
        }
        WriteToLog($"Did not vote for {candidateId} in term {term}");
        return false;
    }

    public void AppendEntries(AppendEntriesRequest req)
    {
        if (req.Term >= Term)
        {
            Term = req.Term;
            State = RaftNodeState.Follower;
            LastHeartbeat = DateTime.UtcNow;
            MostRecentLeader = req.LeaderId;

            foreach (var entry in req.Entries)
            {
                if (!Log.ContainsKey(entry.key))
                {
                    Log.Add(entry.key, (entry.value, Log.Count));
                }
                else
                {
                    Log[entry.key] = (entry.value, Log[entry.key].logIndex); // Update if key exists
                }
            }

            WriteToLog($"Appended entries from {req.LeaderId}");
        }
    }
    // Receive a heartbeat from a leader 
    public void ReceiveHeartbeat(int term, Guid leaderId)
    {
        if (term >= Term)
        {
            Term = term;
            State = RaftNodeState.Follower;
            LastHeartbeat = DateTime.UtcNow;
            MostRecentLeader = leaderId; // Update the most recent leader
            WriteToLog($"Received heartbeat from {leaderId}");
        }
    }

    // Function that "Kills" the node for testing
    public void KillNode()
    {
        IsAlive = false;
    }

    // Function that "Revives" the node for testing
    public void ReviveNode()
    {
        IsAlive = true;
    }

}

public enum RaftNodeState
{
    Follower,
    Candidate,
    Leader
}