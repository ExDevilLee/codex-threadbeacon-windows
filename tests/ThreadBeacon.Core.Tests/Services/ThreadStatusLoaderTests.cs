using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.Core.Tests.Services;

public sealed class ThreadStatusLoaderTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(10_000);

    [Fact]
    public void Load_MergesSourcesAndSortsByStatusPriority()
    {
        ThreadRecord[] records =
        [
            Record("missing", "Missing rollout", tokens: 50),
            Record("idle", "Idle"),
            Record("completed", "Completed"),
            Record("running", "Original running title", tokens: 999),
        ];
        var observations = new Dictionary<string, RolloutLoadResult>
        {
            ["running"] = HealthyObservation(
                ThreadStatus.Running,
                Now.AddSeconds(-10),
                Now.AddSeconds(-5),
                new TokenUsageSnapshot(1_000, null, null, Now.AddSeconds(-5))),
            ["completed"] = HealthyObservation(
                ThreadStatus.JustCompleted,
                Now.AddSeconds(-30),
                Now.AddSeconds(-30)),
            ["idle"] = HealthyObservation(
                ThreadStatus.JustCompleted,
                Now.AddSeconds(-120),
                Now.AddSeconds(-120)),
            ["missing"] = new(RolloutSourceStatus.Missing, RolloutObservation.Empty),
        };
        ThreadStatusLoader loader = CreateLoader(
            records,
            new Dictionary<string, string> { ["running"] = "Renamed running task" },
            observations);

        ThreadSnapshotLoadResult result = loader.Load();

        Assert.Equal(
            [ThreadStatus.Running, ThreadStatus.JustCompleted, ThreadStatus.Idle, ThreadStatus.Unknown],
            result.Threads.Select(thread => thread.Status));
        Assert.Equal("Renamed running task", result.Threads[0].Title);
        Assert.Equal(1_000, result.Threads[0].TokenUsage?.TotalTokens);
        Assert.Equal(50, result.Threads[^1].TokenUsage?.TotalTokens);
        Assert.Equal(RolloutSourceStatus.Missing, result.Threads[^1].RolloutSourceStatus);
        Assert.False(result.IsHealthy);
    }

    [Fact]
    public void Load_SortsEqualStatusesByLatestEventThenId()
    {
        ThreadRecord[] records =
        [
            Record("b", "B"),
            Record("a", "A"),
        ];
        var observations = new Dictionary<string, RolloutLoadResult>
        {
            ["a"] = HealthyObservation(ThreadStatus.Running, Now, Now),
            ["b"] = HealthyObservation(ThreadStatus.Running, Now, Now.AddSeconds(-1)),
        };
        ThreadStatusLoader loader = CreateLoader(records, new Dictionary<string, string>(), observations);

        ThreadSnapshotLoadResult result = loader.Load();

        Assert.Equal(["a", "b"], result.Threads.Select(thread => thread.Id));
    }

    [Fact]
    public void Load_PrefersDatabaseModelAndFillsMissingReasoningFromRollout()
    {
        ThreadRecord record = Record("task", "Task") with { Model = "db-model" };
        var observations = new Dictionary<string, RolloutLoadResult>
        {
            ["task"] = HealthyObservation(
                ThreadStatus.Running,
                Now,
                Now,
                model: "rollout-model",
                reasoningEffort: "medium"),
        };
        ThreadStatusLoader loader = CreateLoader(
            [record],
            new Dictionary<string, string>(),
            observations);

        ThreadSnapshot snapshot = Assert.Single(loader.Load().Threads);

        Assert.Equal("db-model", snapshot.Model);
        Assert.Equal("medium", snapshot.ReasoningEffort);
    }

    [Fact]
    public void Load_CountsFreshRunningSubagentsWhileParentIsCollapsed()
    {
        ThreadRecord parent = Record("parent", "Parent") with { SubagentCount = 3 };
        var repository = new ActiveCandidateThreadRepository(
            parent,
            [
                new("running-a", "parent", "running-a", Now),
                new("running-b", "parent", "running-b", Now),
                new("completed", "parent", "completed", Now),
            ]);
        var parser = new TrackingRolloutParser(new Dictionary<string, RolloutLoadResult>
        {
            ["parent"] = HealthyObservation(ThreadStatus.Idle, Now, Now),
            ["running-a"] = HealthyObservation(ThreadStatus.Running, Now, Now),
            ["running-b"] = HealthyObservation(ThreadStatus.Running, Now, Now),
            ["completed"] = HealthyObservation(ThreadStatus.JustCompleted, Now, Now),
        });
        var loader = new ThreadStatusLoader(
            repository,
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            parser,
            new FixedTimeProvider(Now));

        ThreadSnapshot snapshot = Assert.Single(loader.Load().Threads);

        Assert.Equal(2, snapshot.ActiveSubagentCount);
        Assert.Empty(snapshot.Subagents);
        Assert.Equal(Now.AddSeconds(-120), repository.RequestedCutoff);
        Assert.Equal(1, parser.ParseCounts["running-a"]);
    }

    [Fact]
    public void Load_ArchivedParentDoesNotRequestActiveCandidates()
    {
        ThreadRecord parent = Record("archived", "Archived") with
        {
            IsArchived = true,
            SubagentCount = 1,
        };
        var repository = new ActiveCandidateThreadRepository(parent, []);
        var loader = new ThreadStatusLoader(
            repository,
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
            {
                ["archived"] = HealthyObservation(ThreadStatus.Running, Now, Now),
            }),
            new FixedTimeProvider(Now));

        ThreadSnapshot snapshot = Assert.Single(loader.Load(new ThreadLoadRequest(
            8,
            new HashSet<string>(),
            new HashSet<string>(),
            new HashSet<string> { "archived" })).Threads);

        Assert.Null(repository.RequestedCutoff);
        Assert.Equal(0, snapshot.ActiveSubagentCount);
    }

    [Fact]
    public void Load_ActiveCandidateFailureDegradesWithoutDiscardingPrimaryList()
    {
        ThreadRecord parent = Record("parent", "Parent") with { SubagentCount = 1 };
        var repository = new ActiveCandidateThreadRepository(
            parent,
            [],
            ThreadRepositoryStatus.Busy);
        var loader = new ThreadStatusLoader(
            repository,
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
            {
                ["parent"] = HealthyObservation(ThreadStatus.Idle, Now, Now),
            }),
            new FixedTimeProvider(Now));

        ThreadSnapshotLoadResult result = loader.Load();

        Assert.Equal(ThreadRepositoryStatus.Healthy, result.ThreadSourceStatus);
        Assert.Single(result.Threads);
        Assert.Equal(DataSourceHealthLevel.Degraded, result.Health.TaskDatabase.Level);
    }

    [Fact]
    public void Load_MergesExplicitlyIncludedThreadsWithoutDuplicates()
    {
        ThreadRecord recent = Record("recent", "Recent");
        ThreadRecord included = Record("included", "Included");
        var repository = new IncludingThreadRepository([recent], [recent, included]);
        var observations = new Dictionary<string, RolloutLoadResult>(StringComparer.Ordinal)
        {
            ["recent"] = HealthyObservation(ThreadStatus.Running, Now, Now),
            ["included"] = HealthyObservation(ThreadStatus.Idle, Now, Now.AddSeconds(-1)),
        };
        var loader = new ThreadStatusLoader(
            repository,
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            new StubRolloutParser(observations),
            new FixedTimeProvider(Now));

        ThreadSnapshotLoadResult result = loader.Load(new ThreadLoadRequest(
            8,
            new HashSet<string>(StringComparer.Ordinal) { "included", "recent" },
            new HashSet<string>(StringComparer.Ordinal)));

        Assert.Equal(["included", "recent"], repository.RequestedIds);
        Assert.Equal(["recent", "included"], result.Threads.Select(thread => thread.Id));
        Assert.Equal(2, result.Threads.Count);
    }

    [Fact]
    public void Load_PromotesOnlyRenamedDetachedSubagentCandidates()
    {
        ThreadRecord recent = Record("recent", "Recent");
        ThreadRecord renamedDetached = Record("renamed-detached", "Database title");
        ThreadRecord unnamedDetached = Record("unnamed-detached", "Unlisted title");
        var repository = new DetachedCandidateThreadRepository(
            [recent],
            new ThreadLoadResult(
                ThreadRepositoryStatus.Healthy,
                [renamedDetached, unnamedDetached]));
        var observations = new Dictionary<string, RolloutLoadResult>(StringComparer.Ordinal)
        {
            ["recent"] = HealthyObservation(ThreadStatus.Idle, Now, Now),
            ["renamed-detached"] = HealthyObservation(ThreadStatus.Running, Now, Now),
        };
        var loader = new ThreadStatusLoader(
            repository,
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["renamed-detached"] = "User-visible detached task",
                })),
            new StubRolloutParser(observations),
            new FixedTimeProvider(Now));

        ThreadSnapshotLoadResult result = loader.Load(8);

        Assert.Equal(8, repository.RequestedDetachedLimit);
        Assert.Equal(["renamed-detached", "recent"], result.Threads.Select(thread => thread.Id));
        Assert.Equal("User-visible detached task", result.Threads[0].Title);
    }

    [Fact]
    public void Load_SkipsDetachedSubagentCandidatesWhenRenameIndexIsUnavailable()
    {
        ThreadRecord recent = Record("recent", "Recent");
        var repository = new DetachedCandidateThreadRepository(
            [recent],
            new ThreadLoadResult(
                ThreadRepositoryStatus.Healthy,
                [Record("detached", "Detached")]));
        var loader = new ThreadStatusLoader(
            repository,
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Missing,
                new Dictionary<string, string>())),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
            {
                ["recent"] = HealthyObservation(ThreadStatus.Idle, Now, Now),
            }),
            new FixedTimeProvider(Now));

        ThreadSnapshotLoadResult result = loader.Load(8);

        Assert.Null(repository.RequestedDetachedLimit);
        Assert.Equal("recent", Assert.Single(result.Threads).Id);
    }

    [Fact]
    public void Load_ReportsDetachedCandidateQueryFailureAsDegraded()
    {
        ThreadRecord recent = Record("recent", "Recent");
        var repository = new DetachedCandidateThreadRepository(
            [recent],
            new ThreadLoadResult(ThreadRepositoryStatus.Busy, []));
        var loader = new ThreadStatusLoader(
            repository,
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>
                {
                    ["detached"] = "Detached",
                })),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
            {
                ["recent"] = HealthyObservation(ThreadStatus.Idle, Now, Now),
            }),
            new FixedTimeProvider(Now));

        ThreadSnapshotLoadResult result = loader.Load(8);

        Assert.Equal(OverallDataSourceHealth.Degraded, result.Health.OverallStatus);
        Assert.Equal(DataSourceHealthLevel.Degraded, result.Health.TaskDatabase.Level);
        Assert.Equal("recent", Assert.Single(result.Threads).Id);
    }

    [Fact]
    public void Load_PropagatesDataSourceHealthWithoutDroppingThreads()
    {
        var threadRepository = new StubThreadRepository(new ThreadLoadResult(
            ThreadRepositoryStatus.Healthy,
            [Record("thread", "SQLite title")]));
        var titleRepository = new StubTitleRepository(new TitleLoadResult(
            SessionIndexStatus.Missing,
            new Dictionary<string, string>()));
        var rolloutParser = new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
        {
            ["thread"] = HealthyObservation(ThreadStatus.Running, Now, Now),
        });
        var loader = new ThreadStatusLoader(
            threadRepository,
            titleRepository,
            rolloutParser,
            new FixedTimeProvider(Now));

        ThreadSnapshotLoadResult result = loader.Load();

        ThreadSnapshot snapshot = Assert.Single(result.Threads);
        Assert.Equal("SQLite title", snapshot.Title);
        Assert.Equal(SessionIndexStatus.Missing, result.TitleSourceStatus);
        Assert.False(result.IsHealthy);
    }

    [Fact]
    public void Load_OnlyLoadsSubagentsForExpandedVisibleParents()
    {
        ThreadRecord parent = new("parent", "Parent", "parent", Now, 0, 2);
        SubagentRecord[] children =
        [
            new("idle-child", "parent", "Idle child", "idle-child", Now.AddMinutes(-3), 20, null, "explorer", null, null),
            new("running-child", "parent", "Running child", "running-child", Now.AddMinutes(-2), 10, "worker", "reviewer", "gpt-test", "high", "/root/audit_running_task"),
        ];
        var repository = new TrackingThreadRepository(
            new ThreadLoadResult(ThreadRepositoryStatus.Healthy, [parent]),
            new SubagentLoadResult(
                ThreadRepositoryStatus.Healthy,
                new Dictionary<string, IReadOnlyList<SubagentRecord>>(StringComparer.Ordinal)
                {
                    ["parent"] = children,
                }));
        var titles = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["idle-child"] = "Renamed idle child",
        };
        var observations = new Dictionary<string, RolloutLoadResult>(StringComparer.Ordinal)
        {
            ["parent"] = HealthyObservation(ThreadStatus.Idle, Now, Now),
            ["running-child"] = HealthyObservation(ThreadStatus.Running, Now.AddSeconds(-10), Now.AddSeconds(-5)),
            ["idle-child"] = HealthyObservation(ThreadStatus.JustCompleted, Now.AddMinutes(-2), Now.AddMinutes(-2)),
        };
        var loader = new ThreadStatusLoader(
            repository,
            new StubTitleRepository(new TitleLoadResult(SessionIndexStatus.Healthy, titles)),
            new StubRolloutParser(observations),
            new FixedTimeProvider(Now));

        ThreadSnapshotLoadResult result = loader.Load(
            expandedThreadIds: new HashSet<string>(StringComparer.Ordinal)
            {
                "parent",
                "not-visible",
            });

        Assert.Equal(["parent"], repository.RequestedParentIds);
        ThreadSnapshot snapshot = Assert.Single(result.Threads);
        Assert.Equal(["running-child", "idle-child"], snapshot.Subagents.Select(child => child.Id));
        Assert.Equal("Renamed idle child", snapshot.Subagents[1].Title);
        Assert.Equal(ThreadStatus.Idle, snapshot.Subagents[1].Status);
        Assert.Equal(20, snapshot.Subagents[1].TokenUsage?.TotalTokens);
        Assert.Equal("reviewer", snapshot.Subagents[0].AgentRole);
        Assert.Equal("gpt-test", snapshot.Subagents[0].Model);
        Assert.Equal("high", snapshot.Subagents[0].ReasoningEffort);
        Assert.Equal("/root/audit_running_task", snapshot.Subagents[0].AgentPath);
    }

    [Fact]
    public void Load_RequestRetentionAppliesToPrimaryAndExpandedSubagent()
    {
        ThreadRecord parent = new("parent", "Parent", "parent", Now, 0, 1);
        var child = new SubagentRecord(
            "child",
            "parent",
            "Child",
            "child",
            Now.AddMinutes(-2),
            0,
            null,
            null,
            null,
            null);
        var repository = new TrackingThreadRepository(
            new ThreadLoadResult(ThreadRepositoryStatus.Healthy, [parent]),
            new SubagentLoadResult(
                ThreadRepositoryStatus.Healthy,
                new Dictionary<string, IReadOnlyList<SubagentRecord>>(StringComparer.Ordinal)
                {
                    ["parent"] = [child],
                }));
        DateTimeOffset completedAt = Now.AddMinutes(-2);
        var observations = new Dictionary<string, RolloutLoadResult>(StringComparer.Ordinal)
        {
            ["parent"] = HealthyObservation(
                ThreadStatus.JustCompleted,
                completedAt,
                completedAt,
                completionEventAt: completedAt),
            ["child"] = HealthyObservation(
                ThreadStatus.JustCompleted,
                completedAt,
                completedAt,
                completionEventAt: completedAt),
        };
        var loader = new ThreadStatusLoader(
            repository,
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            new StubRolloutParser(observations),
            new FixedTimeProvider(Now));

        ThreadSnapshot snapshot = Assert.Single(loader.Load(new ThreadLoadRequest(
            8,
            new HashSet<string>(),
            new HashSet<string> { "parent" },
            CompletedRetention: TimeSpan.FromMinutes(3))).Threads);

        Assert.Equal(ThreadStatus.JustCompleted, snapshot.Status);
        Assert.Equal(ThreadStatus.JustCompleted, Assert.Single(snapshot.Subagents).Status);
    }

    [Fact]
    public void Load_SkipsSubagentSourceWhenNothingIsExpanded()
    {
        var repository = new TrackingThreadRepository(
            new ThreadLoadResult(ThreadRepositoryStatus.Healthy, [Record("parent", "Parent")]),
            new SubagentLoadResult(
                ThreadRepositoryStatus.Healthy,
                new Dictionary<string, IReadOnlyList<SubagentRecord>>(StringComparer.Ordinal)));
        var loader = new ThreadStatusLoader(
            repository,
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
            {
                ["parent"] = HealthyObservation(ThreadStatus.Idle, Now, Now),
            }),
            new FixedTimeProvider(Now));

        ThreadSnapshotLoadResult result = loader.Load();

        Assert.Null(repository.RequestedParentIds);
        Assert.Empty(Assert.Single(result.Threads).Subagents);
    }

    [Fact]
    public void Load_QueriesVisibleThreadsAndOverlaysRetryIncident()
    {
        ThreadRecord[] records = [Record("visible", "Visible")];
        var incidents = new TrackingLogEventRepository(new Dictionary<string, ServiceIncident>
        {
            ["visible"] = Incident(
                "turn-retry",
                ServiceIncidentPhase.Retrying,
                Now.AddSeconds(-4),
                429,
                2,
                5),
        });
        var loader = new ThreadStatusLoader(
            new StubThreadRepository(new ThreadLoadResult(ThreadRepositoryStatus.Healthy, records)),
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
            {
                ["visible"] = HealthyObservation(
                    ThreadStatus.JustCompleted,
                    Now.AddSeconds(-5),
                    Now.AddSeconds(-5),
                    completionEventAt: Now.AddSeconds(-5)),
            }),
            new FixedTimeProvider(Now),
            logEventRepository: incidents);

        ThreadSnapshot snapshot = Assert.Single(loader.Load().Threads);

        Assert.Equal(["visible"], incidents.RequestedThreadIds);
        Assert.Equal(ThreadStatus.Warning, snapshot.Status);
        Assert.Equal(Now.AddSeconds(-4), snapshot.StatusChangedAt);
        Assert.Null(snapshot.CompletionEventAt);
        Assert.Equal("turn-retry", snapshot.ServiceIncident?.EpisodeId);
    }

    [Fact]
    public void Load_OverlaysFinalFailureAsError()
    {
        var loader = CreateLoaderWithIncident(
            Incident("turn-failed", ServiceIncidentPhase.Failed, Now.AddSeconds(-3), 503),
            HealthyObservation(ThreadStatus.Running, Now.AddSeconds(-1), Now.AddSeconds(-1)));

        ThreadSnapshot snapshot = Assert.Single(loader.Load().Threads);

        Assert.Equal(ThreadStatus.Error, snapshot.Status);
        Assert.Equal(ServiceIncidentPhase.Failed, snapshot.ServiceIncident?.Phase);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Load_ClearsOldRetryWhenRolloutHasNewerLifecycleEvent(bool useTaskStarted)
    {
        DateTimeOffset lifecycleAt = Now.AddSeconds(-2);
        RolloutLoadResult rollout = HealthyObservation(
            ThreadStatus.Running,
            lifecycleAt,
            lifecycleAt,
            completionEventAt: useTaskStarted ? null : lifecycleAt,
            latestTaskStartedAt: useTaskStarted ? lifecycleAt : null);
        var loader = CreateLoaderWithIncident(
            Incident("old-turn", ServiceIncidentPhase.Retrying, Now.AddSeconds(-5), 503),
            rollout);

        ThreadSnapshot snapshot = Assert.Single(loader.Load().Threads);

        Assert.Equal(ThreadStatus.Running, snapshot.Status);
        Assert.Null(snapshot.ServiceIncident);
    }

    [Fact]
    public void Load_ResolvedIncidentDoesNotSuppressNewerCompaction()
    {
        using var fixture = new CompactionActivityFixture();
        DateTimeOffset startedAt = Now.AddSeconds(-10);
        fixture.Repository.WritePreCompact(new CompactionActivity("thread", "turn", "auto", startedAt));
        var loader = new ThreadStatusLoader(
            new StubThreadRepository(new ThreadLoadResult(
                ThreadRepositoryStatus.Healthy,
                [Record("thread", "Thread")])),
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
            {
                ["thread"] = HealthyObservation(
                    ThreadStatus.Running,
                    Now.AddSeconds(-20),
                    Now.AddSeconds(-20),
                    latestTaskStartedAt: Now.AddSeconds(-20)),
            }),
            new FixedTimeProvider(Now),
            logEventRepository: new TrackingLogEventRepository(
                new Dictionary<string, ServiceIncident>
                {
                    ["thread"] = Incident(
                        "old-incident",
                        ServiceIncidentPhase.Failed,
                        Now.AddSeconds(-30),
                        503),
                }),
            compactionActivityRepository: fixture.Repository);

        ThreadSnapshot snapshot = Assert.Single(loader.Load().Threads);

        Assert.Null(snapshot.ServiceIncident);
        Assert.NotNull(snapshot.CompactionActivity);
        Assert.Equal(ThreadStatus.Running, snapshot.Status);
        Assert.Equal(startedAt, snapshot.StatusChangedAt);
    }

    [Fact]
    public void Load_LogRepositoryFailureDoesNotDropMainThreads()
    {
        var loader = new ThreadStatusLoader(
            new StubThreadRepository(new ThreadLoadResult(
                ThreadRepositoryStatus.Healthy,
                [Record("thread", "Thread")])),
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
            {
                ["thread"] = HealthyObservation(ThreadStatus.Idle, Now, Now),
            }),
            new FixedTimeProvider(Now),
            logEventRepository: new ThrowingLogEventRepository());

        ThreadSnapshot snapshot = Assert.Single(loader.Load().Threads);

        Assert.Equal(ThreadStatus.Idle, snapshot.Status);
        Assert.Null(snapshot.ServiceIncident);
    }

    [Fact]
    public void Load_ArchivedFavoriteRetainsContentButClearsLifecycleAndIncidentState()
    {
        ThreadRecord active = Record("active", "Active");
        ThreadRecord archived = new(
            "archived",
            "Archived database title",
            "archived",
            Now.AddMinutes(-5),
            900,
            3,
            IsArchived: true);
        var repository = new FavoriteThreadRepository([active], [archived]);
        var incidents = new TrackingLogEventRepository(new Dictionary<string, ServiceIncident>
        {
            ["active"] = Incident("active-turn", ServiceIncidentPhase.Retrying, Now.AddSeconds(-3), 429),
            ["archived"] = Incident("archived-turn", ServiceIncidentPhase.Failed, Now.AddSeconds(-2), 503),
        });
        var loader = new ThreadStatusLoader(
            repository,
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string> { ["archived"] = "Renamed archived task" })),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
            {
                ["active"] = HealthyObservation(ThreadStatus.Running, Now, Now),
                ["archived"] = HealthyObservation(
                    ThreadStatus.Running,
                    Now.AddSeconds(-1),
                    Now.AddSeconds(-1),
                    new TokenUsageSnapshot(1_234, null, null, Now.AddSeconds(-1)),
                    completionEventAt: Now.AddSeconds(-1),
                    latestTaskStartedAt: Now.AddSeconds(-2)),
            }),
            new FixedTimeProvider(Now),
            logEventRepository: incidents);

        ThreadSnapshotLoadResult result = loader.Load(new ThreadLoadRequest(
            8,
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal) { "archived" }));

        Assert.Equal(["archived"], repository.RequestedFavoriteIds);
        Assert.Equal(["active"], incidents.RequestedThreadIds);
        ThreadSnapshot snapshot = Assert.Single(result.Threads, item => item.Id == "archived");
        Assert.True(snapshot.IsArchived);
        Assert.Equal("Renamed archived task", snapshot.Title);
        Assert.Equal(ThreadStatus.Idle, snapshot.Status);
        Assert.Equal(archived.UpdatedAt, snapshot.StatusChangedAt);
        Assert.Null(snapshot.LatestTaskStartedAt);
        Assert.Null(snapshot.CompletionEventAt);
        Assert.Null(snapshot.ServiceIncident);
        Assert.Equal(1_234, snapshot.TokenUsage?.TotalTokens);
        Assert.Equal(0, snapshot.SubagentCount);
        Assert.Empty(snapshot.Subagents);
    }

    [Fact]
    public void Load_ReportsOptionalFailuresAndAccurateRolloutCounts()
    {
        ThreadRecord[] records =
        [
            Record("healthy", "Healthy"),
            Record("missing", "Missing"),
        ];
        var loader = new ThreadStatusLoader(
            new StubThreadRepository(new ThreadLoadResult(
                ThreadRepositoryStatus.Healthy,
                records)),
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Missing,
                new Dictionary<string, string>())),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
            {
                ["healthy"] = HealthyObservation(ThreadStatus.Running, Now, Now),
                ["missing"] = new(RolloutSourceStatus.Missing, RolloutObservation.Empty),
            }),
            new FixedTimeProvider(Now),
            logEventRepository: new StatusLogEventRepository(ServiceLogSourceStatus.Missing));

        ThreadSnapshotLoadResult result = loader.Load();

        Assert.Equal(OverallDataSourceHealth.Degraded, result.Health.OverallStatus);
        Assert.Equal(DataSourceHealthLevel.Unavailable, result.Health.RenameIndex.Level);
        Assert.Equal(DataSourceHealthLevel.Degraded, result.Health.Rollout.Level);
        Assert.Equal(1, result.Health.RolloutSuccessCount);
        Assert.Equal(1, result.Health.RolloutFailureCount);
        Assert.Equal(DataSourceHealthLevel.Unavailable, result.Health.ServiceLogs.Level);
        Assert.Equal(2, result.Threads.Count);
    }

    [Fact]
    public void Load_ReportsCoreTaskDatabaseUnavailable()
    {
        var loader = new ThreadStatusLoader(
            new StubThreadRepository(new ThreadLoadResult(
                ThreadRepositoryStatus.Missing,
                [])),
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>()),
            new FixedTimeProvider(Now));

        ThreadSnapshotLoadResult result = loader.Load();

        Assert.Equal(OverallDataSourceHealth.Unavailable, result.Health.OverallStatus);
        Assert.Equal(DataSourceHealthLevel.Unavailable, result.Health.TaskDatabase.Level);
        Assert.Equal("未找到 Codex 任务数据库", result.Health.TaskDatabase.DetailText);
    }

    [Fact]
    public void Load_ReportsSupplementalTaskQueryFailureAsDegraded()
    {
        ThreadRecord recent = Record("recent", "Recent");
        var loader = new ThreadStatusLoader(
            new StatusThreadRepository(
                new ThreadLoadResult(ThreadRepositoryStatus.Healthy, [recent]),
                new ThreadLoadResult(ThreadRepositoryStatus.Busy, [])),
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
            {
                ["recent"] = HealthyObservation(ThreadStatus.Running, Now, Now),
            }),
            new FixedTimeProvider(Now));

        ThreadSnapshotLoadResult result = loader.Load(new ThreadLoadRequest(
            8,
            new HashSet<string>(StringComparer.Ordinal) { "included" },
            new HashSet<string>(StringComparer.Ordinal)));

        Assert.Equal(OverallDataSourceHealth.Degraded, result.Health.OverallStatus);
        Assert.Equal(DataSourceHealthLevel.Degraded, result.Health.TaskDatabase.Level);
        Assert.Equal("recent", Assert.Single(result.Threads).Id);
    }

    [Fact]
    public void Load_ReportsUnusedOptionalSourcesWithoutCallingThem()
    {
        var serviceLogs = new StatusLogEventRepository(ServiceLogSourceStatus.Healthy);
        var loader = new ThreadStatusLoader(
            new StubThreadRepository(new ThreadLoadResult(
                ThreadRepositoryStatus.Healthy,
                [])),
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>()),
            new FixedTimeProvider(Now),
            logEventRepository: serviceLogs);

        ThreadSnapshotLoadResult result = loader.Load();

        Assert.Equal(DataSourceHealthLevel.NotUsed, result.Health.Rollout.Level);
        Assert.Equal(DataSourceHealthLevel.NotUsed, result.Health.ServiceLogs.Level);
        Assert.Equal(0, serviceLogs.LoadCount);
    }

    [Fact]
    public void Load_CountsExpandedSubagentRolloutReads()
    {
        ThreadRecord parent = new("parent", "Parent", "parent", Now, 0, 1);
        SubagentRecord child = new(
            "child", "parent", "Child", "child", Now, 0, null, null, null, null);
        var repository = new TrackingThreadRepository(
            new ThreadLoadResult(ThreadRepositoryStatus.Healthy, [parent]),
            new SubagentLoadResult(
                ThreadRepositoryStatus.Healthy,
                new Dictionary<string, IReadOnlyList<SubagentRecord>>(StringComparer.Ordinal)
                {
                    ["parent"] = [child],
                }));
        var loader = new ThreadStatusLoader(
            repository,
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
            {
                ["parent"] = HealthyObservation(ThreadStatus.Running, Now, Now),
                ["child"] = new(RolloutSourceStatus.Missing, RolloutObservation.Empty),
            }),
            new FixedTimeProvider(Now));

        ThreadSnapshotLoadResult result = loader.Load(
            expandedThreadIds: new HashSet<string>(StringComparer.Ordinal) { "parent" });

        Assert.Equal(1, result.Health.RolloutSuccessCount);
        Assert.Equal(1, result.Health.RolloutFailureCount);
        Assert.Equal(DataSourceHealthLevel.Degraded, result.Health.Rollout.Level);
    }

    [Fact]
    public void Load_ActiveCompactionPromotesTaskToRunning()
    {
        using var fixture = new CompactionActivityFixture();
        DateTimeOffset startedAt = Now.AddSeconds(-15);
        fixture.Repository.WritePreCompact(new CompactionActivity("thread", "turn", "auto", startedAt));
        var loader = new ThreadStatusLoader(
            new StubThreadRepository(new ThreadLoadResult(
                ThreadRepositoryStatus.Healthy,
                [Record("thread", "Thread")])),
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
            {
                ["thread"] = HealthyObservation(ThreadStatus.Idle, Now.AddMinutes(-2), Now.AddMinutes(-2)),
            }),
            new FixedTimeProvider(Now),
            compactionActivityRepository: fixture.Repository);

        ThreadSnapshot snapshot = Assert.Single(loader.Load().Threads);

        Assert.Equal(ThreadStatus.Running, snapshot.Status);
        Assert.Equal(startedAt, snapshot.StatusChangedAt);
        Assert.NotNull(snapshot.CompactionActivity);
        Assert.Null(snapshot.CompletionEventAt);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Load_CompletionOrInterruptionEvidenceClearsCompaction(
        bool hasCompletion,
        bool hasInterruption)
    {
        using var fixture = new CompactionActivityFixture();
        fixture.Repository.WritePreCompact(new CompactionActivity(
            "thread",
            "turn",
            "manual",
            Now.AddSeconds(-15)));
        var observation = new RolloutObservation(
            ThreadStatus.JustCompleted,
            Now.AddSeconds(-5),
            Now.AddSeconds(-5),
            hasCompletion ? Now.AddSeconds(-5) : null,
            Now.AddMinutes(-1),
            null,
            InterruptionEventAt: hasInterruption ? Now.AddSeconds(-5) : null);
        var loader = new ThreadStatusLoader(
            new StubThreadRepository(new ThreadLoadResult(
                ThreadRepositoryStatus.Healthy,
                [Record("thread", "Thread")])),
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
            {
                ["thread"] = new(RolloutSourceStatus.Healthy, observation),
            }),
            new FixedTimeProvider(Now),
            compactionActivityRepository: fixture.Repository);

        ThreadSnapshot snapshot = Assert.Single(loader.Load().Threads);

        Assert.Null(snapshot.CompactionActivity);
        Assert.NotEqual(ThreadStatus.Running, snapshot.Status);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Load_ArchivedOrIncidentTaskSuppressesCompaction(bool archived, bool incident)
    {
        using var fixture = new CompactionActivityFixture();
        fixture.Repository.WritePreCompact(new CompactionActivity(
            "thread",
            "turn",
            "auto",
            Now.AddSeconds(-15)));
        ThreadRecord record = Record("thread", "Thread") with { IsArchived = archived };
        var loader = new ThreadStatusLoader(
            new StubThreadRepository(new ThreadLoadResult(ThreadRepositoryStatus.Healthy, [record])),
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
            {
                ["thread"] = HealthyObservation(ThreadStatus.Idle, Now.AddMinutes(-2), Now.AddMinutes(-2)),
            }),
            new FixedTimeProvider(Now),
            logEventRepository: incident
                ? new TrackingLogEventRepository(new Dictionary<string, ServiceIncident>
                {
                    ["thread"] = Incident("episode", ServiceIncidentPhase.Failed, Now.AddSeconds(-5), 503),
                })
                : null,
            compactionActivityRepository: fixture.Repository);

        ThreadSnapshot snapshot = Assert.Single(loader.Load(new ThreadLoadRequest(
            8,
            new HashSet<string>(),
            new HashSet<string>(),
            archived ? new HashSet<string> { "thread" } : null)).Threads);

        Assert.Null(snapshot.CompactionActivity);
        Assert.Equal(archived ? ThreadStatus.Idle : ThreadStatus.Error, snapshot.Status);
    }

    private static ThreadStatusLoader CreateLoader(
        IReadOnlyList<ThreadRecord> records,
        IReadOnlyDictionary<string, string> titles,
        IReadOnlyDictionary<string, RolloutLoadResult> observations) =>
        new(
            new StubThreadRepository(new ThreadLoadResult(ThreadRepositoryStatus.Healthy, records)),
            new StubTitleRepository(new TitleLoadResult(SessionIndexStatus.Healthy, titles)),
            new StubRolloutParser(observations),
            new FixedTimeProvider(Now));

    private static ThreadStatusLoader CreateLoaderWithIncident(
        ServiceIncident incident,
        RolloutLoadResult rollout) =>
        new(
            new StubThreadRepository(new ThreadLoadResult(
                ThreadRepositoryStatus.Healthy,
                [Record("thread", "Thread")])),
            new StubTitleRepository(new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new Dictionary<string, string>())),
            new StubRolloutParser(new Dictionary<string, RolloutLoadResult>
            {
                ["thread"] = rollout,
            }),
            new FixedTimeProvider(Now),
            logEventRepository: new TrackingLogEventRepository(
                new Dictionary<string, ServiceIncident> { ["thread"] = incident }));

    private static ThreadRecord Record(
        string id,
        string title,
        long tokens = 0) =>
        new(id, title, id, Now.AddMinutes(-5), tokens, 0);

    private static RolloutLoadResult HealthyObservation(
        ThreadStatus status,
        DateTimeOffset changedAt,
        DateTimeOffset latestEventAt,
        TokenUsageSnapshot? tokenUsage = null,
        DateTimeOffset? completionEventAt = null,
        DateTimeOffset? latestTaskStartedAt = null,
        string? model = null,
        string? reasoningEffort = null) =>
        new(
            RolloutSourceStatus.Healthy,
            new RolloutObservation(
                status,
                changedAt,
                latestEventAt,
                completionEventAt,
                latestTaskStartedAt,
                tokenUsage,
                model,
                reasoningEffort));

    private static ServiceIncident Incident(
        string episodeId,
        ServiceIncidentPhase phase,
        DateTimeOffset occurredAt,
        int? statusCode = null,
        int? retryAttempt = null,
        int? retryLimit = null) =>
        new(episodeId, phase, statusCode, retryAttempt, retryLimit, occurredAt);

    private sealed class StubThreadRepository(ThreadLoadResult result) : IThreadRepository
    {
        public ThreadLoadResult LoadRecent(int limit = 8) => result;
    }

    private sealed class CompactionActivityFixture : IDisposable
    {
        private readonly string directory = Path.Combine(
            Path.GetTempPath(),
            "ThreadBeaconCompactionLoader",
            Guid.NewGuid().ToString("N"));

        public CompactionActivityRepository Repository => new(directory);

        public void Dispose()
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private sealed class ActiveCandidateThreadRepository(
        ThreadRecord parent,
        IReadOnlyList<SubagentActivityCandidate> candidates,
        ThreadRepositoryStatus activityStatus = ThreadRepositoryStatus.Healthy) : IThreadRepository
    {
        public DateTimeOffset? RequestedCutoff { get; private set; }

        public ThreadLoadResult LoadRecent(int limit = 8) =>
            new(ThreadRepositoryStatus.Healthy, parent.IsArchived ? [] : [parent]);

        public ThreadLoadResult LoadByIdsIncludingArchived(IReadOnlySet<string> threadIds) =>
            new(ThreadRepositoryStatus.Healthy, threadIds.Contains(parent.Id) ? [parent] : []);

        public SubagentActivityLoadResult LoadRecentSubagentCandidates(
            IReadOnlySet<string> parentIds,
            DateTimeOffset updatedAfter)
        {
            RequestedCutoff = updatedAfter;
            return new(
                activityStatus,
                new Dictionary<string, IReadOnlyList<SubagentActivityCandidate>>
                {
                    [parent.Id] = candidates,
                });
        }
    }

    private sealed class TrackingRolloutParser(
        IReadOnlyDictionary<string, RolloutLoadResult> observations) : IRolloutTailParser
    {
        public Dictionary<string, int> ParseCounts { get; } = new(StringComparer.Ordinal);

        public RolloutLoadResult Parse(string filePath)
        {
            ParseCounts[filePath] = ParseCounts.GetValueOrDefault(filePath) + 1;
            return observations[filePath];
        }
    }

    private sealed class TrackingThreadRepository(
        ThreadLoadResult threadResult,
        SubagentLoadResult subagentResult) : IThreadRepository
    {
        public IReadOnlySet<string>? RequestedParentIds { get; private set; }

        public ThreadLoadResult LoadRecent(int limit = 8) => threadResult;

        public SubagentLoadResult LoadDirectSubagents(IReadOnlySet<string> parentIds)
        {
            RequestedParentIds = new HashSet<string>(parentIds, StringComparer.Ordinal);
            return subagentResult;
        }
    }

    private sealed class DetachedCandidateThreadRepository(
        IReadOnlyList<ThreadRecord> recent,
        ThreadLoadResult detachedResult) : IThreadRepository
    {
        public int? RequestedDetachedLimit { get; private set; }

        public ThreadLoadResult LoadRecent(int limit = 8) =>
            new(ThreadRepositoryStatus.Healthy, recent);

        public ThreadLoadResult LoadDetachedSubagentCandidates(int limit)
        {
            RequestedDetachedLimit = limit;
            return detachedResult;
        }
    }

    private sealed class IncludingThreadRepository(
        IReadOnlyList<ThreadRecord> recent,
        IReadOnlyList<ThreadRecord> included) : IThreadRepository
    {
        public IReadOnlySet<string>? RequestedIds { get; private set; }

        public ThreadLoadResult LoadRecent(int limit = 8) =>
            new(ThreadRepositoryStatus.Healthy, recent);

        public ThreadLoadResult LoadByIds(IReadOnlySet<string> threadIds)
        {
            RequestedIds = new HashSet<string>(threadIds, StringComparer.Ordinal);
            return new ThreadLoadResult(ThreadRepositoryStatus.Healthy, included);
        }
    }

    private sealed class StatusThreadRepository(
        ThreadLoadResult recent,
        ThreadLoadResult included) : IThreadRepository
    {
        public ThreadLoadResult LoadRecent(int limit = 8) => recent;

        public ThreadLoadResult LoadByIds(IReadOnlySet<string> threadIds) => included;
    }

    private sealed class FavoriteThreadRepository(
        IReadOnlyList<ThreadRecord> recent,
        IReadOnlyList<ThreadRecord> favorites) : IThreadRepository
    {
        public IReadOnlySet<string>? RequestedFavoriteIds { get; private set; }

        public ThreadLoadResult LoadRecent(int limit = 8) =>
            new(ThreadRepositoryStatus.Healthy, recent);

        public ThreadLoadResult LoadByIdsIncludingArchived(IReadOnlySet<string> threadIds)
        {
            RequestedFavoriteIds = new HashSet<string>(threadIds, StringComparer.Ordinal);
            return new ThreadLoadResult(ThreadRepositoryStatus.Healthy, favorites);
        }
    }

    private sealed class StubTitleRepository(TitleLoadResult result) : ISessionIndexTitleRepository
    {
        public TitleLoadResult LoadLatestTitles() => result;
    }

    private sealed class StubRolloutParser(
        IReadOnlyDictionary<string, RolloutLoadResult> observations) : IRolloutTailParser
    {
        public RolloutLoadResult Parse(string filePath) => observations[filePath];
    }

    private sealed class TrackingLogEventRepository(
        IReadOnlyDictionary<string, ServiceIncident> incidents) : ILogEventRepository
    {
        public IReadOnlySet<string>? RequestedThreadIds { get; private set; }

        public ServiceLogLoadResult LoadLatestIncidents(
            IReadOnlySet<string> threadIds)
        {
            RequestedThreadIds = new HashSet<string>(threadIds, StringComparer.Ordinal);
            return new ServiceLogLoadResult(ServiceLogSourceStatus.Healthy, incidents);
        }
    }

    private sealed class ThrowingLogEventRepository : ILogEventRepository
    {
        public ServiceLogLoadResult LoadLatestIncidents(
            IReadOnlySet<string> threadIds) =>
            throw new IOException("Synthetic log read failure.");
    }

    private sealed class StatusLogEventRepository(ServiceLogSourceStatus status)
        : ILogEventRepository
    {
        public int LoadCount { get; private set; }

        public ServiceLogLoadResult LoadLatestIncidents(IReadOnlySet<string> threadIds)
        {
            LoadCount++;
            return new ServiceLogLoadResult(
                status,
                new Dictionary<string, ServiceIncident>(StringComparer.Ordinal));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
