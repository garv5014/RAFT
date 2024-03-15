namespace Raft_Library;

public class CompareAndSwapRequest
{
    public string Key { get; set; } = null!;
    public string? OldValue { get; set; }
    public string NewValue { get; set; } = null!;
}
