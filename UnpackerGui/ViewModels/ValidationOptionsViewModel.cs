using System;

namespace UnpackerGui.ViewModels;

/// <summary>
/// Represents a filter based on the validity of a specified type.
/// </summary>
/// <typeparam name="T">The type of the item to filter.</typeparam>
public class ValidationOptionsViewModel<T> : FilterViewModel<T>
{
    private readonly Func<T, bool> _converter;

    private bool? _showValid;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationOptionsViewModel{T}"/>
    /// class from the specified validation property selector.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public ValidationOptionsViewModel(Func<T, bool> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    /// <summary>
    /// Gets or sets whether the filter is satisfied by valid, invalid, or both states.
    /// </summary>
    public bool? ShowValid
    {
        get => _showValid;
        set => SetValue(ref _showValid, value);
    }

    /// <inheritdoc/>
    public override bool IsAlwaysMatch => ShowValid == null;

    /// <inheritdoc/>
    protected override void UpdateMatchPredicate() => IsMatch = ShowValid switch
    {
        false => x => !_converter(x),
        true => _converter,
        null => _ => true
    };
}
