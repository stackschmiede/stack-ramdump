using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using RamDump.Models;

namespace RamDump.ViewModels;

public partial class ProcessMemoryInfoViewModel : ObservableObject
{
    public int Pid { get; }
    public string Name { get; }

    [ObservableProperty] private long _workingSet;
    [ObservableProperty] private long _privateBytes;
    [ObservableProperty] private long _peakWorkingSet;
    [ObservableProperty] private bool _isGrowing;
    [ObservableProperty] private long _growthDelta;
    [ObservableProperty] private BitmapSource? _icon;

    public ProcessMemoryInfoViewModel(ProcessMemoryInfo model)
    {
        Pid = model.Pid;
        Name = model.Name;
        _workingSet = model.WorkingSet;
        _privateBytes = model.PrivateBytes;
        _peakWorkingSet = model.PeakWorkingSet;
    }
}
