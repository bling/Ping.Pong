using System;
using System.Windows.Input;

namespace PingPong
{
    public class DelegateCommand<T> : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public DelegateCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute ?? (_ => true);
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            var e = CanExecuteChanged;
            if (e != null)
                e(this, EventArgs.Empty);
        }
    }

    public class DelegateCommand : DelegateCommand<object>
    {
        public DelegateCommand(Action<object> execute, Func<object, bool> canExecute = null)
            : base(execute, canExecute)
        {
        }
    }
}