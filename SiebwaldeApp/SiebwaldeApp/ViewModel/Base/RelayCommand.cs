using System;
using System.Windows.Input;

namespace SiebwaldeApp
{

    /// <summary>
    /// A basic, WPF-independent RelayCommand.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action execute)
            : this(_ => execute(), _ => true) { }

        public RelayCommand(Action execute, Func<bool> canExecute)
            : this(_ => execute(), _ => canExecute()) { }

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Nullable-correct ICommand event
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);

        /// <summary>Manually notify listeners that CanExecute has changed.</summary>
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    ///// <summary>
    ///// A basic command that runs an Action
    ///// </summary>
    //public class RelayCommand : ICommand
    //{
    //    #region Private Members

    //    /// <summary>
    //    /// The action to run
    //    /// </summary>
    //    private Action mAction;

    //    #endregion

    //    #region Public events

    //    /// <summary>
    //    /// The event thats fired when the <see cref="CanExecute(object)"/> value has changed
    //    /// </summary>
    //    public event EventHandler CanExecuteChanged = (Sender, e) => { };

    //    #endregion

    //    #region Constructor

    //    /// <summary>
    //    /// Default Constructor
    //    /// </summary>
    //    public RelayCommand(Action action)
    //    {
    //        mAction = action;
    //    }

    //    #endregion

    //    #region Command Methods

    //    /// <summary>
    //    /// A relay command can always execute
    //    /// </summary>
    //    /// <param name="parameter"></param>
    //    /// <returns></returns>
    //    public bool CanExecute(object parameter)
    //    {
    //        return true;
    //    }

    //    /// <summary>
    //    /// Executes the commands Action
    //    /// </summary>
    //    /// <param name="parameter"></param>
    //    public void Execute(object parameter)
    //    {
    //        mAction();
    //    }

    //    #endregion
    //}
}
