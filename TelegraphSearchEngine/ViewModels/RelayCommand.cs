using System.Diagnostics;
using System.Windows.Input;

namespace TelegraphSearchEngine.ViewModels;

// avalonia has no CommandManager.RequerySuggested, so canexecute changes are
// announced explicitly through RaiseCanExecuteChanged
public sealed class RelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public async void Execute(object? parameter)
    {
        try
        {
            await ExecuteAsync(parameter);
        }
        catch (Exception exception)
        {
            // icommand.execute is async void by design; never let an exception escape it
            Debug.WriteLine(exception);
        }
    }

    // awaitable path for tests and programmatic callers
    public Task ExecuteAsync(object? parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
