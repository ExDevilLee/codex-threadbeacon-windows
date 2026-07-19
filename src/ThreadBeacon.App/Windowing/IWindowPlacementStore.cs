namespace ThreadBeacon.App.Windowing;

public interface IWindowPlacementStore
{
    WindowPlacement? Load();

    bool Save(WindowPlacement placement);
}
