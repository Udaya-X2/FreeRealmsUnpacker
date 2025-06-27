using Avalonia.Controls;
using System;
using System.Threading.Tasks;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Services;

public interface IDialogService
{
    Task ShowDialog(Window window, bool terminal = false);
    Task ShowErrorDialog(Exception exception, bool terminal = false, bool unhandled = false);
    Task<bool> ShowConfirmDialog(ConfirmViewModel confirm, bool terminal = false);
}
