namespace RamDump.Models;

public class SystemMemoryInfo
{
    public long TotalPhysical { get; init; }
    public long AvailablePhysical { get; init; }
    public long UsedPhysical => TotalPhysical - AvailablePhysical;
    public double UsagePercent => TotalPhysical > 0
        ? (double)UsedPhysical / TotalPhysical * 100.0
        : 0;
    public long CachedBytes { get; init; }
}
