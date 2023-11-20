using Avalonia.Controls;
using System.Threading.Tasks;

namespace UnpackerGui.Services;

public interface IDialogService
{
    Task ShowDialog(Window window);
    Task ShowDialog(Window owner, Window window);
    Task ShowTerminalDialog(Window window);
    Task ShowTerminalDialog(Window owner, Window window);
}
