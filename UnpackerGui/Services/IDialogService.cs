using Avalonia.Controls;
using System;
using System.Threading.Tasks;

namespace UnpackerGui.Services;

public interface IDialogService
{
    Task ShowDialog(Window window, bool terminal = false);
    Task ShowDialog(Window owner, Window window, bool terminal = false);
    Task ShowErrorDialog(Exception exception, bool terminal = false, bool unhandled = false);
    Task ShowErrorDialog(Window owner, Exception exception, bool terminal = false, bool unhandled = false);
}
