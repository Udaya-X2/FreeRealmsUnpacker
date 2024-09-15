using AssetIO;
using ReactiveUI;
using System;

namespace UnpackerGui.ViewModels;

public class PreferencesViewModel : ViewModelBase
{
    private FileConflictOptions _conflictOptions;

    public PreferencesViewModel(MainViewModel mainViewModel)
    {
        _conflictOptions = mainViewModel.ConflictOptions;
        this.WhenAnyValue(x => x.ConflictOptions)
            .Subscribe(x => mainViewModel.ConflictOptions = x);
    }

    public FileConflictOptions ConflictOptions
    {
        get => _conflictOptions;
        set => this.RaiseAndSetIfChanged(ref _conflictOptions, value);
    }
}
