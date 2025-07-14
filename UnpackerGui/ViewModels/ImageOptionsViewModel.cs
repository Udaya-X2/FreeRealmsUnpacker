using System;
using System.ComponentModel.DataAnnotations;

namespace UnpackerGui.ViewModels;

/// <summary>
/// Represents a filter based on the image extension of the specified type.
/// </summary>
/// <typeparam name="T">The type of the item to filter.</typeparam>
/// <param name="converter">The converter from <typeparamref name="T"/> to an image extension.</param>
public class ImageOptionsViewModel<T> : FilterViewModel<T>
{
    private readonly Func<T, string> _converter;

    public ImageOptionsViewModel(Func<T, string> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        IsMatch = x => _converter(x) is ".dds" or ".png" or ".jpg" or ".gif" or ".bmp" or ".tga";
    }

    /// <summary>
    /// Returns <see langword="true"/> if the search options always produce a match; otherwise, <see langword="false"/>.
    /// </summary>
    public override bool IsAlwaysMatch => false;

    /// <summary>
    /// Updates the match predicate according to the current search options.
    /// </summary>
    /// <exception cref="ValidationException">If the regular expression is invalid.</exception>
    protected override void UpdateMatchPredicate() { }
}
