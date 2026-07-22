namespace ThreadBeacon.App.AutoRecovery;

public interface IRecoveryForegroundSession
{
    void RestoreIfSafe();
}

public interface IRecoveryForegroundSessionFactory
{
    IRecoveryForegroundSession Capture();
}

internal sealed class NoOpRecoveryForegroundSessionFactory : IRecoveryForegroundSessionFactory
{
    public static NoOpRecoveryForegroundSessionFactory Instance { get; } = new();

    private NoOpRecoveryForegroundSessionFactory()
    {
    }

    public IRecoveryForegroundSession Capture() => NoOpRecoveryForegroundSession.Instance;
}

internal sealed class NoOpRecoveryForegroundSession : IRecoveryForegroundSession
{
    public static NoOpRecoveryForegroundSession Instance { get; } = new();

    private NoOpRecoveryForegroundSession()
    {
    }

    public void RestoreIfSafe()
    {
    }
}
