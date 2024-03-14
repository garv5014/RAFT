namespace Raft_Node;

public class VoteResponse
{
    public bool VoteGranted { get; set; }
    public int VotedId { get; set; }
}
