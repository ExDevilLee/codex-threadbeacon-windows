using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ThreadBeacon.App.Commands;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ThreadStatusLoader loader;
    private readonly ThreadRowCollection threadRows = new();
    private readonly SemaphoreSlim refreshGate = new(1, 1);
    private readonly AsyncRelayCommand refreshCommand;
    private bool isRefreshing;
    private string statusText = "准备监听";
    private string updatedText = string.Empty;

    public MainWindowViewModel(ThreadStatusLoader loader, WindowPinState windowPin)
    {
        this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
        WindowPin = windowPin ?? throw new ArgumentNullException(nameof(windowPin));
        refreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsRefreshing);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ThreadRowViewModel> Threads => threadRows.Items;

    public WindowPinState WindowPin { get; }

    public AsyncRelayCommand RefreshCommand => refreshCommand;

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
        get => statusText;
        private set => SetField(ref statusText, value);
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

    public async Task RefreshAsync()
    {
        if (!await refreshGate.WaitAsync(0))
        {
            return;
        }

        IsRefreshing = true;
        try
        {
            ThreadSnapshotLoadResult result = await Task.Run(() => loader.Load());
            ReplaceThreads(result);
            StatusText = GetStatusText(result);
            UpdatedText = result.RefreshedAt.ToLocalTime().ToString("HH:mm:ss");
        }
        catch
        {
            StatusText = "刷新失败";
        }
        finally
        {
            IsRefreshing = false;
            refreshGate.Release();
        }
    }

    private void ReplaceThreads(ThreadSnapshotLoadResult result)
    {
        threadRows.Reconcile(result.Threads, result.RefreshedAt);

        OnPropertyChanged(nameof(EmptyVisibility));
        OnPropertyChanged(nameof(ListVisibility));
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
}
