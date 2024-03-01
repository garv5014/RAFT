namespace RAFT;

public class Gateway
{
    private List<RaftNode> Nodes;

    public Gateway(List<RaftNode> nodes)
    {
        Nodes = nodes;
    }

    private RaftNode FindLeader()
    {
        return Nodes.FirstOrDefault(node => node.GetState() == RaftNodeState.Leader && node.isHealthy());
    }

    public (string value, bool success) EventualGet(string key)
    {
        var randomNode = Nodes[new Random().Next(Nodes.Count)];
        var success = randomNode.Log.TryGetValue(key, out var logEntry);
        return success ? (logEntry.value, true) : (null, false);
    }

    public (string value, bool success) StrongGet(string key)
    {
        var leader = FindLeader();
        if (leader == null) return (null, false);

        int acknowledgements = 1;
        foreach (var node in Nodes)
        {
            if (node.Name != leader.Name && node.MostRecentLeader == leader.Name)
            {
                acknowledgements++;
            }
        }

        if (acknowledgements > Nodes.Count / 2)
        {
            var success = leader.Log.TryGetValue(key, out var logEntry);
            return success ? (logEntry.value, true) : (null, false);
        }
        else
        {
            return (null, false);
        }
    }

    public bool CompareVersionAndSwap(string key, string expectedValue, string newValue)
    {
        var leader = FindLeader();
        if (leader == null) return false;

        // Confirm leadership by majority before proceeding with the operation
        int acknowledgements = 1; // Start with 1 for the leader itself
        foreach (var node in Nodes)
        {
            if (node.Name != leader.Name && node.MostRecentLeader == leader.Name)
            {
                acknowledgements++;
            }
        }

        if (acknowledgements == Nodes.Count)
        {
            if (!leader.Log.ContainsKey(key) || leader.Log.TryGetValue(key, out var logEntry) && logEntry.value == expectedValue)
            {
                if (!leader.Log.ContainsKey(key))
                {
                    leader.Log.Add(key, (newValue, leader.Log.Count));
                }
                var entries = new List<(string key, string value)> { (key, newValue) };

                int replicationSuccesses = 1;
                foreach (var node in Nodes)
                {
                    if (node != leader)
                    {
                        node.AppendEntries(leader.Term, leader.Name, entries);
                        replicationSuccesses++;
                    }
                }

                if (replicationSuccesses == Nodes.Count) // dont want it to end early for easy testing
                {
                    leader.Log[key] = (newValue, leader.Log[key].logIndex + 1);
                    return true;
                }
            }
        }

        return false;
    }

}
