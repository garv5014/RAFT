namespace RAFT_Test;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Raft_Node;
using Raft_Node.Options;
using Xunit;

public class RaftNodeServiceTests
{
    private readonly Mock<ILogger<RaftNodeService>> mockLogger;
    private readonly Mock<IRaftNodeClient> mockNodeClient;
    private readonly ApiOptions options;

    public RaftNodeServiceTests()
    {
        mockLogger = new Mock<ILogger<RaftNodeService>>();
        mockNodeClient = new Mock<IRaftNodeClient>();
        options = new ApiOptions
        {
            NodeIdentifier = 1,
            NodeCount = 3,
            NodeServiceName = "raftnode",
            NodeServicePort = 8080,
            LogMessageIntervalSeconds = 30
        };
    }

    [Fact]
    public async Task StartElection_ShouldSetStateToCandidate_AndIncrementTerm()
    {
        // Arrange
        mockNodeClient
            .Setup(x =>
                x.RequestVoteAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()
                )
            )
            .ReturnsAsync(new VoteResponse() { VotedId = 1, VoteGranted = true });
        var service = new RaftNodeService(mockLogger.Object, options, mockNodeClient.Object);

        // Act
        await service.StartElection();

        // Assert
        Assert.Equal(RaftNodeState.Leader, service.State);
        Assert.Equal(1, service.CurrentTerm);
    }

    [Fact]
    public void VoteForCandidate_ShouldGrantVote_WhenTermIsHigher()
    {
        // Arrange
        var service = new RaftNodeService(mockLogger.Object, options, mockNodeClient.Object);

        // Act
        var result = service.VoteForCandidate(2, 1, 0); // CandidateId, TheirTerm, TheirCommittedLogIndex

        // Assert
        Assert.True(result);
        Assert.Equal(2, service.VotedFor);
    }
}
