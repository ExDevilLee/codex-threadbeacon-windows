using System.Windows.Input;

namespace ThreadBeacon.App.Commands;

public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Action execute = execute;
    private readonly Func<bool>? canExecute = canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add => canExecuteChanged += value;
        remove => canExecuteChanged -= value;
    }

    private event EventHandler? canExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            execute();
        }
    }

    public void NotifyCanExecuteChanged() => canExecuteChanged?.Invoke(this, EventArgs.Empty);
}
