using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace UnpackerGui.ViewModels;

/// <summary>
/// Represents a filter based on the image extension of the specified type.
/// </summary>
/// <typeparam name="T">The type of the item to filter.</typeparam>
public class ImageOptionsViewModel<T> : FilterViewModel<T>
{
    private static readonly HashSet<string> s_imageFormats = [.. Enum.GetValues<SKEncodedImageFormat>()
                                                                     .Except([SKEncodedImageFormat.Avif])
                                                                     .Select(x => x.ToString().ToUpperInvariant())
                                                                     .Concat(["DDS", "TGA"])];

    private readonly Func<T, string> _converter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageOptionsViewModel{T}"/> class.
    /// </summary>
    /// <param name="converter">The converter from <typeparamref name="T"/> to an image extension.</param>
    /// <exception cref="ArgumentNullException"/>
    public ImageOptionsViewModel(Func<T, string> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        IsMatch = x => s_imageFormats.Contains(_converter(x));
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
