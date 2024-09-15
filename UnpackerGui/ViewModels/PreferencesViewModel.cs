using AssetIO;
using ReactiveUI;
using System;
using System.Diagnostics;

namespace UnpackerGui.ViewModels;

public class PreferencesViewModel : ViewModelBase
{
    private FileConflictOptions _conflictOptions;

    public PreferencesViewModel(MainViewModel mainViewModel)
    {
        _conflictOptions = mainViewModel.ConflictOptions;
        this.WhenAnyValue(x => x.ConflictOptions)
            .Subscribe(x => Debug.WriteLine($"ConflictOptions = {mainViewModel.ConflictOptions = x}"));
    }

    public FileConflictOptions ConflictOptions
    {
        get => _conflictOptions;
        set => this.RaiseAndSetIfChanged(ref _conflictOptions, value);
    }
}
