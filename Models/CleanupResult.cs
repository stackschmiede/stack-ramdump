namespace RamDump.Models;

public class CleanupResult
{
    public int ProcessesTrimmed { get; init; }
    public int ProcessesFailed { get; init; }
    public long BytesFreed { get; init; }
    public bool StandbyCleared { get; init; }
    public long MemBefore { get; init; }
    public long MemAfter { get; init; }
    public string Summary { get; init; } = string.Empty;
}
