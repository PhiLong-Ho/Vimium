using System.Windows;

namespace Vimium.Views;

public partial class ShellView : Window
{
    /// <summary>True while the tray context menu is visible.</summary>
    public static bool IsTrayMenuOpen { get; private set; }

    public ShellView()
    {
        InitializeComponent();
    }

    private void TrayContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        IsTrayMenuOpen = true;
    }

    private void TrayContextMenu_Closed(object sender, RoutedEventArgs e)
    {
        IsTrayMenuOpen = false;
    }
}
