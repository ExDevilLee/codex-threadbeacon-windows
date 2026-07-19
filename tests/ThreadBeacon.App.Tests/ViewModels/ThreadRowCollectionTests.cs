using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class ThreadRowCollectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Reconcile_ExistingThread_PreservesIdentityAndUpdatesValues()
    {
        var collection = new ThreadRowCollection();
        collection.Reconcile([Snapshot("a", "Initial", 1_000)], Now);
        ThreadRowViewModel original = collection.Items.Single();

        collection.Reconcile([Snapshot("a", "Renamed", 2_000)], Now.AddSeconds(2));

        Assert.Same(original, collection.Items.Single());
        Assert.Equal("Renamed", original.Title);
        Assert.Equal("2K", original.TokenText);
        Assert.NotNull(original.TokenDetails);
        Assert.Equal("2K", original.TokenDetails.Rows[0].Value);
    }

    [Fact]
    public void Reconcile_InsertsAndRemovesThreadsInLatestOrder()
    {
        var collection = new ThreadRowCollection();
        collection.Reconcile(
            [Snapshot("a", "A", 100), Snapshot("b", "B", 200)],
            Now);
        ThreadRowViewModel retained = collection.Items[1];

        collection.Reconcile(
            [Snapshot("c", "C", 300), Snapshot("b", "B", 250)],
            Now.AddSeconds(2));

        Assert.Equal(["c", "b"], collection.Items.Select(row => row.Id));
        Assert.Same(retained, collection.Items[1]);
        Assert.DoesNotContain(collection.Items, row => row.Id == "a");
    }

    [Fact]
    public void Reconcile_ReordersExistingThreadsWithoutReplacingThem()
    {
        var collection = new ThreadRowCollection();
        collection.Reconcile(
            [Snapshot("a", "A", 100), Snapshot("b", "B", 200)],
            Now);
        ThreadRowViewModel first = collection.Items[0];
        ThreadRowViewModel second = collection.Items[1];

        collection.Reconcile(
            [Snapshot("b", "B", 210), Snapshot("a", "A", 110)],
            Now.AddSeconds(2));

        Assert.Same(second, collection.Items[0]);
        Assert.Same(first, collection.Items[1]);
    }

    [Fact]
    public void Reconcile_ExistingThreadPreservesIdentityAndUpdatesSubagentCount()
    {
        var collection = new ThreadRowCollection();
        collection.Reconcile([Snapshot("a", "Task", 100, subagentCount: 1)], Now);
        ThreadRowViewModel original = collection.Items.Single();

        collection.Reconcile(
            [Snapshot("a", "Task", 100, subagentCount: 4)],
            Now.AddSeconds(2));

        Assert.Same(original, collection.Items.Single());
        Assert.Equal(4, original.SubagentCount);
        Assert.True(original.HasSubagents);
        Assert.Equal("4", original.SubagentCountText);
        Assert.Equal("4 个 Subagent", original.SubagentAccessibilityLabel);
    }

    [Fact]
    public void Reconcile_ExpandedThreadPreservesChildIdentityAndUpdatesValues()
    {
        var collection = new ThreadRowCollection();
        collection.Reconcile(
            [Snapshot("a", "Task", 100, 1, [Subagent("child", "Initial")])],
            Now,
            new HashSet<string>(StringComparer.Ordinal) { "a" });
        ThreadRowViewModel parent = collection.Items.Single();
        SubagentRowViewModel child = parent.Subagents.Single();

        collection.Reconcile(
            [Snapshot("a", "Task", 100, 1, [Subagent("child", "Renamed")])],
            Now.AddSeconds(2),
            new HashSet<string>(StringComparer.Ordinal) { "a" });

        Assert.Same(parent, collection.Items.Single());
        Assert.Same(child, parent.Subagents.Single());
        Assert.Equal("Renamed", child.Title);
        Assert.True(parent.IsSubagentExpanded);
    }

    [Fact]
    public void Reconcile_UpdatesFavoriteAndArchivedStateOnRetainedRow()
    {
        var collection = new ThreadRowCollection();
        collection.Reconcile([Snapshot("a", "Task", 100)], Now);
        ThreadRowViewModel row = Assert.Single(collection.Items);

        collection.Reconcile(
            [Snapshot("a", "Task", 100, isArchived: true)],
            Now.AddSeconds(2),
            favoriteThreadIds: new HashSet<string>(StringComparer.Ordinal) { "a" });

        Assert.Same(row, Assert.Single(collection.Items));
        Assert.True(row.IsFavorite);
        Assert.True(row.IsArchived);
        Assert.Equal("已归档", row.StatusLabel);
    }

    private static ThreadSnapshot Snapshot(
        string id,
        string title,
        long tokens,
        int subagentCount = 0,
        IReadOnlyList<SubagentSnapshot>? subagents = null,
        bool isArchived = false) =>
        new(
            id,
            title,
            ThreadStatus.Running,
            Now.AddMinutes(-1),
            Now,
            Now,
            Now.AddMinutes(-1),
            null,
            new TokenUsageSnapshot(
                tokens,
                new TokenUsage(tokens, tokens / 2, 0, 0, tokens),
                null,
                Now),
            subagentCount,
            RolloutSourceStatus.Healthy,
            subagents,
            isArchived: isArchived);

    private static SubagentSnapshot Subagent(string id, string title) =>
        new(
            id,
            title,
            ThreadStatus.Running,
            Now.AddMinutes(-1),
            Now,
            Now,
            new TokenUsageSnapshot(100, null, null, Now),
            "worker",
            "reviewer",
            "gpt-test",
            "high",
            RolloutSourceStatus.Healthy);
}
