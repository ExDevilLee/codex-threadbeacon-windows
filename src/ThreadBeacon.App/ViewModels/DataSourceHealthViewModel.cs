using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.ViewModels;

public sealed class DataSourceHealthViewModel : INotifyPropertyChanged
{
    private static readonly DataSourceHealthReport InitialReport = new(
        DataSourceHealthStatus.NotUsed,
        DataSourceHealthStatus.NotUsed,
        DataSourceHealthStatus.NotUsed,
        DataSourceHealthStatus.NotUsed,
        0,
        0,
        null);

    public DataSourceHealthViewModel()
    {
        Sources =
        [
            new DataSourceHealthRowViewModel("任务数据库"),
            new DataSourceHealthRowViewModel("Rename 索引"),
            new DataSourceHealthRowViewModel("Rollout"),
            new DataSourceHealthRowViewModel("服务日志"),
        ];
        Report = InitialReport;
        Update(InitialReport);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DataSourceHealthReport Report { get; private set; }

    public IReadOnlyList<DataSourceHealthRowViewModel> Sources { get; }

    public OverallDataSourceHealth OverallStatus => Report.OverallStatus;

    public string Summary => Report.Summary;

    public string AccessibilityLabel => $"数据源健康：{Summary}";

    public string OverallGlyph => OverallStatus switch
    {
        OverallDataSourceHealth.Healthy => "\uE73E",
        OverallDataSourceHealth.Degraded => "\uE7BA",
        OverallDataSourceHealth.Unavailable => "\uEA39",
        _ => string.Empty,
    };

    public Brush OverallBrush => HealthBrush(OverallStatus switch
    {
        OverallDataSourceHealth.Healthy => DataSourceHealthLevel.Healthy,
        OverallDataSourceHealth.Degraded => DataSourceHealthLevel.Degraded,
        OverallDataSourceHealth.Unavailable => DataSourceHealthLevel.Unavailable,
        _ => DataSourceHealthLevel.NotUsed,
    });

    public string LastSuccessfulRefreshText =>
        Report.LastSuccessfulRefreshAt is DateTimeOffset refreshedAt
            ? $"最后成功刷新：{refreshedAt.ToLocalTime():HH:mm:ss}"
            : "尚无成功刷新记录";

    public string RolloutCountsText =>
        Report.RolloutSuccessCount + Report.RolloutFailureCount > 0
            ? $"成功 {Report.RolloutSuccessCount} | 失败 {Report.RolloutFailureCount}"
            : string.Empty;

    public void Update(DataSourceHealthReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        Report = report;
        Sources[0].Update(report.TaskDatabase);
        Sources[1].Update(report.RenameIndex);
        Sources[2].Update(report.Rollout, RolloutCountsText);
        Sources[3].Update(report.ServiceLogs);
        OnPropertyChanged(nameof(Report));
        OnPropertyChanged(nameof(OverallStatus));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(AccessibilityLabel));
        OnPropertyChanged(nameof(OverallGlyph));
        OnPropertyChanged(nameof(OverallBrush));
        OnPropertyChanged(nameof(LastSuccessfulRefreshText));
        OnPropertyChanged(nameof(RolloutCountsText));
    }

    internal static Brush HealthBrush(DataSourceHealthLevel level) => level switch
    {
        DataSourceHealthLevel.Healthy => Brushes.SeaGreen,
        DataSourceHealthLevel.Degraded => Brushes.DarkGoldenrod,
        DataSourceHealthLevel.Unavailable => Brushes.IndianRed,
        _ => Brushes.DimGray,
    };

    internal static string HealthGlyph(DataSourceHealthLevel level) => level switch
    {
        DataSourceHealthLevel.Healthy => "\uE73E",
        DataSourceHealthLevel.Degraded => "\uE7BA",
        DataSourceHealthLevel.Unavailable => "\uEA39",
        _ => "\uE738",
    };

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class DataSourceHealthRowViewModel : INotifyPropertyChanged
{
    private DataSourceHealthStatus status = DataSourceHealthStatus.NotUsed;
    private string supplementalText = string.Empty;

    public DataSourceHealthRowViewModel(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title { get; }

    public DataSourceHealthLevel Level => status.Level;

    public string StatusText => status.DisplayText;

    public string DetailText => status.DetailText ?? string.Empty;

    public bool HasDetail => !string.IsNullOrEmpty(DetailText);

    public string SupplementalText => supplementalText;

    public bool HasSupplementalText => !string.IsNullOrEmpty(SupplementalText);

    public string Glyph => DataSourceHealthViewModel.HealthGlyph(Level);

    public Brush Brush => DataSourceHealthViewModel.HealthBrush(Level);

    public void Update(DataSourceHealthStatus value, string? supplemental = null)
    {
        status = value ?? throw new ArgumentNullException(nameof(value));
        supplementalText = supplemental ?? string.Empty;
        OnPropertyChanged(nameof(Level));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(DetailText));
        OnPropertyChanged(nameof(HasDetail));
        OnPropertyChanged(nameof(SupplementalText));
        OnPropertyChanged(nameof(HasSupplementalText));
        OnPropertyChanged(nameof(Glyph));
        OnPropertyChanged(nameof(Brush));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
