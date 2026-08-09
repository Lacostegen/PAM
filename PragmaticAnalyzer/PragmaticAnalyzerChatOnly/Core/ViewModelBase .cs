using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PragmaticAnalyzer.Core
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private readonly Dictionary<string, object?> _properties = [];
        private readonly Dictionary<string, RelayCommand> _commands = [];
        public event PropertyChangedEventHandler? PropertyChanged;

        protected T? Get<T>([CallerMemberName] string propertyName = "")
        {
            T? value;
            if (_properties.TryGetValue(propertyName, out object? _prop))
            {
                value = (T?)_prop;
            }
            else
            {
                value = default;
            }
            return value;
        }

        protected void Set<T>(T newValue, [CallerMemberName] string propertyName = "")
        {
            _properties[propertyName] = newValue;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            CommandManager.InvalidateRequerySuggested();
        }

        protected RelayCommand GetCommand(Action<object> execute, Predicate<object>? canExecute = null, [CallerMemberName] string commandName = null)
        {
            if (!_commands.TryGetValue(commandName, out var command))
            {
                command = new RelayCommand(execute, canExecute);
                _commands[commandName] = command;
            }
            return command;
        }

        protected void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    } // реализация интерфейса INotifyPropertyChanged для дальнейшего наследование vm + реализация свойств с автоматическими уведомлениями
}