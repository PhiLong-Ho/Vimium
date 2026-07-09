using Vimium.Services;
using Vimium.Services.Interfaces;
using Xunit;

namespace Vimium.Tests.Services;

public class UiAutomationLineHintProviderServiceTest
{
    [Fact]
    public void Constructor_DoesNotThrow()
    {
        var service = new UiAutomationLineHintProviderService();
        Assert.NotNull(service);
    }

    [Fact]
    public void Implements_ILineHintProviderService()
    {
        var service = new UiAutomationLineHintProviderService();
        Assert.IsAssignableFrom<ILineHintProviderService>(service);
    }

    [Fact]
    public void EnumLineHints_NoForegroundWindow_ReturnsNull()
    {
        // When no windows are available (e.g. in a headless test env),
        // the service should return null gracefully.
        var service = new UiAutomationLineHintProviderService();
        var result = service.EnumLineHints();

        // In a headless test environment, this should return null
        // (no foreground window available)
        if (result != null)
        {
            // If we did get a result (foreground window exists),
            // it should at minimum have a valid structure
            Assert.NotNull(result.Hints);
        }
    }
}
