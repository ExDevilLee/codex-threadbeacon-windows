namespace ThreadBeacon.App.Theme;

public sealed class AppThemeState : IDisposable
{
    private readonly IAppThemeDetector detector;
    private readonly Action<AppTheme>? persist;
    private AppTheme preference;
    private AppTheme effectiveTheme;
    private bool isDisposed;

    public AppThemeState(
        AppTheme preference,
        IAppThemeDetector detector,
        Action<AppTheme>? persist = null)
    {
        this.detector = detector ?? throw new ArgumentNullException(nameof(detector));
        this.persist = persist;
        this.preference = preference;
        effectiveTheme = ResolveEffectiveTheme();
        detector.Changed += OnDetectorChanged;
    }

    public event EventHandler? Changed;

    public AppTheme Preference => preference;

    public AppTheme EffectiveTheme => effectiveTheme;

    public void SetPreference(AppTheme value)
    {
        if (preference == value)
        {
            return;
        }

        preference = value;
        persist?.Invoke(value);
        RefreshEffectiveTheme();
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        detector.Changed -= OnDetectorChanged;
        detector.Dispose();
    }

    private AppTheme ResolveEffectiveTheme()
    {
        if (preference is AppTheme.Light or AppTheme.Dark)
        {
            return preference;
        }

        AppTheme detected = detector.ReadCurrentTheme();
        return detected is AppTheme.Dark ? AppTheme.Dark : AppTheme.Light;
    }

    private void OnDetectorChanged(object? sender, EventArgs e)
    {
        if (preference is AppTheme.System)
        {
            RefreshEffectiveTheme();
        }
    }

    private void RefreshEffectiveTheme()
    {
        AppTheme next = ResolveEffectiveTheme();
        if (effectiveTheme == next)
        {
            return;
        }

        effectiveTheme = next;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
