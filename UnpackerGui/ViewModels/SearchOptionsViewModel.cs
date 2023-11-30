using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace UnpackerGui.ViewModels;

/// <summary>
/// Represents a set of string-based search options to filter a specified type.
/// </summary>
/// <typeparam name="T">The type of the item to filter.</typeparam>
public class SearchOptionsViewModel<T> : FilterViewModel<T>
{
    private readonly Func<T, string> _converter;

    private bool _matchCase;
    private bool _useRegex;
    private string _pattern;

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
    }

    /// <summary>
    /// Gets or sets whether the search should be case-sensitive.
    /// </summary>
    public bool MatchCase
    {
        get => _matchCase;
        set => SetValue(ref _matchCase, value);
    }

    /// <summary>
    /// Gets or sets whether to interpret the search pattern as a regular expression.
    /// </summary>
    public bool UseRegex
    {
        get => _useRegex;
        set => SetValue(ref _useRegex, value);
    }

    /// <summary>
    /// Gets or sets the search pattern.
    /// </summary>
    public string Pattern
    {
        get => _pattern;
        set => SetValue(ref _pattern, value);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the search options always produce a match; otherwise, <see langword="false"/>.
    /// </summary>
    public override bool IsAlwaysMatch => Pattern is "" || (UseRegex && Pattern is "^" or "$" or "^$");

    /// <summary>
    /// Updates the match predicate according to the current search options.
    /// </summary>
    /// <exception cref="ValidationException">If the regular expression is invalid.</exception>
    protected override void UpdateMatchPredicate()
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
