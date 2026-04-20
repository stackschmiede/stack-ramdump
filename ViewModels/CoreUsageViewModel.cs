using CommunityToolkit.Mvvm.ComponentModel;

namespace RamDump.ViewModels;

public partial class CoreUsageViewModel : ObservableObject
{
    public int CoreIndex { get; }

    [ObservableProperty] private double _percent;
    [ObservableProperty] private double _clockMHz;

    public CoreUsageViewModel(int coreIndex)
    {
        CoreIndex = coreIndex;
    }
}
