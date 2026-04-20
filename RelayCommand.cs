using System;
using System.Windows.Input;

namespace BarberShopWPF.Helpers
{
    /// <summary>Generic RelayCommand dùng cho mọi ViewModel.</summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _exec;
        private readonly Func<object?, bool>? _canExec;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        { _exec = execute; _canExec = canExecute; }

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(_ => execute(), canExecute is null ? null : _ => canExecute()) { }

        public bool CanExecute(object? p) => _canExec is null || _canExec(p);
        public void Execute(object? p) => _exec(p);

        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    /// <summary>Typed RelayCommand&lt;T&gt;.</summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _exec;
        private readonly Func<T?, bool>? _canExec;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        { _exec = execute; _canExec = canExecute; }

        public bool CanExecute(object? p)
            => _canExec is null || _canExec(p is T t ? t : default);
        public void Execute(object? p)
            => _exec(p is T t ? t : default);

        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
