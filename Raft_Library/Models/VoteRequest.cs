namespace Raft_Library.Models;

public class VoteRequest
{
    public int CandidateId { get; set; }
    public int Term { get; set; }
    public long LastLogIndex { get; set; }
}
