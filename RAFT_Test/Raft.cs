using RAFT;
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace RAFT_Test;

public class RaftTestsBase
{
    internal List<RaftNode> testNodes = [];
    public RaftTestsBase(int numberOfNodes, int timeDelay = 1)
    {
        for (int i = 0; i < numberOfNodes; i++)
        {
            var node = new RaftNode(testNodes, timeDelay, new Random(0));
            testNodes.Add(node);
        }
    }

    internal void StartNodes()
    {
        foreach (var node in testNodes)
        {
            node.Initialize();
        }
    }
    public static bool IsLeader(RaftNode n)
    {
        return n.GetState() == RaftNodeState.Leader && n.isHealthy();
    }

    internal void KillFirstXNodes(int numberNodesToKill)
    {
        for (int i = 0; i < numberNodesToKill; i++)
        {
            testNodes[i].KillNode();
        }
    }

    internal void KillFirstXFollowers(int numberNodesToKill)
    {
        var followers = testNodes.Where(n => n.GetState() == RaftNodeState.Follower && n.isHealthy()).ToList();

        if (followers.Count < numberNodesToKill) throw new Exception("no followers to kill obtain more followers");

        for (int i = 0; i < numberNodesToKill; i++)
        {
            followers[i].KillNode();
        }
    }

    internal async Task WaitForLeaderElected()
    {
        int count = 0;
        while (testNodes.Count(node => IsLeader(node)) == 0)
        {
            if (count > 10)
            {
                Assert.Fail("Leader election took too long.");
            }
            await Task.Delay(1000);
            count++;
        }
    }
    // Add additional test methods here for each scenario you described.
}

public class TwoOfThree : RaftTestsBase
{
    public TwoOfThree() : base(3)
    {
    }

    [Fact]
    public async void LeaderIsElectedWithMajorityNodes()
    {
        StartNodes();
        KillFirstXNodes(1);
        await WaitForLeaderElected();
        int leaderCount = testNodes.Count(n => IsLeader(n));
        Assert.Equal(1, leaderCount); // Assert that one leader is elected.
        int notLeaderCount = testNodes.Count(n => !IsLeader(n));
        Assert.Equal(2, notLeaderCount); // Assert that one leader is elected.
    }
}

public class ThreeOfFiveHealthy : RaftTestsBase
{
    public ThreeOfFiveHealthy() : base(5)
    {
    }

    [Fact]
    public async void LeaderIsElectedWithMajorityNodes()
    {
        StartNodes();

        // You may need to wait a bit here to allow the election process to complete.
        KillFirstXNodes(2);
        await WaitForLeaderElected();
        int leaderCount = testNodes.Count(n => IsLeader(n));
        Assert.Equal(1, leaderCount); // Assert that one leader is elected.
        int notLeaderCount = testNodes.Count(n => !IsLeader(n));
        Assert.Equal(4, notLeaderCount); // Assert that one leader is elected.
    }
}

public class ThreeOfFiveUnhealthy : RaftTestsBase
{
    public ThreeOfFiveUnhealthy() : base(5)
    {
    }

    [Fact]
    public async void LeaderIsNOTElected()
    {
        StartNodes();
        KillFirstXNodes(3);
        Task.Delay(300).Wait();
        int leaderCount = testNodes.Count(n => IsLeader(n));
        Assert.Equal(0, leaderCount); // Assert that one leader is elected.
    }
}

public class LeaderStaysLeader : RaftTestsBase
{
    public LeaderStaysLeader() : base(5)
    {
    }

    [Fact]
    public async void LeaderIsElectedAndStays()
    {
        StartNodes();
        await WaitForLeaderElected();
        int leaderCount = testNodes.Count(n => IsLeader(n));
        var leader = testNodes.First(n => IsLeader(n));
        Assert.Equal(1, leaderCount); // Assert that one leader is elected.
        Task.Delay(300).Wait();
        Assert.Equal(RaftNodeState.Leader, leader.GetState());
    }
}

public class NodeCallForElection : RaftTestsBase
{
    public NodeCallForElection() : base(3)
    {
    }

