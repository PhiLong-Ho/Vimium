using System.Windows;
using System.Windows.Controls;

namespace Vimium.Views;

public partial class ShellView : Window
{
    /// <summary>Exposed so ShellViewModel can dismiss it before showing the overlay.</summary>
    public static ContextMenu? TrayMenu { get; private set; }

    public ShellView()
    {
        InitializeComponent();
        TrayMenu = TrayContextMenu;
    }
}
