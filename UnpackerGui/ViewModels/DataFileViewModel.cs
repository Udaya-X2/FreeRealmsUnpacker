using ReactiveUI;
using System.IO;

namespace UnpackerGui.ViewModels;

public class DataFileViewModel(string path) : ViewModelBase
{
    private readonly FileInfo _info = new(path);

    public string Name => _info.Name;
    public string FullName => _info.FullName;
    public string? DirectoryName => _info.DirectoryName;

    /// <summary>
    /// Moves the data file to a new location, providing the option to specify a new file name.
    /// </summary>
    public void MoveTo(string destFileName)
    {
        if (FullName != destFileName)
        {
            _info.MoveTo(destFileName, overwrite: true);
            this.RaisePropertyChanged(nameof(Name));
            this.RaisePropertyChanged(nameof(FullName));
            this.RaisePropertyChanged(nameof(DirectoryName));
        }
    }

    /// <summary>
    /// Permanently deletes the data file.
    /// </summary>
    public void Delete() => _info.Delete();
}
