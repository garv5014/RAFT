using RAFT;

namespace RAFT_Test;

public class RaftGatewayTestsBase : RaftTestsBase
{
    protected Gateway Gateway;

    public RaftGatewayTestsBase(int numberOfNodes, int timeDelay = 1)
        : base(numberOfNodes, timeDelay)
    {
        Gateway = new Gateway(testNodes);
    }

    public async Task<(string value, bool success)> EventualGetWithRetry(
        string key,
        int maxRetries = 10
    )
    {
        int attempts = 0;
        while (attempts < maxRetries)
        {
            var (value, success) = Gateway.EventualGet(key);
            if (success)
            {
                return (value, true);
            }

            attempts++;
            await Task.Delay(1000); // Wait for 500ms before retrying
        }

        return (null, false); // Return failure after exceeding max retries
    }
}

public class EventualGetTest : RaftGatewayTestsBase
{
    public EventualGetTest()
        : base(3) // Setup with 3 nodes
    { }

    [Fact]
    public async void ReturnsValueForExistingKey()
    {
        var expectedValue = "testValue";
        StartNodes();
        Task.Delay(1000).Wait(); // Wait for nodes to start
        Gateway.CompareVersionAndSwap("testKey", null, expectedValue);
        Task.Delay(1000).Wait(); // Wait for nodes to start

        var (value, success) = await EventualGetWithRetry("testKey");

        Assert.True(success);
        Assert.Equal(expectedValue, value);
    }
}

public class StrongGetTest : RaftGatewayTestsBase
{
    public StrongGetTest()
        : base(5) { }

    [Fact]
    public async Task ReturnsValueForExistingKeyFromLeader()
    {
        StartNodes();
        await WaitForLeaderElected();

        var leader = testNodes.First(node => IsLeader(node));
        var expectedValue = "leaderValue";
        leader.Log["leaderKey"] = (expectedValue, 0);

        var (value, success) = Gateway.StrongGet("leaderKey");

        Assert.True(success);
        Assert.Equal(expectedValue, value);
    }
}

public class CompareVersionAndSwapTest : RaftGatewayTestsBase
{
    public CompareVersionAndSwapTest()
        : base(3) { }

    [Fact]
    public async Task SuccessfullySwapsValueIfExpectedValueMatches()
    {
        var initialValue = "initial";
        var newValue = "updated";

        StartNodes();
        await WaitForLeaderElected();
        var leader = testNodes.First(node => IsLeader(node));
        var success = Gateway.CompareVersionAndSwap("swapKey", null, newValue);

        Assert.True(success);
        Assert.Equal(newValue, leader.Log["swapKey"].value);
    }
}
