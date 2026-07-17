using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public interface ISessionIndexTitleRepository
{
    TitleLoadResult LoadLatestTitles();
}
