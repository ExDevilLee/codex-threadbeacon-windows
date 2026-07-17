using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public interface IRolloutTailParser
{
    RolloutLoadResult Parse(string filePath);
}
