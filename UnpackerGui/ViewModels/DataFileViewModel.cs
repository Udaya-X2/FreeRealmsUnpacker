using ReactiveUI;
using System.IO;
using UnpackerGui.Converters;

namespace UnpackerGui.ViewModels;

public class DataFileViewModel : ViewModelBase
{
    private readonly FileInfo _info;
    private readonly string _size;

    public DataFileViewModel(string path)
    {
        _info = new(path);
        _size = FileSizeConverter.GetFileSize(_info.Length);
    }

    /// <inheritdoc cref="FileInfo.Name"/>
    public string Name => _info.Name;

    /// <inheritdoc cref="FileSystemInfo.FullName"/>
    public string FullName => _info.FullName;

    /// <inheritdoc cref="FileInfo.DirectoryName"/>
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

    /// <summary>
    /// Returns a string representation of the data file's properties.
    /// </summary>
    /// <returns>A string representation of the data file's properties.</returns>
    public override string ToString() => $"{Name}\nSize: {_size}\nLocation: {DirectoryName}";
}
