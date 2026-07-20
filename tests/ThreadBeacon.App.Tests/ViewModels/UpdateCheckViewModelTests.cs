using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class UpdateCheckViewModelTests
{
    private static readonly SemanticVersion Current = new(0, 5, 0);

    [Fact]
    public async Task CheckAsync_ExposesNewerReleaseAndOpenCommand()
    {
        var available = new AvailableUpdate(
            new SemanticVersion(0, 6, 0),
            new Uri("https://example.test/release"),
            false);
        Uri? opened = null;
        var viewModel = new UpdateCheckViewModel(
            new StubReleaseClient(available),
            Current,
            uri => opened = uri);

        await viewModel.CheckAsync();

        Assert.Equal(UpdateCheckStatus.Available, viewModel.Status);
        Assert.True(viewModel.IsUpdateAvailable);
        Assert.Equal("0.6.0", viewModel.LatestVersionText);
        Assert.True(viewModel.OpenReleaseCommand.CanExecute(null));
        viewModel.OpenReleaseCommand.Execute(null);
        Assert.Equal(available.ReleaseUrl, opened);
    }

    [Fact]
    public async Task CheckAsync_TreatsSameOrOlderReleaseAsUpToDate()
    {
        var viewModel = new UpdateCheckViewModel(
            new StubReleaseClient(new AvailableUpdate(
                Current,
                new Uri("https://example.test/current"),
                false)),
            Current,
            _ => throw new InvalidOperationException());

        await viewModel.CheckAsync();

        Assert.Equal(UpdateCheckStatus.UpToDate, viewModel.Status);
        Assert.False(viewModel.IsUpdateAvailable);
        Assert.False(viewModel.OpenReleaseCommand.CanExecute(null));
    }

    [Fact]
    public async Task CheckAsync_ConvertsNetworkFailureIntoNonBlockingStatus()
    {
        var viewModel = new UpdateCheckViewModel(
            new StubReleaseClient(new HttpRequestException("offline")),
            Current,
            _ => { });

        await viewModel.CheckAsync();

        Assert.Equal(UpdateCheckStatus.Failed, viewModel.Status);
        Assert.False(viewModel.IsUpdateAvailable);
    }

    private sealed class StubReleaseClient : IReleaseClient
    {
        private readonly AvailableUpdate? result;
        private readonly Exception? exception;

        public StubReleaseClient(AvailableUpdate? result) => this.result = result;

        public StubReleaseClient(Exception exception) => this.exception = exception;

        public Task<AvailableUpdate?> GetLatestAsync(CancellationToken cancellationToken) =>
            exception is null
                ? Task.FromResult(result)
                : Task.FromException<AvailableUpdate?>(exception);
    }
}
