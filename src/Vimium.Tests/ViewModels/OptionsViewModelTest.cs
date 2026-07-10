using Vimium.ViewModels;
using Xunit;

namespace Vimium.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="OptionsViewModel"/> (feature 005 — version display).
/// </summary>
public class OptionsViewModelTest
{
    [Fact]
    public void AppVersion_MatchesAssemblyVersion()
    {
        var vm = new OptionsViewModel();

        Assert.Equal(System.AssemblyVersionInformation.Version, vm.AppVersion);
    }

    [Fact]
    public void AppVersion_IsNotEmpty()
    {
        var vm = new OptionsViewModel();

        Assert.False(string.IsNullOrWhiteSpace(vm.AppVersion));
    }
}
