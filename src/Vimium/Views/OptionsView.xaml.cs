using System.Windows;
using System.Windows.Input;
using Vimium.ViewModels;

namespace Vimium.Views;

public partial class OptionsView : Window
{
    public OptionsView()
    {
        InitializeComponent();
    }

    /// <summary>Arrow-key navigation within the sidebar ListBox.</summary>
    private void SidebarList_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not OptionsViewModel vm) return;

        switch (e.Key)
        {
            case Key.Down:
                vm.SelectNextPage();
                e.Handled = true;
                break;
            case Key.Up:
                vm.SelectPreviousPage();
                e.Handled = true;
                break;
        }
    }
}
