namespace RAFT;

public class RaftNode
{
    private RaftNodeState State = RaftNodeState.Follower;

    public int Term;
    private Guid VotedFor { get; set; }
    public Guid Name { get; set; }

    private Random Random;

    private List<RaftNode> OtherNodes { get; set; }

    private string FilePath;

    private int ElectionTimeout;

    private DateTime LastHeartbeat;

    private bool IsAlive;

    private int TimeFactor = 1;

    public RaftNode(List<RaftNode> otherNodes, int timeFactor = 1, Random? seeded = null)
    {
        if (seeded != null)
        {
            Random = seeded;
        }
        else
        {
            Random = new Random();
        }
        Term = 0;
        VotedFor = Guid.Empty;
        State = RaftNodeState.Follower;
        Name = Guid.NewGuid();
        FilePath = $"{Name}.log";
        OtherNodes = otherNodes;
        UpdateElectionTimer();
        IsAlive = false;
        TimeFactor = timeFactor;
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
        foreach (var node in OtherNodes)
        {
            if (node.Name != Name)
            {
                node.ReceiveHeartbeat(Term, Name);
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
}

public enum RaftNodeState
{
    Follower,
    Candidate,
    Leader
}