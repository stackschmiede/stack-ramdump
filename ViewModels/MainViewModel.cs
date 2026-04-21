using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using RamDump.Models;
using RamDump.Services;
using RamDump.ViewModels.Messages;

namespace RamDump.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DispatcherTimer _refreshTimer;
    private readonly object _processLock = new();
    private Dictionary<int, long> _previousWorkingSets = new();
    private const long GrowthThresholdBytes = 50 * 1024 * 1024; // 50 MB
    private const long TopThresholdBytes = 100 * 1024 * 1024; // 100 MB
    private const long CriticalThresholdBytes = (long)(1.5 * 1024 * 1024 * 1024); // 1.5 GB
    private bool _suppressSettingsSave;
    private bool _windowHidden;
    private bool _ramTabActive = true; // Prozess-Enumeration nur wenn RAM-Tab sichtbar

    [ObservableProperty]
    private SystemMemoryInfo _systemMemory = new();

    [ObservableProperty]
    private string _statusText = "Bereit";

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _autoRefresh = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TrimAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearStandbyCommand))]
    [NotifyCanExecuteChangedFor(nameof(FullCleanupCommand))]
    [NotifyCanExecuteChangedFor(nameof(TrimSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(KillSelectedCommand))]
    private bool _isAdmin;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TrimAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearStandbyCommand))]
    [NotifyCanExecuteChangedFor(nameof(FullCleanupCommand))]
    [NotifyCanExecuteChangedFor(nameof(TrimSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(KillSelectedCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _sortColumn = "WorkingSet";

    [ObservableProperty]
    private ListSortDirection _sortDirection = ListSortDirection.Descending;

    [ObservableProperty]
    private bool _isGrouped;

    [ObservableProperty]
    private bool _showSystemProcesses = false;

    [ObservableProperty]
    private string _activeFilter = "all";

    [ObservableProperty]
    private int _allCount;

    [ObservableProperty]
    private int _topCount;

    [ObservableProperty]
    private int _browserCount;

    [ObservableProperty]
    private int _devCount;

    [ObservableProperty]
    private int _systemCount;

    [ObservableProperty]
    private int _criticalCount;

    [ObservableProperty]
    private int _displayedCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TrimSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(KillSelectedCommand))]
    private int _selectedCount;

    public ObservableCollection<ProcessMemoryInfoViewModel> Processes { get; } = [];
    public ObservableCollection<ProcessMemoryInfoViewModel> SelectedProcesses { get; } = [];
    public ICollectionView ProcessesView { get; }
    public MonitorViewModel Monitor { get; } = new();
    public AboutViewModel About { get; } = new();

    public MainViewModel()
    {
        ProcessesView = CollectionViewSource.GetDefaultView(Processes);
        ProcessesView.Filter = FilterProcess;
        BindingOperations.EnableCollectionSynchronization(Processes, _processLock);

        LoadSettings();

        IsAdmin = MemoryQueryService.IsAdmin();
        if (IsAdmin)
        {
            try { MemoryQueryService.EnableRequiredPrivileges(); } catch { /* best effort */ }
        }

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _refreshTimer.Tick += (_, _) => RefreshCommand.Execute(null);

        UpdateTimerState();

        RefreshCommand.Execute(null);
    }

    public void SetWindowHidden(bool hidden)
    {
        if (_windowHidden == hidden) return;
        _windowHidden = hidden;
        UpdateTimerState();
        // Monitor-Sensoren (LHM + Perf-Counter) im Tray abschalten — sonst laufen sie unsichtbar weiter
        if (hidden) Monitor.IsActive = false;
    }

    // Wird von MainWindow beim Tab-Wechsel aufgerufen. RAM-Tab: volle Refresh inkl. Prozessliste.
    // Andere Tabs: nur SystemMemory (günstig, hält Monitor-Hero aktuell). So läuft die teure
    // Process.GetProcesses()-Enumeration nur, wenn jemand die Prozessliste tatsächlich sieht.
    public void SetRamTabActive(bool active)
    {
        bool wasActive = _ramTabActive;
        _ramTabActive = active;
        if (active && !wasActive)
        {
            // Beim Wechsel zurück auf RAM sofort aktualisieren — sonst bis zu 5 s Altdaten
            RefreshCommand.Execute(null);
        }
    }

    private void UpdateTimerState()
    {
        if (AutoRefresh && !_windowHidden) _refreshTimer.Start();
        else _refreshTimer.Stop();
    }

    partial void OnSearchTextChanged(string value)
    {
        ProcessesView.Refresh();
        UpdateDisplayedCount();
    }

    partial void OnAutoRefreshChanged(bool value)
    {
        UpdateTimerState();
        SaveSettings();
    }

    partial void OnSortColumnChanged(string value) => SaveSettings();
    partial void OnSortDirectionChanged(ListSortDirection value) => SaveSettings();

    partial void OnShowSystemProcessesChanged(bool value)
    {
        ProcessesView.Refresh();
        UpdateDisplayedCount();
        SaveSettings();
    }

    partial void OnIsGroupedChanged(bool value)
    {
        ProcessesView.GroupDescriptions.Clear();
        if (value)
            ProcessesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ProcessMemoryInfoViewModel.Name)));
        ProcessesView.Refresh();
        SaveSettings();
    }

    partial void OnActiveFilterChanged(string value)
    {
        ProcessesView.Refresh();
        UpdateDisplayedCount();
        SaveSettings();
    }

    partial void OnSystemMemoryChanged(SystemMemoryInfo value)
    {
        Monitor.SystemMemory = value;
        if (value.UsagePercent >= 85)
            WeakReferenceMessenger.Default.Send(new MemoryThresholdMessage(value.UsagePercent));
    }

    private bool FilterProcess(object obj)
    {
        if (obj is not ProcessMemoryInfoViewModel vm) return false;
        if (!ShowSystemProcesses && vm.IsSystemProcess) return false;

        if (!string.IsNullOrWhiteSpace(SearchText)
            && !vm.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            && !vm.Pid.ToString().Contains(SearchText))
            return false;

        return ActiveFilter switch
        {
            "top" => vm.WorkingSet >= TopThresholdBytes,
            "browser" => vm.Category == ProcessCategory.Browser,
            "dev" => vm.Category == ProcessCategory.Dev,
            "sys" => vm.Category == ProcessCategory.System,
            "crit" => vm.WorkingSet >= CriticalThresholdBytes
                      || (vm.IsGrowing && vm.GrowthDelta >= 200 * 1024 * 1024),
            _ => true,
        };
    }

    [RelayCommand]
    private void SetFilter(string filter)
    {
        ActiveFilter = string.IsNullOrWhiteSpace(filter) ? "all" : filter;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var mem = await Task.Run(MemoryQueryService.GetSystemMemory);
        SystemMemory = mem;

        // Andere Tabs brauchen die Prozessliste nicht — enumerieren nur wenn RAM-Tab aktiv.
        if (!_ramTabActive) return;

        var procs = await Task.Run(MemoryQueryService.GetProcesses);

        var prevSets = _previousWorkingSets;
        _previousWorkingSets = procs.ToDictionary(p => p.Pid, p => p.WorkingSet);

        var newVms = new List<ProcessMemoryInfoViewModel>();

        lock (_processLock)
        {
            var existingMap = Processes.ToDictionary(vm => vm.Pid);
            var freshPids = procs.Select(p => p.Pid).ToHashSet();

            for (int i = Processes.Count - 1; i >= 0; i--)
            {
                if (!freshPids.Contains(Processes[i].Pid))
                    Processes.RemoveAt(i);
            }

            foreach (var p in procs)
            {
                long prev = prevSets.TryGetValue(p.Pid, out long v) ? v : 0;
                long delta = p.WorkingSet - prev;

                if (existingMap.TryGetValue(p.Pid, out var vm))
                {
                    vm.WorkingSet = p.WorkingSet;
                    vm.PrivateBytes = p.PrivateBytes;
                    vm.PeakWorkingSet = p.PeakWorkingSet;
                    vm.GrowthDelta = delta;
                    vm.IsGrowing = delta >= GrowthThresholdBytes;
                }
                else
                {
                    vm = new ProcessMemoryInfoViewModel(p)
                    {
                        GrowthDelta = delta,
                        IsGrowing = delta >= GrowthThresholdBytes,
                    };
                    Processes.Add(vm);
                    newVms.Add(vm);
                }
            }
        }

        // Icons nur für neue Prozesse laden (gecached per Pfad)
        if (newVms.Count > 0)
        {
            _ = Task.Run(() =>
            {
                foreach (var vm in newVms)
                {
                    var icon = ProcessIconService.GetIcon(vm.Pid);
                    if (icon != null)
                        Application.Current?.Dispatcher.InvokeAsync(() => vm.Icon = icon);
                }
            });
        }

        ApplySort();
        UpdateCounts();
        UpdateDisplayedCount();
        SyncTopProcess();
    }

    private void UpdateCounts()
    {
        int all = 0, top = 0, browser = 0, dev = 0, sys = 0, crit = 0;
        foreach (var p in Processes)
        {
            if (!ShowSystemProcesses && p.IsSystemProcess) continue;
            all++;
            if (p.WorkingSet >= TopThresholdBytes) top++;
            if (p.Category == ProcessCategory.Browser) browser++;
            if (p.Category == ProcessCategory.Dev) dev++;
            if (p.IsSystemProcess) sys++;
            if (p.WorkingSet >= CriticalThresholdBytes
                || (p.IsGrowing && p.GrowthDelta >= 200 * 1024 * 1024)) crit++;
        }
        AllCount = all;
        TopCount = top;
        BrowserCount = browser;
        DevCount = dev;
        SystemCount = sys;
        CriticalCount = crit;
    }

    private void UpdateDisplayedCount()
    {
        int count = 0;
        foreach (var _ in ProcessesView) count++;
        DisplayedCount = count;
    }

    public void UpdateSelection(IEnumerable<ProcessMemoryInfoViewModel> items)
    {
        SelectedProcesses.Clear();
        foreach (var item in items)
            SelectedProcesses.Add(item);
        SelectedCount = SelectedProcesses.Count;
    }

    private void SyncTopProcess()
    {
        var top = Processes.OrderByDescending(p => p.WorkingSet).FirstOrDefault();
        if (top is null)
        {
            Monitor.TopProcessName = "—";
            Monitor.TopProcessSize = "—";
            return;
        }
        Monitor.TopProcessName = top.Name;
        Monitor.TopProcessSize = top.WorkingSet switch
        {
            >= 1_073_741_824 => $"{top.WorkingSet / 1_073_741_824.0:F1} GB",
            >= 1_048_576 => $"{top.WorkingSet / 1_048_576.0:F0} MB",
            _ => $"{top.WorkingSet / 1024.0:F0} KB",
        };
    }

    [RelayCommand(CanExecute = nameof(CanCleanup))]
    private async Task TrimAllAsync()
    {
        IsBusy = true;
        StatusText = "Trimme Working Sets...";
        try
        {
            var result = await MemoryCleanupService.TrimAllWorkingSetsAsync();
            StatusText = result.Summary;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Fehler: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCleanup))]
    private async Task ClearStandbyAsync()
    {
        IsBusy = true;
        StatusText = "Leere Standby-Liste...";
        try
        {
            var result = await MemoryCleanupService.ClearStandbyListAsync();
            StatusText = result.Summary;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Fehler: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCleanup))]
    private async Task FullCleanupAsync()
    {
        var confirmed = await WeakReferenceMessenger.Default.Send(
            new ConfirmationRequestMessage(
                "RAM vollständig bereinigen?\n\nAlle Working Sets werden geleert und die Standby-Liste wird gelöscht."));
        if (!confirmed) return;

        IsBusy = true;
        StatusText = "Voll-Bereinigung...";
        try
        {
            var result = await MemoryCleanupService.FullCleanupAsync();
            StatusText = result.Summary;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Fehler: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task TrimProcessAsync(ProcessMemoryInfoViewModel? process)
    {
        if (process == null) return;
        IsBusy = true;
        StatusText = $"Trimme {process.Name}...";
        try
        {
            var result = await MemoryCleanupService.TrimProcessAsync(process.Pid);
            StatusText = result.Summary;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Fehler: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanActOnSelection))]
    private async Task TrimSelectedAsync()
    {
        var targets = SelectedProcesses.ToList();
        if (targets.Count == 0) return;

        IsBusy = true;
        int ok = 0, fail = 0;
        StatusText = $"Trimme {targets.Count} Prozesse...";
        try
        {
            foreach (var p in targets)
            {
                try
                {
                    var r = await MemoryCleanupService.TrimProcessAsync(p.Pid);
                    if (r.ProcessesTrimmed > 0) ok++;
                    else fail++;
                }
                catch
                {
                    fail++;
                }
            }
            StatusText = $"Auswahl getrimmt: {ok} ok, {fail} fehlgeschlagen";
            await RefreshAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanActOnSelection))]
    private async Task KillSelectedAsync()
    {
        var targets = SelectedProcesses.ToList();
        if (targets.Count == 0) return;

        var names = string.Join(", ", targets.Take(5).Select(p => p.Name));
        if (targets.Count > 5) names += ", …";
        var confirmed = await WeakReferenceMessenger.Default.Send(
            new ConfirmationRequestMessage(
                $"{targets.Count} Prozess(e) beenden?\n\n{names}\n\nUngespeicherte Daten gehen verloren."));
        if (!confirmed) return;

        IsBusy = true;
        int ok = 0, fail = 0;
        StatusText = $"Beende {targets.Count} Prozess(e)...";
        try
        {
            foreach (var p in targets)
            {
                try
                {
                    var r = await MemoryCleanupService.KillProcessAsync(p.Pid);
                    if (r.ProcessesTrimmed > 0) ok++;
                    else fail++;
                }
                catch
                {
                    fail++;
                }
            }
            StatusText = $"Beendet: {ok} ok, {fail} fehlgeschlagen";
            await RefreshAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column)
            SortDirection = SortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        else
        {
            SortColumn = column;
            SortDirection = ListSortDirection.Descending;
        }
        ApplySort();
    }

    [RelayCommand]
    private void FocusSearch()
    {
        WeakReferenceMessenger.Default.Send(new FocusSearchMessage());
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        var dlg = new SaveFileDialog
        {
            FileName = $"ram-dump-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            Filter = "CSV-Dateien (*.csv)|*.csv",
            DefaultExt = ".csv",
        };
        if (dlg.ShowDialog() != true) return;

        var lines = new List<string> { "Name,PID,WorkingSet,PrivateBytes,PeakWorkingSet,Category" };
        foreach (var p in Processes)
            lines.Add($"\"{p.Name}\",{p.Pid},{p.WorkingSet},{p.PrivateBytes},{p.PeakWorkingSet},{p.CategoryTag}");

        await File.WriteAllLinesAsync(dlg.FileName, lines, System.Text.Encoding.UTF8);
        StatusText = $"Exportiert: {Path.GetFileName(dlg.FileName)}";
    }

    private void ApplySort()
    {
        ProcessesView.SortDescriptions.Clear();
        ProcessesView.SortDescriptions.Add(new SortDescription(SortColumn, SortDirection));
    }

    private bool CanCleanup() => IsAdmin && !IsBusy;
    private bool CanActOnSelection() => IsAdmin && !IsBusy && SelectedCount > 0;

    private void LoadSettings()
    {
        _suppressSettingsSave = true;
        try
        {
            var s = SettingsService.Load();
            AutoRefresh = s.AutoRefresh;
            SortColumn = s.SortColumn;
            SortDirection = Enum.TryParse<ListSortDirection>(s.SortDirection, out var d) ? d : ListSortDirection.Descending;
            IsGrouped = s.IsGrouped;
            ShowSystemProcesses = s.ShowSystemProcesses;
            Monitor.RefreshIntervalSeconds = s.MonitorRefreshIntervalSeconds;
            ActiveFilter = string.IsNullOrWhiteSpace(s.ActiveFilter) ? "all" : s.ActiveFilter;
        }
        finally
        {
            _suppressSettingsSave = false;
        }
    }

    public void SaveSettings()
    {
        if (_suppressSettingsSave) return;
        var s = SettingsService.Load();
        s.AutoRefresh = AutoRefresh;
        s.SortColumn = SortColumn;
        s.SortDirection = SortDirection.ToString();
        s.IsGrouped = IsGrouped;
        s.ShowSystemProcesses = ShowSystemProcesses;
        s.MonitorRefreshIntervalSeconds = Monitor.RefreshIntervalSeconds;
        s.ActiveFilter = ActiveFilter;
        SettingsService.Save(s);
    }

    public void SaveWindowSettings(double width, double height, double left, double top, double? ramHeight)
    {
        var s = SettingsService.Load();
        s.WindowWidth = width;
        s.WindowHeight = height;
        if (ramHeight.HasValue && ramHeight.Value > 0)
            s.RamWindowHeight = ramHeight.Value;
        s.WindowLeft = left;
        s.WindowTop = top;
        s.AutoRefresh = AutoRefresh;
        s.SortColumn = SortColumn;
        s.SortDirection = SortDirection.ToString();
        s.IsGrouped = IsGrouped;
        s.ShowSystemProcesses = ShowSystemProcesses;
        s.MonitorRefreshIntervalSeconds = Monitor.RefreshIntervalSeconds;
        s.ActiveFilter = ActiveFilter;
        SettingsService.Save(s);
    }
}
