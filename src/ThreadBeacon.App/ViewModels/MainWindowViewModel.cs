using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ThreadBeacon.App.Commands;
using ThreadBeacon.App.Formatting;
using ThreadBeacon.App.Sounds;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Notifications;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ThreadStatusLoader loader;
    private readonly ThreadRowCollection threadRows;
    private readonly HashSet<string> expandedThreadIds = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim refreshGate = new(1, 1);
    private readonly ICompletionNotificationObserver? completionObserver;
    private readonly AsyncRelayCommand refreshCommand;
    private ThreadCountLabel threadCountLabel = ThreadCountFormatter.Format([]);
    private bool isRefreshing;
    private string sourceStatusText = "准备监听";
    private string updatedText = string.Empty;
    private bool hasSourceError;

    public MainWindowViewModel(ThreadStatusLoader loader, WindowPinState windowPin)
        : this(loader, windowPin, new MonitoringState())
    {
    }

    public MainWindowViewModel(
        ThreadStatusLoader loader,
        WindowPinState windowPin,
        MonitoringState monitoring,
        ICompletionNotificationObserver? completionObserver = null)
    {
        this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
        WindowPin = windowPin ?? throw new ArgumentNullException(nameof(windowPin));
        Monitoring = monitoring ?? throw new ArgumentNullException(nameof(monitoring));
        this.completionObserver = completionObserver;
        threadRows = new ThreadRowCollection(ToggleSubagentsAsync);
        refreshCommand = new AsyncRelayCommand(
            () => RefreshAsync(RefreshNotificationPolicy.Baseline),
            () => !IsRefreshing);
        Monitoring.PropertyChanged += OnMonitoringPropertyChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ThreadRowViewModel> Threads => threadRows.Items;

    public WindowPinState WindowPin { get; }

    public MonitoringState Monitoring { get; }

    public AsyncRelayCommand RefreshCommand => refreshCommand;

    public string ThreadCountText => threadCountLabel.DisplayText;

    public string ThreadCountAccessibilityLabel => threadCountLabel.AccessibilityLabel;

    public bool IsRefreshing
    {
        get => isRefreshing;
        private set
        {
            if (SetField(ref isRefreshing, value))
            {
                refreshCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string StatusText
    {
        get
        {
            if (hasSourceError)
            {
                return sourceStatusText;
            }

            if (Monitoring.IsPaused)
            {
                return string.IsNullOrEmpty(updatedText)
                    ? "监听已暂停 · 尚未更新"
                    : "监听已暂停 · 上次更新";
            }

            return sourceStatusText;
        }
    }

    public string UpdatedText
    {
        get => updatedText;
        private set => SetField(ref updatedText, value);
    }

    public Visibility EmptyVisibility => Threads.Count == 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility ListVisibility => Threads.Count > 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public async Task RefreshAsync(
        RefreshNotificationPolicy policy = RefreshNotificationPolicy.Baseline)
    {
        if (!await refreshGate.WaitAsync(0))
        {
            return;
        }

        IsRefreshing = true;
        try
        {
            var requestedExpandedIds = new HashSet<string>(expandedThreadIds, StringComparer.Ordinal);
            ThreadSnapshotLoadResult result = await Task.Run(
                () => loader.Load(expandedThreadIds: requestedExpandedIds));
            ReplaceThreads(result);
            completionObserver?.Observe(result.Threads, policy);
            sourceStatusText = GetStatusText(result);
            hasSourceError = result.ThreadSourceStatus is not ThreadRepositoryStatus.Healthy;
            updatedText = result.RefreshedAt.ToLocalTime().ToString("HH:mm:ss");
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(UpdatedText));
        }
        catch
        {
            foreach (ThreadRowViewModel row in Threads.Where(
                row => expandedThreadIds.Contains(row.Id)))
            {
                row.MarkSubagentLoadFailed();
            }

            sourceStatusText = "刷新失败";
            hasSourceError = true;
            OnPropertyChanged(nameof(StatusText));
        }
        finally
        {
            IsRefreshing = false;
            refreshGate.Release();
        }
    }

    public async Task ToggleSubagentsAsync(string threadId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(threadId);
        ThreadRowViewModel? row = Threads.FirstOrDefault(
            candidate => StringComparer.Ordinal.Equals(candidate.Id, threadId));
        if (row is null || !row.HasSubagents)
        {
            return;
        }

        if (expandedThreadIds.Remove(threadId))
        {
            row.SetSubagentExpanded(isExpanded: false, isLoading: false);
            return;
        }

        expandedThreadIds.Add(threadId);
        row.SetSubagentExpanded(isExpanded: true, isLoading: true);
        await RefreshAsync(RefreshNotificationPolicy.Baseline);
    }

    private void ReplaceThreads(ThreadSnapshotLoadResult result)
    {
        expandedThreadIds.IntersectWith(result.Threads.Select(thread => thread.Id));
        threadRows.Reconcile(result.Threads, result.RefreshedAt, expandedThreadIds);
        SetThreadCountLabel(ThreadCountFormatter.Format(
            result.Threads.Select(thread => thread.Status)));

        OnPropertyChanged(nameof(EmptyVisibility));
        OnPropertyChanged(nameof(ListVisibility));
    }

    private void SetThreadCountLabel(ThreadCountLabel value)
    {
        if (threadCountLabel == value)
        {
            return;
        }

        threadCountLabel = value;
        OnPropertyChanged(nameof(ThreadCountText));
        OnPropertyChanged(nameof(ThreadCountAccessibilityLabel));
    }

    private static string GetStatusText(ThreadSnapshotLoadResult result)
    {
        if (result.ThreadSourceStatus is not ThreadRepositoryStatus.Healthy)
        {
            return result.ThreadSourceStatus switch
            {
                ThreadRepositoryStatus.Missing => "未找到 Codex 状态数据库",
                ThreadRepositoryStatus.Busy => "Codex 数据库繁忙",
                ThreadRepositoryStatus.Incompatible => "Codex 数据格式暂不兼容",
                _ => "Codex 数据暂不可用",
            };
        }

        string summary = $"监听中 · {result.Threads.Count} 个任务";
        return result.IsHealthy ? summary : $"{summary} · 部分数据降级";
    }

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

    private void OnMonitoringPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MonitoringState.IsPaused))
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }
}
