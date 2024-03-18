using Raft_Library.Models;
using Raft_Node.Options;

namespace Raft_Node;

public class RaftNodeService : BackgroundService
{
    public RaftNodeState State { get; set; } = RaftNodeState.Follower;

    private DateTime lastHeartbeatReceived;

    private int electionTimeout;

    private Random random = new Random();

    public List<string> OtherNodeAddresses { get; set; } = new List<string>();

    public int Id { get; private set; }

    public Dictionary<string, VersionedValue<string>> Data { get; set; } = new();

    public int CurrentTerm { get; set; } = 0;

    public int CommittedIndex { get; set; }

    public int VotedFor { get; set; }

    public int LeaderId { get; set; }

    public bool IsLeader => State == RaftNodeState.Leader;

    private readonly ILogger<RaftNodeService> logger;

    private readonly ApiOptions options;

    private readonly IRaftNodeClient nodeClient;

    public RaftNodeService(
        ILogger<RaftNodeService> logger,
        ApiOptions options,
        IRaftNodeClient nodeClient
    )
    {
        this.logger = logger;
        this.options = options;
        this.nodeClient = nodeClient;
        Id = options.NodeIdentifier;
        electionTimeout = random.Next(150, 300);
        for (int i = 1; i <= options.NodeCount; i++)
        {
            if (i == options.NodeIdentifier)
            {
                continue;
            }
            OtherNodeAddresses.Add(
                $"http://{options.NodeServiceName}{i}:{options.NodeServicePort}"
            );
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Initializing node.");

        lastHeartbeatReceived = DateTime.UtcNow;
        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (State == RaftNodeState.Leader)
                {
                    Task.Delay(100).Wait();
                    await SendHeartbeats();
                }
                else if (HasElectionTimedOut())
                {
                    WriteToLog("Election timed out.");
                    await StartElection();
                }
            }
        });
    }

    public (string value, int version) Get(string key)
    {
        return ("value", 1);
    }

    public async Task StartElection(int term = 0)
    {
        State = RaftNodeState.Candidate;
        if (term > 0)
        {
            CurrentTerm = term;
        }
        else
        {
            CurrentTerm++;
        }
        ResetElectionTimeout();
        WriteToLog($"Running for election cycle {CurrentTerm}. Requesting votes from other nodes.");
        int votesReceived = 1; // vote for self
        VotedFor = Id;
        long myLatestCommittedLogIndex = 0;
        if (Data.Count() > 0)
            myLatestCommittedLogIndex = Data.Values.Max(v => v.Version);

        foreach (var nodeAddress in OtherNodeAddresses)
        {
            VoteResponse? response = null;
            try
            {
                response = await nodeClient.RequestVoteAsync(
                    nodeAddress,
                    CurrentTerm,
                    myLatestCommittedLogIndex,
                    Id
                );
            }
            catch (Exception ex)
            {
                WriteToLog($"Vote request failed at {nodeAddress}. {ex.Message}");
            }

            if (response != null && response.VoteGranted)
            {
                votesReceived++;
                WriteToLog(
                    $"Received vote from node #{response.VotedId} {votesReceived}/{options.NodeCount} votes received."
                );
            }
            else
            {
                WriteToLog($"Vote request denied at {nodeAddress}.");
            }
        }

        if (votesReceived > options.NodeCount / 2)
        {
            State = RaftNodeState.Leader;
            LeaderId = Id;
            WriteToLog("Became the Leader.");
            await SendHeartbeats();
        }
        else
        {
            State = RaftNodeState.Follower;
            WriteToLog("Lost election.");
        }
    }

    public bool VoteForCandidate(VoteRequest request)
    {
        return VoteForCandidate(request.CandidateId, request.Term, request.LastLogIndex);
    }

    public bool VoteForCandidate(int candidateId, int theirTerm, long theirCommittedLogIndex)
    {
        if (theirTerm < CurrentTerm || theirCommittedLogIndex < CommittedIndex)
        {
            WriteToLog(
                $"Denied vote request from node {candidateId} in election cycle {theirTerm}. {theirCommittedLogIndex} < {CommittedIndex}"
            );
            return false;
        }

        if (theirTerm > CurrentTerm || theirTerm == CurrentTerm && VotedFor == candidateId)
        {
            State = RaftNodeState.Follower;
            CurrentTerm = theirTerm;
            VotedFor = candidateId;
            ResetElectionTimeout();
            WriteToLog($"Voted for node {candidateId} in election term {theirTerm}.");
            return true;
        }
        else
        {
            WriteToLog(
                $"Denied vote request from node {candidateId} in election cycle {theirTerm}."
            );
            return false;
        }
    }

    private bool HasElectionTimedOut()
    {
        return DateTime.UtcNow - lastHeartbeatReceived > TimeSpan.FromMilliseconds(electionTimeout);
    }

    private void ResetElectionTimeout()
    {
        electionTimeout = random.Next(5000, 10500);
        lastHeartbeatReceived = DateTime.UtcNow;
    }

    private DateTime lastMessageClearTime = DateTime.MinValue;
    private HashSet<string> sentMessages = new HashSet<string>();

    private void WriteToLog(string message)
    {
        if (
            DateTime.UtcNow - lastMessageClearTime
            >= TimeSpan.FromSeconds(options.LogMessageIntervalSeconds)
        )
        {
            sentMessages.Clear();
            lastMessageClearTime = DateTime.UtcNow;
        }
        if (!sentMessages.Contains(message))
        {
            logger.LogInformation($"{message}");
            sentMessages.Add(message);
        }
    }

    public async Task SendHeartbeats()
    {
        if (State != RaftNodeState.Leader)
        {
            return;
        }
        foreach (var nodeAddress in OtherNodeAddresses)
        {
            await RequestAppendEntriesAsync(nodeAddress, CurrentTerm, CommittedIndex, Data);
        }
    }

    private async Task RequestAppendEntriesAsync(
        string nodeAddress,
        int currentTerm,
        int committedIndex,
        Dictionary<string, VersionedValue<string>> data
    )
    {
        var request = new AppendEntriesRequest
        {
            LeaderId = Id,
            Term = currentTerm,
            LeaderCommittedIndex = committedIndex,
            Entries = data
        };

        try
        {
            if (State != RaftNodeState.Leader)
            {
                WriteToLog("Not the leader. Why are we sending heartbeats?");
                return;
            }

            var success = await nodeClient.RequestAppendEntriesAsync(nodeAddress, request);

            if (success)
            {
                WriteToLog(
                    $"Heartbeat sent | Term: {currentTerm} | Committed: {committedIndex} | Occupation: {State}"
                );
            }
            else
            {
                WriteToLog("Heartbeat failed.");
            }
        }
        catch (Exception ex)
        {
            WriteToLog($"Heartbeat failed. {ex.Message}");
        }
    }

    public bool AppendEntry(AppendEntryRequest request)
    {
        return AppendEntry(request.Key, request.Value, request.Version, request.Term);
    }

    public bool AppendEntry(string key, string value, long logIndex, int term)
    {
        var mostRecentIndex = 0;
        if (Directory.Exists(options.EntryLogPath))
        {
            mostRecentIndex = new DirectoryInfo(options.EntryLogPath).GetFiles().Length;
        }

        if (logIndex > mostRecentIndex)
        {
            LogEntry(key, value, logIndex, term);
        }
        return true;
    }

    private void LogEntry(string key, string value, long index, int leaderTerm)
    {
        if (!Directory.Exists(options.EntryLogPath))
        {
            Directory.CreateDirectory(options.EntryLogPath);
        }

        var filePath = $"{options.EntryLogPath}/{index}.log";

        if (File.Exists(filePath))
        {
            return;
        }

        using (
            var stream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Write
            )
        )
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine(leaderTerm);
                writer.WriteLine(key);
                writer.WriteLine(value);
            }
        }

        WriteToLog(
            $"Log entry added | Index: {index} | Term: {leaderTerm} | Key: {key} | Value: {value}"
        );
    }

    public bool AppendEntries(AppendEntriesRequest request)
    {
        if (request.Term >= CurrentTerm)
        {
            ResetElectionTimeout();
            CurrentTerm = request.Term;
            State = RaftNodeState.Follower;
            LeaderId = request.LeaderId;
            WriteToLog(
                $"Heartbeat received | Term: {CurrentTerm} | Committed: {CommittedIndex} | Following: {LeaderId}"
            );

            foreach (var entry in request.Entries)
            {
                var mostRecentIndex = 0;
                if (Directory.Exists(options.EntryLogPath))
                {
                    mostRecentIndex = new DirectoryInfo(options.EntryLogPath).GetFiles().Length;
                }
                var newEntries = request.Entries.Where(e => e.Value.Version > mostRecentIndex);
                LogEntry(entry.Key, entry.Value.Value, entry.Value.Version, request.Term);
            }

            if (request.LeaderCommittedIndex > CommittedIndex)
            {
                CommitLogs(request.LeaderCommittedIndex);
            }
            return true;
        }

        return false;
    }

    private void CommitLogs(int committedIndex)
    {
        // Commit logs up to the committed index
        if (Directory.Exists(options.EntryLogPath))
        {
            var logFiles = new DirectoryInfo(options.EntryLogPath).GetFiles().OrderBy(f => f.Name);
            foreach (var file in logFiles)
            {
                WriteToLog(file.Name);
                var index = int.Parse(file.Name.Split('.')[0]);
                if (index <= committedIndex)
                {
                    WriteToLog($"Committing index {index}.");
                    var lines = File.ReadAllLines(file.FullName);
                    Data[lines[1]] = new VersionedValue<string>
                    {
                        Value = lines[2],
                        Version = index
                    };
                }
                if (index > committedIndex)
                {
                    WriteToLog(
                        $"Deleting index {index}. Over elected majority committed index: {committedIndex}."
                    );
                    file.Delete();
                }
            }
            CommittedIndex = committedIndex;
        }
    }

    public async Task<VersionedValue<string>> StrongGet(string key)
    {
        WriteToLog($"StrongGet called with key: {key}");
        if (!IsLeader)
        {
            throw new Exception("Not the leader.");
        }
        var confirmLeaderCount = 1;
        foreach (var nodeAddr in OtherNodeAddresses)
        {
            if (await nodeClient.ConfirmLeaderAsync(nodeAddr, LeaderId))
            {
                confirmLeaderCount++;
            }
            if (confirmLeaderCount > options.NodeCount / 2)
            {
                if (Data.ContainsKey(key))
                {
                    return Data[key];
                }
                else
                {
                    throw new Exception("Value not found.");
                }
            }
        }
        throw new Exception("Not the leader.");
    }

    public VersionedValue<string> EventualGet(string key)
    {
        WriteToLog($"EventualGet called with key: {key}");

        if (Data.ContainsKey(key))
        {
            return Data[key];
        }

        return new VersionedValue<string> { Value = String.Empty, Version = 0 };
    }

    public async Task CompareAndSwap(string key, string? oldValue, string newValue)
    {
        if (!IsLeader)
        {
            throw new Exception("Not the leader.");
        }

        var newIndex = 1;
        if (Directory.Exists(options.EntryLogPath))
        {
            newIndex = new DirectoryInfo(options.EntryLogPath).GetFiles().Length + 1;
        }
        if (Data.ContainsKey(key) && oldValue != Data[key].Value)
            throw new Exception("Value does not match.");

        LogEntry(key, newValue, newIndex, CurrentTerm);
        if (await BroadcastReplication(key, newValue, newIndex))
        {
            // if majority of nodes have replicated the log, update the data
            Data[key] = new VersionedValue<string> { Value = newValue, Version = newIndex };
            CommittedIndex = newIndex;
            return;
        }
        throw new Exception("Could not replicate to majority of nodes.");
    }

    private async Task<bool> BroadcastReplication(string key, string value, int index)
    {
        var confirmReplicationCount = 1;
        foreach (var nodeAddr in OtherNodeAddresses)
        {
            if (await nodeClient.RequestAppendEntryAsync(nodeAddr, CurrentTerm, key, value, index))
            {
                confirmReplicationCount++;
            }
        }
        if (confirmReplicationCount > options.NodeCount / 2)
        {
            return true;
        }
        return false;
    }
}

public enum RaftNodeState
{
    Follower,
    Candidate,
    Leader
}
