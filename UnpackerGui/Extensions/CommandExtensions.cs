using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;

namespace UnpackerGui.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ReactiveCommandBase{TParam, TResult}"/>.
/// </summary>
public static class CommandExtensions
{
    /// <summary>
    /// Executes the specified command, if possible.
    /// </summary>
    /// <typeparam name="TResult">The type of value returned by command executions.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <returns>An object that, when disposed, disconnects the observable from the command.</returns>
    public static IDisposable Invoke<TResult>(this ReactiveCommandBase<Unit, TResult> command)
        => command == null
        ? throw new ArgumentNullException(nameof(command))
        : Observable.Return(Unit.Default).InvokeCommand(command);

    /// <summary>
    /// Executes the specified command with the given parameter, if possible.
    /// </summary>
    /// <typeparam name="TParam">The type of the parameter passed through to command execution.</typeparam>
    /// <typeparam name="TResult">The type of value returned by command executions.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="parameter">The parameter to pass to the command.</param>
    /// <returns>An object that, when disposed, disconnects the observable from the command.</returns>
    public static IDisposable Invoke<TParam, TResult>(this ReactiveCommandBase<TParam, TResult> command,
                                                      TParam parameter)
        => command == null
        ? throw new ArgumentNullException(nameof(command))
        : Observable.Return(parameter).InvokeCommand(command);
}
