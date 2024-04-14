using System.Runtime.CompilerServices;

namespace MZikmund.ViewModels;

public abstract class ViewModelBase : ObservableRecipient
{
	private readonly Dictionary<string, ICommand> _commands = new();

	protected ICommand GetOrCreateCommand(Action action, [CallerMemberName] string commandName = "")
	{
		if (!_commands.TryGetValue(commandName, out var command))
		{
			command = new RelayCommand(action);
			_commands.Add(commandName, command);
		}
		return command;
	}

	protected ICommand GetOrCreateCommand(Action action, Func<bool> canExecute, [CallerMemberName] string commandName = "")
	{
		if (!_commands.TryGetValue(commandName, out var command))
		{
			command = new RelayCommand(action, canExecute);
			_commands.Add(commandName, command);
		}
		return command;
	}

	protected ICommand GetOrCreateCommand<T>(Action<T?> action, [CallerMemberName] string commandName = "")
	{
		if (!_commands.TryGetValue(commandName, out var command))
		{
			command = new RelayCommand<T>(action);
			_commands.Add(commandName, command);
		}
		return command;
	}

	protected ICommand GetOrCreateCommand<T>(Action<T?> action, Predicate<T?> canExecute, [CallerMemberName] string commandName = "")
	{
		if (!_commands.TryGetValue(commandName, out var command))
		{
			command = new RelayCommand<T>(action, canExecute);
			_commands.Add(commandName, command);
		}
		return command;
	}

	protected ICommand GetOrCreateAsyncCommand(Func<Task> action, [CallerMemberName] string commandName = "")
	{
		if (!_commands.TryGetValue(commandName, out var command))
		{
			command = new AsyncRelayCommand(action);
			_commands.Add(commandName, command);
		}
		return command;
	}

	protected ICommand GetOrCreateAsyncCommand(Func<Task> action, Func<bool> canExecute, [CallerMemberName] string commandName = "")
	{
		if (!_commands.TryGetValue(commandName, out var command))
		{
			command = new AsyncRelayCommand(action, canExecute);
			_commands.Add(commandName, command);
		}
		return command;
	}

	protected ICommand GetOrCreateAsyncCommand<T>(Func<T?, Task> action, [CallerMemberName] string commandName = "")
	{
		if (!_commands.TryGetValue(commandName, out var command))
		{
			command = new AsyncRelayCommand<T>(action);
			_commands.Add(commandName, command);
		}
		return command;
	}

	protected ICommand GetOrCreateAsyncCommand<T>(Func<T?, Task> action, Predicate<T?> canExecute, [CallerMemberName] string commandName = "")
	{
		if (!_commands.TryGetValue(commandName, out var command))
		{
			command = new AsyncRelayCommand<T>(action, canExecute);
			_commands.Add(commandName, command);
		}
		return command;
	}
}
