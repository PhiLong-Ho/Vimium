using Vimium.Services;
using Vimium.ViewModels;
using Xunit;

namespace Vimium.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="GeneralSettingsViewModel"/> admin-mode bindings
/// (feature 005). The view model delegates to the ConfigService singleton,
/// so each test restores the original config value it touched.
/// </summary>
[Collection(Vimium.Tests.ConfigSingletonCollection.Name)]
public class GeneralSettingsViewModelTest
{
    [Fact]
    public void RunAsAdministrator_Get_ForwardsToConfigService()
    {
        var svc = ConfigService.Instance;
        var original = svc.RunAsAdministrator;
        try
        {
            svc.RunAsAdministrator = false;
            var vm = new GeneralSettingsViewModel();

            Assert.False(vm.RunAsAdministrator);

            svc.RunAsAdministrator = true;
            Assert.True(vm.RunAsAdministrator);
        }
        finally
        {
            svc.RunAsAdministrator = original;
        }
    }

    [Fact]
    public void RunAsAdministrator_Set_ForwardsToConfigService()
    {
        var svc = ConfigService.Instance;
        var original = svc.RunAsAdministrator;
        try
        {
            svc.RunAsAdministrator = true;
            var vm = new GeneralSettingsViewModel();

            vm.RunAsAdministrator = false;

            Assert.False(svc.RunAsAdministrator);
        }
        finally
        {
            svc.RunAsAdministrator = original;
        }
    }

    [Fact]
    public void ShowRestartMessage_HiddenAtInit()
    {
        var svc = ConfigService.Instance;
        var original = svc.RunAsAdministrator;
        try
        {
            svc.RunAsAdministrator = true;
            var vm = new GeneralSettingsViewModel();

            // No user interaction yet — message must be hidden.
            Assert.False(vm.ShowRestartMessage);
        }
        finally
        {
            svc.RunAsAdministrator = original;
        }
    }

    [Fact]
    public void ShowRestartMessage_VisibleAfterToggle()
    {
        var svc = ConfigService.Instance;
        var original = svc.RunAsAdministrator;
        try
        {
            svc.RunAsAdministrator = true;
            var vm = new GeneralSettingsViewModel();

            vm.RunAsAdministrator = false; // user toggles

            Assert.True(vm.ShowRestartMessage);
        }
        finally
        {
            svc.RunAsAdministrator = original;
        }
    }

    [Fact]
    public void ShowRestartMessage_HiddenAgain_WhenToggledBackToInitial()
    {
        var svc = ConfigService.Instance;
        var original = svc.RunAsAdministrator;
        try
        {
            svc.RunAsAdministrator = true;
            var vm = new GeneralSettingsViewModel();

            vm.RunAsAdministrator = false;
            vm.RunAsAdministrator = true; // back to the value at open time

            Assert.False(vm.ShowRestartMessage);
        }
        finally
        {
            svc.RunAsAdministrator = original;
        }
    }
}
