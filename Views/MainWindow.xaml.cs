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
    private const int MonitorTabIndex = 1;
    private const int AboutTabIndex = 2;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        Loaded += (_, _) =>
        {
            ApplyDarkTitleBar();
            var saved = RamDump.Services.SettingsService.Load();
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
        var isMonitor = MainTabs.SelectedIndex == MonitorTabIndex
                        && WindowState != WindowState.Minimized;
        if (isMonitor)
            _viewModel.Monitor.EnsureInitialized();
        _viewModel.Monitor.IsActive = isMonitor;

        if (MainTabs.SelectedIndex == AboutTabIndex)
            _viewModel.About.LoadSystemInfo();

        var s = RamDump.Services.SettingsService.Load();
        s.ActiveTabIndex = MainTabs.SelectedIndex;
        RamDump.Services.SettingsService.Save(s);
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

        _viewModel.SaveWindowSettings(
            ActualWidth, ActualHeight, Left, Top);

        e.Cancel = true;
        Hide();
    }
}
