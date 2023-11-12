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

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchOptionsViewModel{T}"/> class
    /// with the specified converter from <typeparamref name="T"/> to a searchable string.
    /// </summary>
    /// <param name="converter"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public SearchOptionsViewModel(Func<T, string> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        _pattern = "";
        _isMatch = _ => true;
    }

    /// <summary>
    /// Gets or sets whether the search should be case-sensitive.
    /// </summary>
    public bool MatchCase
    {
        get => _matchCase;
        set
        {
            this.RaiseAndSetIfChanged(ref _matchCase, value);
            UpdateMatchPredicate();
        }
    }

    /// <summary>
    /// Gets or sets whether to interpret the search pattern as a regular expression.
    /// </summary>
    public bool UseRegex
    {
        get => _useRegex;
        set
        {
            this.RaiseAndSetIfChanged(ref _useRegex, value);
            UpdateMatchPredicate();
        }
    }

    /// <summary>
    /// Gets or sets the search pattern.
    /// </summary>
    public string Pattern
    {
        get => _pattern;
        set
        {
            this.RaiseAndSetIfChanged(ref _pattern, value);
            UpdateMatchPredicate();
        }
    }

    /// <summary>
    /// Gets or sets the search predicate.
    /// </summary>
    public Func<T, bool> IsMatch
    {
        get => _isMatch;
        set => this.RaiseAndSetIfChanged(ref _isMatch, value);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the search options always produce a match; otherwise, <see langword="false"/>.
    /// </summary>
    public bool IsAlwaysMatch => Pattern is "" || (UseRegex && Pattern is "^" or "$" or "^$");

    /// <summary>
    /// Updates the match predicate according to the current search options.
    /// </summary>
    /// <exception cref="ValidationException">If the regular expression is invalid.</exception>
    private void UpdateMatchPredicate()
    {
        if (UseRegex)
        {
            try
            {
                RegexOptions caseOption = MatchCase ? 0 : RegexOptions.IgnoreCase;
                Regex regex = new(Pattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture | caseOption);
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
