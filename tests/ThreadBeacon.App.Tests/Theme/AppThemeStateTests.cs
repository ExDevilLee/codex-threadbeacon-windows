using ThreadBeacon.App.Theme;

namespace ThreadBeacon.App.Tests.Theme;

public sealed class AppThemeStateTests
{
    [Fact]
    public void SystemPreference_UsesDetectorAndRaisesOnlyOnEffectiveChange()
    {
        var detector = new FakeThemeDetector(AppTheme.Dark);
        var state = new AppThemeState(AppTheme.System, detector);
        int changes = 0;
        state.Changed += (_, _) => changes++;

        detector.SetTheme(AppTheme.Light);
        detector.SetTheme(AppTheme.Light);

        Assert.Equal(AppTheme.System, state.Preference);
        Assert.Equal(AppTheme.Light, state.EffectiveTheme);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void ExplicitTheme_IgnoresDetectorChanges()
    {
        var detector = new FakeThemeDetector(AppTheme.Light);
        var state = new AppThemeState(AppTheme.Light, detector);
        int changes = 0;
        state.Changed += (_, _) => changes++;

        detector.SetTheme(AppTheme.Dark);

        Assert.Equal(AppTheme.Light, state.EffectiveTheme);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void SetPreference_PersistsAndRaisesWhenEffectiveThemeChanges()
    {
        var detector = new FakeThemeDetector(AppTheme.Light);
        AppTheme? persisted = null;
        var state = new AppThemeState(
            AppTheme.System,
            detector,
            value => persisted = value);
        int changes = 0;
        state.Changed += (_, _) => changes++;

        state.SetPreference(AppTheme.Dark);

        Assert.Equal(AppTheme.Dark, state.Preference);
        Assert.Equal(AppTheme.Dark, state.EffectiveTheme);
        Assert.Equal(AppTheme.Dark, persisted);
        Assert.Equal(1, changes);
    }

    private sealed class FakeThemeDetector(AppTheme initial) : IAppThemeDetector
    {
        public event EventHandler? Changed;

        public AppTheme CurrentTheme { get; private set; } = initial;

        public void SetTheme(AppTheme value)
        {
            if (CurrentTheme == value)
            {
                return;
            }

            CurrentTheme = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public AppTheme ReadCurrentTheme() => CurrentTheme;

        public void Dispose()
        {
        }
    }
}
