using ThreadBeacon.App.Startup;

namespace ThreadBeacon.App.Tests.Startup;

public sealed class WindowsLoginStartupServiceTests
{
    [Fact]
    public void Enable_WritesQuotedExecutablePathAndReportsEnabled()
    {
        var registry = new MemoryStartupRegistry();
        var service = new WindowsLoginStartupService(
            @"C:\Program Files\ThreadBeacon\ThreadBeacon.App.exe",
            registry);

        service.SetEnabled(true);

        Assert.Equal(
            "\"C:\\Program Files\\ThreadBeacon\\ThreadBeacon.App.exe\"",
            registry.Value);
        Assert.True(service.IsEnabled);
    }

    [Fact]
    public void Disable_RemovesCurrentUserRunValue()
    {
        var registry = new MemoryStartupRegistry
        {
            Value = "\"C:\\ThreadBeacon.App.exe\"",
        };
        var service = new WindowsLoginStartupService(@"C:\ThreadBeacon.App.exe", registry);

        service.SetEnabled(false);

        Assert.Null(registry.Value);
        Assert.False(service.IsEnabled);
    }

    private sealed class MemoryStartupRegistry : IStartupRegistry
    {
        public string? Value { get; set; }

        public string? Read() => Value;

        public void Write(string value) => Value = value;

        public void Delete() => Value = null;
    }
}
