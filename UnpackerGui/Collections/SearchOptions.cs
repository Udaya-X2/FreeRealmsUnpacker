using System;
using System.Text.RegularExpressions;

namespace UnpackerGui.Collections;

public class SearchOptions<T>
{
    public bool IsCaseInsensitive { get; set; } = true;
    public string Pattern { get; set; } = "";

    private readonly Func<T, string> _converter;

    private Regex? _regex;

    public SearchOptions(Func<T, string> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    public bool IsRegex
    {
        set => _regex = (value, IsCaseInsensitive) switch
        {
            (true, true) => new Regex(Pattern),
            (true, false) => new Regex(Pattern, RegexOptions.IgnoreCase),
            (false, _) => null
        };
    }

    public bool IsAlwaysMatch => (Pattern is "") || (_regex is not null && Pattern is "^" or "$" or "^$");

    public bool IsMatch(T input) => (_regex, IsCaseInsensitive) switch
    {
        (not null, _) => _regex.IsMatch(_converter(input)),
        (null, true) => _converter(input).Contains(Pattern, StringComparison.OrdinalIgnoreCase),
        (null, false) => _converter(input).Contains(Pattern, StringComparison.Ordinal)
    };
}
