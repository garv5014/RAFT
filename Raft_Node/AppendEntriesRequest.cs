﻿
namespace Raft_Node;

public class AppendEntriesRequest
{
    public int Term { get; set; }
    public int LeaderId { get; set; }
    public List<(string key, string value)> Entries { get; set; }
}