using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using ThreadBeacon.App.Commands;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.App.ViewModels;

public enum UpdateCheckStatus
{
    Idle,
    Checking,
    UpToDate,
    Available,
    Failed,
}

public sealed class UpdateCheckViewModel : INotifyPropertyChanged
{
    private readonly IReleaseClient releaseClient;
    private readonly SemanticVersion currentVersion;
    private readonly Action<Uri> openUri;
    private readonly SemaphoreSlim checkGate = new(1, 1);
    private AvailableUpdate? availableUpdate;
    private UpdateCheckStatus status;

    public UpdateCheckViewModel(
        IReleaseClient releaseClient,
        SemanticVersion currentVersion,
        Action<Uri> openUri)
    {
        this.releaseClient = releaseClient ?? throw new ArgumentNullException(nameof(releaseClient));
        this.currentVersion = currentVersion;
        this.openUri = openUri ?? throw new ArgumentNullException(nameof(openUri));
        CheckCommand = new AsyncRelayCommand(CheckAsync, () => Status is not UpdateCheckStatus.Checking);
        OpenReleaseCommand = new RelayCommand(OpenRelease, () => availableUpdate is not null);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AsyncRelayCommand CheckCommand { get; }

    public RelayCommand OpenReleaseCommand { get; }

    public UpdateCheckStatus Status
    {
        get => status;
        private set
        {
            if (SetField(ref status, value))
            {
                CheckCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsUpdateAvailable => Status is UpdateCheckStatus.Available;

    public string LatestVersionText => availableUpdate?.Version.ToString() ?? string.Empty;

    public async Task CheckAsync()
    {
        if (!await checkGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            Status = UpdateCheckStatus.Checking;
            AvailableUpdate? latest = await releaseClient.GetLatestAsync(CancellationToken.None);
            availableUpdate = latest is AvailableUpdate candidate
                && candidate.Version > currentVersion
                    ? candidate
                    : null;
            Status = availableUpdate is null
                ? UpdateCheckStatus.UpToDate
                : UpdateCheckStatus.Available;
        }
        catch (Exception exception) when (exception is
            HttpRequestException or
            TaskCanceledException or
            System.Text.Json.JsonException or
            KeyNotFoundException or
            UriFormatException or
            InvalidOperationException)
        {
            availableUpdate = null;
            Status = UpdateCheckStatus.Failed;
        }
        finally
        {
            OnPropertyChanged(nameof(IsUpdateAvailable));
            OnPropertyChanged(nameof(LatestVersionText));
            OpenReleaseCommand.NotifyCanExecuteChanged();
            checkGate.Release();
        }
    }

    private void OpenRelease()
    {
        if (availableUpdate is AvailableUpdate update)
        {
            openUri(update.ReleaseUrl);
        }
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