    [Fact]
    public async void NodeCallsForElection()
    {
        StartNodes();
        await WaitForLeaderElected();
        var firstLeader = testNodes.First(n => IsLeader(n));
        firstLeader.KillNode();
        await WaitForLeaderElected();
        var newLeader = testNodes.First(n => IsLeader(n));
        Assert.NotEqual(firstLeader, newLeader);
        Assert.Equal(RaftNodeState.Leader, newLeader.GetState());
    }
}

public class NodeContinuesAsLeaderAfterTwoFailures : RaftTestsBase
{
    public NodeContinuesAsLeaderAfterTwoFailures() : base(5)
    {
    }

    [Fact]
    public async void NodeCallsLeaderContinues()
    {
        StartNodes();
        await WaitForLeaderElected();
        var firstLeader = testNodes.First(n => IsLeader(n));
        KillFirstXFollowers(2);
        await WaitForLeaderElected();
        var newLeader = testNodes.First(n => IsLeader(n));
        Assert.Equal(RaftNodeState.Leader, newLeader.GetState());
        Assert.Equal(firstLeader, newLeader);
    }
}


public class ComplexScenario : RaftTestsBase
{
    public ComplexScenario() : base(5)
    {
    }
    [Fact]
    public async void Complex()
    {
        var nodeA = testNodes[0];
        var nodeB = testNodes[1];
        var nodeC = testNodes[2];
        var nodeD = testNodes[3];
        var nodeE = testNodes[4];
        //  Node A starts an election and gets votes from B, C, and D and then B, C, and D get rebooted.
        //  Meanwhile node E starts an election in the same term and asks B, C, and D for their vote.
        //  Node E should not get votes from B, C and D since they already gave their votes before reboot.
        nodeA.ReviveNode();
        nodeB.ReviveNode();
        nodeC.ReviveNode();
        nodeD.ReviveNode();
        nodeE.ReviveNode();
        nodeA.Initialize();
        nodeB.Initialize();
        nodeC.Initialize();
        nodeD.Initialize();
        nodeA.StartElection();
        await Task.Delay(1000);
        nodeB.KillNode();
        nodeC.KillNode();
        nodeD.KillNode();
        nodeE.Initialize();
        nodeB.ReviveNode();
        nodeC.ReviveNode();
        nodeD.ReviveNode();
        nodeE.Term = nodeA.Term - 1; // Node E starts an election in the same term because E will increment the term when it starts an election.
        nodeE.StartElection();

        Assert.True(IsLeader(nodeA));
    }
}

public class BasicLogReplication : RaftTestsBase
{
    public BasicLogReplication() : base(3)
    {
    }

    [Fact]
    public async void LogReplicationOccursAcrossAllNodes()
    {
        StartNodes();
        await WaitForLeaderElected();


        var leader = testNodes.FirstOrDefault(n => IsLeader(n));
        Assert.NotNull(leader);


        leader.Term++;


        await Task.Delay(1000);

        foreach (var node in testNodes.Where(n => !IsLeader(n)))
        {
            Assert.Equal(leader.Term, node.Term);
        }
    }
}

public class AllLogReplication : RaftTestsBase
{
    public AllLogReplication() : base(3)
    {
    }

    [Fact]
    public async Task LogReplicationOccursAcrossAllNodes()
    {
        StartNodes();
        await WaitForLeaderElected();

        var leader = testNodes.FirstOrDefault(n => IsLeader(n));
        Assert.NotNull(leader);

        int originalTerm = leader.Term;
        leader.Term = originalTerm + 1;

        await Task.Delay(1000);

        foreach (var node in testNodes.Where(n => n != leader))
        {
            Assert.Equal(leader.Term, node.Term);
        }
    }
}

public class DeadNodeNoUpdate : RaftTestsBase
{
    public DeadNodeNoUpdate() : base(3)
    {
    }

    [Fact]
    public async void NoUpdateOnHeartBeatToDeadNode()
    {
        StartNodes();
        await WaitForLeaderElected();
        var deadNode = testNodes.FirstOrDefault(n => !IsLeader(n));
        Assert.NotNull(deadNode);

        deadNode.KillNode();

        var leader = testNodes.FirstOrDefault(n => IsLeader(n));
        Assert.NotNull(leader);

        int originalTerm = leader.Term;
        leader.Term = originalTerm + 1;

        await Task.Delay(100);


        foreach (var node in testNodes.Where(n => !IsLeader(n)))
        {
            Assert.Equal(leader.Term, node.Term);
        }
    }
}
