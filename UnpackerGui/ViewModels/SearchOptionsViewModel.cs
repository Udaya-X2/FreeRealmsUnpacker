using ReactiveUI;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace UnpackerGui.ViewModels;

public class SearchOptionsViewModel<T> : ViewModelBase
{
    private readonly Func<T, string> _converter;

    private bool _matchCase;
    private bool _useRegex;
    private string _pattern;
    private Func<T, bool> _isMatch;

    public SearchOptionsViewModel(Func<T, string> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        _pattern = "";
        _isMatch = _ => true;
    }

    public bool MatchCase
    {
        get => _matchCase;
        set
        {
            this.RaiseAndSetIfChanged(ref _matchCase, value);
            UpdateMatchPredicate();
        }
    }

    public bool UseRegex
    {
        get => _useRegex;
        set
        {
            this.RaiseAndSetIfChanged(ref _useRegex, value);
            UpdateMatchPredicate();
        }
    }

    public string Pattern
    {
        get => _pattern;
        set
        {
            this.RaiseAndSetIfChanged(ref _pattern, value);
            UpdateMatchPredicate();
        }
    }

    public Func<T, bool> IsMatch
    {
        get => _isMatch;
        set => this.RaiseAndSetIfChanged(ref _isMatch, value);
    }

    public bool IsAlwaysMatch => Pattern is "" || (UseRegex && Pattern is "^" or "$" or "^$");

    private void UpdateMatchPredicate()
    {
        if (UseRegex)
        {
            try
            {
                RegexOptions caseOption = MatchCase ? 0 : RegexOptions.IgnoreCase;
                Regex regex = new(Pattern, RegexOptions.Compiled | caseOption);
                IsMatch = x => regex.IsMatch(_converter(x));
            }
            catch (ArgumentException ex)
            {
                throw new ValidationException(ex.Message);
            }
        }
        else
        {
            StringComparison cmpType = MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            IsMatch = x => _converter(x).Contains(Pattern, cmpType);
        }
    }
}
