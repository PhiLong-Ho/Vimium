using System.ComponentModel;
using Vimium.Services;
using Xunit;

namespace Vimium.Tests.Services;

/// <summary>
/// Tests for the <see cref="ConfigService.RunAsAdministrator"/> convenience
/// property (feature 005). ConfigService is a disk-persisting singleton, so
/// each test saves and restores the original value to avoid cross-test bleed.
/// </summary>
[Collection(Vimium.Tests.ConfigSingletonCollection.Name)]
public class ConfigServiceTest
{
    [Fact]
    public void RunAsAdministrator_Get_ReflectsCurrentConfig()
    {
        var svc = ConfigService.Instance;
        var original = svc.RunAsAdministrator;
        try
        {
            svc.RunAsAdministrator = false;
            Assert.False(svc.RunAsAdministrator);

            svc.RunAsAdministrator = true;
            Assert.True(svc.RunAsAdministrator);
        }
        finally
        {
            svc.RunAsAdministrator = original;
        }
    }

    [Fact]
    public void RunAsAdministrator_Set_RaisesPropertyChanged()
    {
        var svc = ConfigService.Instance;
        var original = svc.RunAsAdministrator;
        try
        {
            // Ensure a known starting state so the toggle actually changes value.
            svc.RunAsAdministrator = true;

            var raised = new List<string?>();
            PropertyChangedEventHandler handler = (_, e) => raised.Add(e.PropertyName);
            svc.PropertyChanged += handler;
            try
            {
                svc.RunAsAdministrator = false;
            }
            finally
            {
                svc.PropertyChanged -= handler;
            }

            Assert.Contains(nameof(ConfigService.RunAsAdministrator), raised);
            Assert.Contains(nameof(ConfigService.IsDirty), raised);
        }
        finally
        {
            svc.RunAsAdministrator = original;
        }
    }

    [Fact]
    public void RunAsAdministrator_Set_SameValue_DoesNotRaise()
    {
        var svc = ConfigService.Instance;
        var original = svc.RunAsAdministrator;
        try
        {
            svc.RunAsAdministrator = true;

            var raised = new List<string?>();
            PropertyChangedEventHandler handler = (_, e) => raised.Add(e.PropertyName);
            svc.PropertyChanged += handler;
            try
            {
                svc.RunAsAdministrator = true; // no change
            }
            finally
            {
                svc.PropertyChanged -= handler;
            }

            Assert.DoesNotContain(nameof(ConfigService.RunAsAdministrator), raised);
        }
        finally
        {
            svc.RunAsAdministrator = original;
        }
    }
}
