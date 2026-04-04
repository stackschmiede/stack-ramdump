using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using RamDump.Models;

namespace RamDump.ViewModels;

public partial class ProcessMemoryInfoViewModel : ObservableObject
{
    public ProcessMemoryInfo Model { get; }

    public int Pid => Model.Pid;
    public string Name => Model.Name;
    public long WorkingSet => Model.WorkingSet;
    public long PrivateBytes => Model.PrivateBytes;
    public long PeakWorkingSet => Model.PeakWorkingSet;

    [ObservableProperty]
    private bool _isGrowing;

    [ObservableProperty]
    private long _growthDelta;

    [ObservableProperty]
    private BitmapSource? _icon;

    public ProcessMemoryInfoViewModel(ProcessMemoryInfo model)
    {
        Model = model;
    }
}
