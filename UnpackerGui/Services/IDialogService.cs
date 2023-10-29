using Avalonia.Controls;
using System.Threading.Tasks;

namespace UnpackerGui.Services;

public interface IDialogService
{
    public Task ShowDialog(Window window);
}
