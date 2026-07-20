namespace ThreadBeacon.Core.Models;

public readonly record struct AvailableUpdate(
    SemanticVersion Version,
    Uri ReleaseUrl,
    bool IsPrerelease);
