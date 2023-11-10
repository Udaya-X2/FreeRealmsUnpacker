using ReactiveUI;
using System;
using System.Text.RegularExpressions;

namespace UnpackerGui.ViewModels;

public class SearchOptionsViewModel<T> : ViewModelBase
{
    private readonly Func<T, string> _converter;

    private bool _matchCase;
    private bool _useRegex;
    private string _pattern;

    public SearchOptionsViewModel(Func<T, string> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        _pattern = "";
    }

    public bool MatchCase
    {
        get => _matchCase;
        set => this.RaiseAndSetIfChanged(ref _matchCase, value);
    }

    public bool UseRegex
    {
        get => _useRegex;
        set => this.RaiseAndSetIfChanged(ref _useRegex, value);
    }

    public string Pattern
    {
        get => _pattern;
        set => this.RaiseAndSetIfChanged(ref _pattern, value);
    }

    public bool IsAlwaysMatch => Pattern is "" || (UseRegex && Pattern is "^" or "$" or "^$");

    public bool IsMatch(T input) => (UseRegex, MatchCase) switch
    {
        (true, true) => Regex.IsMatch(_converter(input), Pattern),
        (true, false) => Regex.IsMatch(_converter(input), Pattern, RegexOptions.IgnoreCase),
        (false, true) => _converter(input).Contains(Pattern, StringComparison.Ordinal),
        (false, false) => _converter(input).Contains(Pattern, StringComparison.OrdinalIgnoreCase)
    };
}
