using System;
using System.Windows.Input;

namespace Database {
    public class DelegateCommand : ICommand {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public bool CanExecute(object parameter) {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter) {
            _execute?.Invoke(parameter);
        }

        public event EventHandler CanExecuteChanged {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public DelegateCommand(Action<object> executeAction) : this(executeAction, null) { }

        public DelegateCommand(Action<object> executeAction, Func<object, bool> canExecuteFunc) {
            _execute = executeAction;
            _canExecute = canExecuteFunc;
        }
    }
}