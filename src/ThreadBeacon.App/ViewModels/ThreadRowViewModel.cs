using System.Globalization;
using System.Windows.Media;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.ViewModels;

public sealed class ThreadRowViewModel
{
    private static readonly Brush ErrorBrush = CreateBrush(0xE5, 0x48, 0x4D);
    private static readonly Brush WarningBrush = CreateBrush(0xF5, 0xA5, 0x24);
    private static readonly Brush RunningBrush = CreateBrush(0x30, 0xA4, 0x6C);
    private static readonly Brush CompletedBrush = CreateBrush(0x00, 0x7C, 0x91);
    private static readonly Brush IdleBrush = CreateBrush(0x8E, 0x8E, 0x93);
    private static readonly Brush UnknownBrush = CreateBrush(0xA0, 0xA0, 0xA6);

    public ThreadRowViewModel(ThreadSnapshot snapshot, DateTimeOffset now)
    {
        Id = snapshot.Id;
        Title = snapshot.Title;
        Status = snapshot.Status;
        StatusLabel = GetStatusLabel(snapshot.Status);
        StatusBrush = GetStatusBrush(snapshot.Status);
        TokenText = FormatTokens(snapshot.TokenUsage?.TotalTokens);
        DurationText = FormatDuration(now - snapshot.StatusChangedAt);
    }

    public string Id { get; }

    public string Title { get; }

    public ThreadStatus Status { get; }

    public string StatusLabel { get; }

    public Brush StatusBrush { get; }

    public string TokenText { get; }

    public string DurationText { get; }

    private static string GetStatusLabel(ThreadStatus status) => status switch
    {
        ThreadStatus.Error => "错误",
        ThreadStatus.NeedsAction => "待处理",
        ThreadStatus.Warning => "重试中",
        ThreadStatus.Running => "运行中",
        ThreadStatus.JustCompleted => "刚完成",
        ThreadStatus.Idle => "空闲",
        ThreadStatus.Unknown => "未知",
        _ => "未知",
    };

    private static Brush GetStatusBrush(ThreadStatus status) => status switch
    {
        ThreadStatus.Error or ThreadStatus.NeedsAction => ErrorBrush,
        ThreadStatus.Warning => WarningBrush,
        ThreadStatus.Running => RunningBrush,
        ThreadStatus.JustCompleted => CompletedBrush,
        ThreadStatus.Idle => IdleBrush,
        _ => UnknownBrush,
    };

    private static string FormatTokens(long? tokens)
    {
        if (tokens is null or < 0)
        {
            return "—";
        }

        return tokens.Value switch
        {
            < 1_000 => tokens.Value.ToString(CultureInfo.InvariantCulture),
            < 1_000_000 => $"{tokens.Value / 1_000d:0.#}K",
            < 1_000_000_000 => $"{tokens.Value / 1_000_000d:0.#}M",
            _ => $"{tokens.Value / 1_000_000_000d:0.#}B",
        };
    }

    private static string FormatDuration(TimeSpan elapsed)
    {
        elapsed = elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        return elapsed.TotalSeconds switch
        {
            < 60 => $"{Math.Floor(elapsed.TotalSeconds)}秒",
            < 3_600 => $"{Math.Floor(elapsed.TotalMinutes)}分",
            < 86_400 => $"{Math.Floor(elapsed.TotalHours)}时",
            _ => $"{Math.Floor(elapsed.TotalDays)}天",
        };
    }

    private static Brush CreateBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}
