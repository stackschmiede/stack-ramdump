namespace RamDump.Models;

public class ProcessMemoryInfo
{
    public int Pid { get; init; }
    public string Name { get; init; } = string.Empty;
    public long WorkingSet { get; init; }
    public long PrivateBytes { get; init; }
    public long PeakWorkingSet { get; init; }
    public bool IsSystemProcess { get; init; }
}
