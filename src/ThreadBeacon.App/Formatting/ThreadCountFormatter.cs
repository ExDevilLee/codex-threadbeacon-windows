using ThreadBeacon.Core.Models;
using ThreadBeacon.App.Localization;

namespace ThreadBeacon.App.Formatting;

public sealed record ThreadCountLabel(
    string DisplayText,
    string AccessibilityLabel);

public static class ThreadCountFormatter
{
    public static ThreadCountLabel Format(
        IEnumerable<ThreadStatus> statuses,
        AppLanguage language = AppLanguage.SimplifiedChinese)
    {
        ArgumentNullException.ThrowIfNull(statuses);

        int runningCount = 0;
        int visibleCount = 0;
        foreach (ThreadStatus status in statuses)
        {
            visibleCount++;
            if (status is ThreadStatus.Running)
            {
                runningCount++;
            }
        }

        return new ThreadCountLabel(
            $"{runningCount}/{visibleCount}",
            AppLanguageText.ThreadCountAccessibility(language, runningCount, visibleCount));
    }
}
