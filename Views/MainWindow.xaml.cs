using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.Messaging;
using RamDump.ViewModels;
using RamDump.ViewModels.Messages;

namespace RamDump.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        Loaded += (_, _) => ApplyDarkTitleBar();

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
            Hide();
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
