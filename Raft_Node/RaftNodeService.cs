using Raft_Node.Options;

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
    public Guid MostRecentLeader { get; set; }

    public RaftNodeService(HttpClient client, ILogger<RaftNodeService> logger, ApiOptions options)
    {
        for (int i = 1; i <= options.NodeCount; i++)
        {
            if (i == options.NodeIdentifier)
            {
                continue;
            }
            otherNodeAddresses.Add($"http://node{i}:{options.NodeServicePort}");
        }
    }
    /*
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
    public void Initialize()
    {
        IsAlive = true;
        MakeTimeoutThread();
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

    // Keep track of time out and start election if needed or send heart beats if leader
    public void Update()
    {
        if (!IsAlive)
        {
            return;
        }

        if (DateTime.UtcNow - LastHeartbeat > TimeSpan.FromMilliseconds(ElectionTimeout))
        {
            StartElection();
        }
        else if (State == RaftNodeState.Leader)
        {
            SendHeartbeats();
        }
    }

    public void MakeTimeoutThread()
    {
        new Thread(() =>
        {
            while (IsAlive)
            {
                if (State == RaftNodeState.Leader)
                {
                    Task.Delay(100).Wait();
                    SendHeartbeats();
                }
                else if (ElectionTimedOut())
                {
                    WriteToLog($"Node {Name} timed out");
                    StartElection();
                }
            }
        }).Start();
    }

    public void StartElection()
    {
        State = RaftNodeState.Candidate;
        Term++;
        VotedFor = Name;
        WriteToLog($"Node {Name} started election for term {Term}");
        UpdateElectionTimer();
        int votes = 1;
        foreach (var node in OtherNodes)
        {
            if (node.Name != Name)
            {
                new Thread(() =>
                {
                    if (node.ReceiveVoteRequest(Term, Name))
                    {
                        votes++;
                    }
                    else
                    {
                        WriteToLog($"Node {node.Name} did not vote for {Name} in term {Term}");
                    }
                }).Start();
            }
        }
        if (votes >= Math.Floor((double)(OtherNodes.Count / 2)))
        {
            State = RaftNodeState.Leader;
            WriteToLog($"Node {Name} became leader for term {Term}");
            SendHeartbeats();
        }
        else
        {
            votes = 0;
            State = RaftNodeState.Follower;
        }
    }

    public void AppendEntries(int term, Guid leaderId, List<(string key, string value)> entries)
    {
        if (term >= Term)
        {
            Term = term;
            State = RaftNodeState.Follower;
            LastHeartbeat = DateTime.UtcNow;
            MostRecentLeader = leaderId;

            foreach (var entry in entries)
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

            WriteToLog($"Appended entries from {leaderId}");
        }
    }


    private bool ElectionTimedOut()
    {
        return DateTime.UtcNow - LastHeartbeat > TimeSpan.FromMilliseconds(ElectionTimeout);
    }

    // send heartbeats to all other nodes
    private void SendHeartbeats()
    {
        if (!IsAlive)
        {
            return;
        }

        // Example of deciding what entries to send - this will depend on your application logic
        List<(string key, string value)> entriesToSend = new List<(string key, string value)>();

        // Determine the entries to send based on the last log index acknowledged by each follower, etc.
        // This is simplified; in practice, you'd track what each follower has and send them what they need.

        foreach (var node in OtherNodes)
        {
            if (node.Name != Name)
            {
                // If there are no new entries, this effectively acts as a heartbeat
                node.AppendEntries(Term, Name, entriesToSend);
            }
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

    */
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
    }
}

public enum RaftNodeState
{
    Follower,
    Candidate,
    Leader
}