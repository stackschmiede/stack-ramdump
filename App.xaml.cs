using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Hardcodet.Wpf.TaskbarNotification;
using RamDump.Services;
using RamDump.ViewModels.Messages;
using RamDump.Views;

namespace RamDump;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private bool _thresholdNotified;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var splash = new SplashWindow();
        splash.AnimationFinished += (_, _) =>
        {
            LaunchMainWindow();
            // Nach Show+Activate den Splash schließen — so bleibt das Logo sichtbar,
            // bis das Hauptfenster wirklich auf dem Screen ist.
            splash.CloseSplash();
        };
        splash.Show();
    }

    private void LaunchMainWindow()
    {
        _mainWindow = new MainWindow();

        var settings = SettingsService.Load();
        if (!double.IsNaN(settings.WindowLeft) && !double.IsNaN(settings.WindowTop))
        {
            _mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            _mainWindow.Left = settings.WindowLeft;
            _mainWindow.Top = settings.WindowTop;
        }
        _mainWindow.Width = settings.WindowWidth;
        _mainWindow.Height = settings.WindowHeight;

        _mainWindow.Show();
        _mainWindow.Activate();

        InitTray();

        WeakReferenceMessenger.Default.Register<MemoryThresholdMessage>(this, (_, msg) =>
        {
            if (_thresholdNotified) return;
            _thresholdNotified = true;
            _trayIcon?.ShowBalloonTip(
                "RAM Dump",
                $"RAM-Auslastung bei {msg.UsagePercent:F0}%!",
                BalloonIcon.Warning);
            _ = Task.Delay(60_000).ContinueWith(_ => _thresholdNotified = false);
        });
    }

    private void InitTray()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "RAM Dump",
            IconSource = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/Resources/app.ico")),
        };

        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();

        var menu = new System.Windows.Controls.ContextMenu();
        var openItem = new System.Windows.Controls.MenuItem { Header = "Öffnen" };
        openItem.Click += (_, _) => ShowMainWindow();
        var exitItem = new System.Windows.Controls.MenuItem { Header = "Beenden" };
        exitItem.Click += (_, _) =>
        {
            _trayIcon?.Dispose();
            _trayIcon = null;
            Shutdown();
        };
        menu.Items.Add(openItem);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(exitItem);
        _trayIcon.ContextMenu = menu;
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        var vm = _mainWindow?.DataContext as RamDump.ViewModels.MainViewModel;
        vm?.Monitor.Dispose();
        vm?.About.Dispose();
        base.OnExit(e);
    }
}
