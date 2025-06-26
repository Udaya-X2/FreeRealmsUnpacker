using Avalonia.Controls;
using FluentIcons.Common;
using System;
using System.Threading.Tasks;

namespace UnpackerGui.Services;

public interface IDialogService
{
    Task ShowDialog(Window window, bool terminal = false);
    Task ShowErrorDialog(Exception exception, bool terminal = false, bool unhandled = false);
    Task<bool> ShowConfirmDialog(string message, Icon icon = Icon.QuestionCircle, bool terminal = false);
}
