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
    private bool _suppressSettingsSave;

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
    private bool _isAdmin;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TrimAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearStandbyCommand))]
    [NotifyCanExecuteChangedFor(nameof(FullCleanupCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _sortColumn = "WorkingSet";

    [ObservableProperty]
    private ListSortDirection _sortDirection = ListSortDirection.Descending;

    [ObservableProperty]
    private bool _isGrouped;

    [ObservableProperty]
    private bool _showSystemProcesses = false;

    public ObservableCollection<ProcessMemoryInfoViewModel> Processes { get; } = [];
    public ICollectionView ProcessesView { get; }

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

        if (AutoRefresh)
            _refreshTimer.Start();

        RefreshCommand.Execute(null);
    }

    partial void OnSearchTextChanged(string value) => ProcessesView.Refresh();

    partial void OnAutoRefreshChanged(bool value)
    {
        if (value) _refreshTimer.Start();
        else _refreshTimer.Stop();
        SaveSettings();
    }

    partial void OnSortColumnChanged(string value) => SaveSettings();
    partial void OnSortDirectionChanged(ListSortDirection value) => SaveSettings();

    partial void OnShowSystemProcessesChanged(bool value)
    {
        ProcessesView.Refresh();
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

    partial void OnSystemMemoryChanged(SystemMemoryInfo value)
    {
        if (value.UsagePercent >= 85)
            WeakReferenceMessenger.Default.Send(new MemoryThresholdMessage(value.UsagePercent));
    }

    private bool FilterProcess(object obj)
    {
        if (obj is not ProcessMemoryInfoViewModel vm) return false;
        if (!ShowSystemProcesses && vm.IsSystemProcess) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        return vm.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || vm.Pid.ToString().Contains(SearchText);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var mem = await Task.Run(MemoryQueryService.GetSystemMemory);
        SystemMemory = mem;

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

        var lines = new List<string> { "Name,PID,WorkingSet,PrivateBytes,PeakWorkingSet" };
        foreach (var p in Processes)
            lines.Add($"\"{p.Name}\",{p.Pid},{p.WorkingSet},{p.PrivateBytes},{p.PeakWorkingSet}");

        await File.WriteAllLinesAsync(dlg.FileName, lines, System.Text.Encoding.UTF8);
        StatusText = $"Exportiert: {Path.GetFileName(dlg.FileName)}";
    }

    private void ApplySort()
    {
        ProcessesView.SortDescriptions.Clear();
        ProcessesView.SortDescriptions.Add(new SortDescription(SortColumn, SortDirection));
    }

    private bool CanCleanup() => IsAdmin && !IsBusy;

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
        }
        finally
        {
            _suppressSettingsSave = false;
        }
    }

    public void SaveSettings()
    {
        if (_suppressSettingsSave) return;
        SettingsService.Save(new AppSettings
        {
            AutoRefresh = AutoRefresh,
            SortColumn = SortColumn,
            SortDirection = SortDirection.ToString(),
            IsGrouped = IsGrouped,
            ShowSystemProcesses = ShowSystemProcesses,
        });
    }

    public void SaveWindowSettings(double width, double height, double left, double top)
    {
        var s = SettingsService.Load();
        s.WindowWidth = width;
        s.WindowHeight = height;
        s.WindowLeft = left;
        s.WindowTop = top;
        s.AutoRefresh = AutoRefresh;
        s.SortColumn = SortColumn;
        s.SortDirection = SortDirection.ToString();
        s.IsGrouped = IsGrouped;
        s.ShowSystemProcesses = ShowSystemProcesses;
        SettingsService.Save(s);
    }
}
