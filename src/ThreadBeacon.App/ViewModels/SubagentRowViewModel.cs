using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using ThreadBeacon.App.Formatting;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.ViewModels;

public sealed class SubagentRowViewModel : INotifyPropertyChanged
{
    private static readonly Brush ErrorBrush = CreateBrush(0xE5, 0x48, 0x4D);
    private static readonly Brush WarningBrush = CreateBrush(0xF5, 0xA5, 0x24);
    private static readonly Brush RunningBrush = CreateBrush(0x30, 0xA4, 0x6C);
    private static readonly Brush CompletedBrush = CreateBrush(0x00, 0x7C, 0x91);
    private static readonly Brush IdleBrush = CreateBrush(0x8E, 0x8E, 0x93);
    private static readonly Brush UnknownBrush = CreateBrush(0xA0, 0xA0, 0xA6);

    private string title = string.Empty;
    private string? alias;
    private string statusLabel = string.Empty;
    private Brush statusBrush = UnknownBrush;
    private string tokenText = "—";
    private string activityText = string.Empty;
    private string activityTooltip = string.Empty;
    private SubagentDetailViewModel details = null!;

    public SubagentRowViewModel(SubagentSnapshot snapshot, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Id = snapshot.Id;
        Update(snapshot, now);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }

    public string Title
    {
        get => title;
        private set => SetField(ref title, value);
    }

    public string? Alias
    {
        get => alias;
        private set
        {
            if (SetField(ref alias, value))
            {
                OnPropertyChanged(nameof(HasAlias));
            }
        }
    }

    public bool HasAlias => Alias is not null;

    public string StatusLabel
    {
        get => statusLabel;
        private set => SetField(ref statusLabel, value);
    }

    public Brush StatusBrush
    {
        get => statusBrush;
        private set => SetField(ref statusBrush, value);
    }

    public string TokenText
    {
        get => tokenText;
        private set => SetField(ref tokenText, value);
    }

    public string ActivityText
    {
        get => activityText;
        private set => SetField(ref activityText, value);
    }

    public string ActivityTooltip
    {
        get => activityTooltip;
        private set => SetField(ref activityTooltip, value);
    }

    public SubagentDetailViewModel Details
    {
        get => details;
        private set => SetField(ref details, value);
    }

    public void Update(SubagentSnapshot snapshot, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!StringComparer.Ordinal.Equals(Id, snapshot.Id))
        {
            throw new ArgumentException("A Subagent row cannot change its thread ID.", nameof(snapshot));
        }

        Title = string.IsNullOrWhiteSpace(snapshot.Title) ? "未命名 Subagent" : snapshot.Title;
        Alias = SubagentAliasFormatter.Format(snapshot.AgentNickname, Title);
        StatusLabel = SubagentDetailViewModel.StatusLabel(snapshot.Status);
        StatusBrush = StatusBrushFor(snapshot.Status);
        TokenText = TokenUsageFormatter.FormatCount(snapshot.TokenUsage?.TotalTokens);
        DateTimeOffset activityAt = snapshot.LatestEventAt ?? snapshot.UpdatedAt;
        ActivityText = RelativeActivityFormatter.Format(activityAt, now);
        ActivityTooltip = activityAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        Details = new SubagentDetailViewModel(snapshot);
    }

    private static Brush StatusBrushFor(ThreadStatus status) => status switch
    {
        ThreadStatus.Error or ThreadStatus.NeedsAction => ErrorBrush,
        ThreadStatus.Warning => WarningBrush,
        ThreadStatus.Running => RunningBrush,
        ThreadStatus.JustCompleted => CompletedBrush,
        ThreadStatus.Idle => IdleBrush,
        _ => UnknownBrush,
    };

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static Brush CreateBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}
