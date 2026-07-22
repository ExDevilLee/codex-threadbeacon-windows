using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.ViewModels;

internal static class StatusGlyphFormatter
{
    public static string Format(ThreadStatus status) => status switch
    {
        ThreadStatus.Error => "\uE711",
        ThreadStatus.NeedsAction => "\uEA39",
        ThreadStatus.Warning => "\uE7BA",
        ThreadStatus.Running => "\uE768",
        ThreadStatus.JustCompleted => "\uE930",
        ThreadStatus.Idle => "\uE738",
        _ => "\uE897",
    };
}
