using System.Windows;

namespace RamDump.Views;

public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
        YesButton.Click += (_, _) => { DialogResult = true; Close(); };
        NoButton.Click += (_, _) => { DialogResult = false; Close(); };
    }

    public static bool Confirm(Window owner, string message)
    {
        var dlg = new ConfirmationDialog(message) { Owner = owner };
        return dlg.ShowDialog() == true;
    }
}
