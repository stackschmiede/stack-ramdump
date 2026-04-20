using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Navigation;
using CommunityToolkit.Mvvm.Messaging;
using RamDump.ViewModels;
using RamDump.ViewModels.Messages;

namespace RamDump.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private const int RamTabIndex = 0;
    private const int MonitorTabIndex = 1;
    private const int AboutTabIndex = 2;

    private double? _ramHeight;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        var saved = RamDump.Services.SettingsService.Load();
        _ramHeight = saved.RamWindowHeight > 0 ? saved.RamWindowHeight : saved.WindowHeight;

        Loaded += (_, _) =>
        {
            ApplyDarkTitleBar();
            MainTabs.SelectedIndex = Math.Clamp(saved.ActiveTabIndex, 0, MainTabs.Items.Count - 1);
        };

        WeakReferenceMessenger.Default.Register<FocusSearchMessage>(this, (_, _) =>
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
        });

        WeakReferenceMessenger.Default.Register<ConfirmationRequestMessage>(this, (_, msg) =>
        {
            var result = ConfirmationDialog.Confirm(this, msg.Message);
            msg.Reply(result);
        });
    }

    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is null) return;
        if (!ReferenceEquals(e.OriginalSource, MainTabs)) return;

        var idx = MainTabs.SelectedIndex;
        var isMonitor = idx == MonitorTabIndex && WindowState != WindowState.Minimized;

        if (isMonitor)
            _viewModel.Monitor.EnsureInitialized();
        _viewModel.Monitor.IsActive = isMonitor;

        if (idx == AboutTabIndex)
            _viewModel.About.LoadSystemInfo();

        ApplyTabSizing(idx);

        var s = RamDump.Services.SettingsService.Load();
        s.ActiveTabIndex = idx;
        RamDump.Services.SettingsService.Save(s);
    }

    private void ApplyTabSizing(int idx)
    {
        if (WindowState == WindowState.Minimized) return;

        if (idx == MonitorTabIndex)
        {
            if (!_ramHeight.HasValue || _ramHeight.Value <= 0)
                _ramHeight = ActualHeight;
            SizeToContent = SizeToContent.Height;
        }
        else
        {
            SizeToContent = SizeToContent.Manual;
            if (_ramHeight.HasValue && _ramHeight.Value > 0)
            {
                Height = Math.Max(MinHeight, _ramHeight.Value);
            }
        }
    }

    private void ProcessGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid dg) return;
        var items = dg.SelectedItems
            .OfType<ProcessMemoryInfoViewModel>()
            .ToList();
        _viewModel.UpdateSelection(items);
    }

    private void TrimCurrentRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ProcessMemoryInfoViewModel vm)
        {
            _viewModel.TrimProcessCommand.Execute(vm);
        }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void ApplyDarkTitleBar()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        int value = 1;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized)
        {
            _viewModel.Monitor.IsActive = false;
            Hide();
        }
        else if (MainTabs.SelectedIndex == MonitorTabIndex)
        {
            _viewModel.Monitor.IsActive = true;
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        base.OnClosing(e);

        double heightToPersist = MainTabs.SelectedIndex == RamTabIndex
            ? ActualHeight
            : (_ramHeight ?? ActualHeight);

        _viewModel.SaveWindowSettings(
            ActualWidth, ActualHeight, Left, Top, heightToPersist);

        e.Cancel = true;
        Hide();
    }
}
