using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using ThreadBeacon.App.Localization;
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
    private AppLanguage language = AppLanguage.SimplifiedChinese;

    public DataSourceHealthViewModel()
    {
        Sources =
        [
            new DataSourceHealthRowViewModel(0),
            new DataSourceHealthRowViewModel(1),
            new DataSourceHealthRowViewModel(2),
            new DataSourceHealthRowViewModel(3),
        ];
        Report = InitialReport;
        Update(InitialReport);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DataSourceHealthReport Report { get; private set; }

    public IReadOnlyList<DataSourceHealthRowViewModel> Sources { get; }

    public OverallDataSourceHealth OverallStatus => Report.OverallStatus;

    public string Summary => AppLanguageText.HealthSummary(language, OverallStatus);

    public string AccessibilityLabel => language is AppLanguage.SimplifiedChinese
        ? $"数据源健康：{Summary}"
        : $"Data source health: {Summary}";

    public string OverallGlyph => OverallStatus switch
    {
        OverallDataSourceHealth.Healthy => "\uE73E",
        OverallDataSourceHealth.Degraded => "\uE7BA",
        OverallDataSourceHealth.Unavailable => "\uEA39",
        _ => string.Empty,
    };

    public string HealthButtonBaseGlyph => OverallStatus switch
    {
        OverallDataSourceHealth.Healthy => "\uEA18",
        OverallDataSourceHealth.Degraded => "\uE7BA",
        OverallDataSourceHealth.Unavailable => "\uEA39",
        _ => string.Empty,
    };

    public string HealthButtonOverlayGlyph => OverallStatus is OverallDataSourceHealth.Healthy
        ? "\uE73E"
        : string.Empty;

    public Brush OverallBrush => HealthBrush(OverallStatus switch
    {
        OverallDataSourceHealth.Healthy => DataSourceHealthLevel.Healthy,
        OverallDataSourceHealth.Degraded => DataSourceHealthLevel.Degraded,
        OverallDataSourceHealth.Unavailable => DataSourceHealthLevel.Unavailable,
        _ => DataSourceHealthLevel.NotUsed,
    });

    public string LastSuccessfulRefreshText =>
        Report.LastSuccessfulRefreshAt is DateTimeOffset refreshedAt
            ? language is AppLanguage.SimplifiedChinese
                ? $"最后成功刷新：{refreshedAt.ToLocalTime():HH:mm:ss}"
                : $"Last successful refresh: {refreshedAt.ToLocalTime():HH:mm:ss}"
            : language is AppLanguage.SimplifiedChinese
                ? "尚无成功刷新记录"
                : "No successful refresh yet";

    public string RolloutCountsText =>
        Report.RolloutSuccessCount + Report.RolloutFailureCount > 0
            ? language is AppLanguage.SimplifiedChinese
                ? $"成功 {Report.RolloutSuccessCount} | 失败 {Report.RolloutFailureCount}"
                : $"Success {Report.RolloutSuccessCount} | Failures {Report.RolloutFailureCount}"
            : string.Empty;

    public void Update(
        DataSourceHealthReport report,
        AppLanguage language = AppLanguage.SimplifiedChinese)
    {
        ArgumentNullException.ThrowIfNull(report);
        Report = report;
        this.language = language;
        Sources[0].Update(report.TaskDatabase, language: language);
        Sources[1].Update(report.RenameIndex, language: language);
        Sources[2].Update(report.Rollout, RolloutCountsText, language);
        Sources[3].Update(report.ServiceLogs, language: language);
        OnPropertyChanged(nameof(Report));
        OnPropertyChanged(nameof(OverallStatus));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(AccessibilityLabel));
        OnPropertyChanged(nameof(OverallGlyph));
        OnPropertyChanged(nameof(HealthButtonBaseGlyph));
        OnPropertyChanged(nameof(HealthButtonOverlayGlyph));
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
    private readonly int sourceIndex;
    private DataSourceHealthStatus status = DataSourceHealthStatus.NotUsed;
    private string supplementalText = string.Empty;
    private AppLanguage language = AppLanguage.SimplifiedChinese;

    public DataSourceHealthRowViewModel(int sourceIndex)
    {
        this.sourceIndex = sourceIndex;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title => AppLanguageText.HealthSourceTitle(language, sourceIndex);

    public DataSourceHealthLevel Level => status.Level;

    public string StatusText => AppLanguageText.HealthStatus(language, status.Level);

    public string DetailText => AppLanguageText.HealthDetail(language, status.DetailText);

    public bool HasDetail => !string.IsNullOrEmpty(DetailText);

    public string SupplementalText => supplementalText;

    public bool HasSupplementalText => !string.IsNullOrEmpty(SupplementalText);

    public string Glyph => DataSourceHealthViewModel.HealthGlyph(Level);

    public Brush Brush => DataSourceHealthViewModel.HealthBrush(Level);

    public void Update(
        DataSourceHealthStatus value,
        string? supplemental = null,
        AppLanguage language = AppLanguage.SimplifiedChinese)
    {
        status = value ?? throw new ArgumentNullException(nameof(value));
        supplementalText = supplemental ?? string.Empty;
        this.language = language;
        OnPropertyChanged(nameof(Title));
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
