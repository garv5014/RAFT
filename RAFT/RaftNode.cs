namespace RAFT;

// 


public class RaftNode
{
    public RaftNodeState State { get; set; }
    public int Term { get; set; }
    public Guid VotedFor { get; set; }
    public Guid Name { get; set; }

    public System.Timers.Timer ElectionTimer { get; set; }
    public RaftNode()
    {
        Term = 0;
        VotedFor = Guid.Empty;
        State = RaftNodeState.Follower;
        Name = Guid.NewGuid();
        var ran = new Random();
        ElectionTimer = new System.Timers.Timer(ran.Next(150, 300));
    }

    public void StartElection()
    {
        State = RaftNodeState.Candidate;
        Term++;
        VotedFor = Name;
        ElectionTimer.Start();
    }

    public void IncreaseTimer()
    {
        var ran = new Random();
        ElectionTimer.Interval = ran.Next(150, 300);
    }
}

public enum RaftNodeState
{
    Follower,
    Candidate,
    Leader
}