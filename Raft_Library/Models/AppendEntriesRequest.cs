namespace Raft_Library.Models;

public class AppendEntriesRequest
{
    public int LeaderId { get; set; }
    public int Term { get; set; }
    public int LeaderCommittedIndex { get; set; }
    public Dictionary<string, VersionedValue<string>> Entries { get; set; } = new();
}

public class AppendEntryRequest
{
    public int LeaderId { get; set; }
    public int Term { get; set; }
    public int LeaderCommittedIndex { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public int Version { get; set; }
}
