namespace RamDump.Services;

public class AppSettings
{
    public bool AutoRefresh { get; set; } = true;
    public string SortColumn { get; set; } = "WorkingSet";
    public string SortDirection { get; set; } = "Descending";
    public double WindowWidth { get; set; } = 900;
    public double WindowHeight { get; set; } = 700;
    public double RamWindowHeight { get; set; } = 700;
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;
    public bool IsGrouped { get; set; }
    public bool ShowSystemProcesses { get; set; } = false;
    public int ActiveTabIndex { get; set; } = 0;
    public int MonitorRefreshIntervalSeconds { get; set; } = 2;
    public string ActiveFilter { get; set; } = "all";
}
