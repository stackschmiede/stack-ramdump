using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using RamDump.Models;
using RamDump.Services;

namespace RamDump.ViewModels;

public partial class ProcessMemoryInfoViewModel : ObservableObject
{
    public int Pid { get; }
    public string Name { get; }
    public bool IsSystemProcess { get; }
    public ProcessCategory Category { get; }
    public string CategoryTag { get; }

    [ObservableProperty] private long _workingSet;
    [ObservableProperty] private long _privateBytes;
    [ObservableProperty] private long _peakWorkingSet;
    [ObservableProperty] private bool _isGrowing;
    [ObservableProperty] private long _growthDelta;
    [ObservableProperty] private BitmapSource? _icon;
    [ObservableProperty] private bool _isSelected;

    public ProcessMemoryInfoViewModel(ProcessMemoryInfo model)
    {
        Pid = model.Pid;
        Name = model.Name;
        IsSystemProcess = model.IsSystemProcess;
        Category = ProcessClassifier.Classify(model.Name, model.IsSystemProcess);
        CategoryTag = Category.ToTag();
        _workingSet = model.WorkingSet;
        _privateBytes = model.PrivateBytes;
        _peakWorkingSet = model.PeakWorkingSet;
    }
}
