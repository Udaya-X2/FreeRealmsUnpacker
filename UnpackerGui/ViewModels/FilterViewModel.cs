﻿using ReactiveUI;
using System;
using System.Runtime.CompilerServices;

namespace UnpackerGui.ViewModels;

/// <summary>
/// Represents a filter on a specified type.
/// </summary>
/// <param name="predicate">The predicate to filter <typeparamref name="T"/>.</param>
/// <typeparam name="T">The type of the item to filter.</typeparam>
public class FilterViewModel<T>(Func<T, bool> predicate) : ViewModelBase
{
    private static readonly Func<T, bool> s_truePredicate = static _ => true;

    private Func<T, bool> _isMatch = predicate;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterViewModel{T}"/> class.
    /// </summary>
    protected FilterViewModel()
        : this(s_truePredicate)
    {
    }

    /// <summary>
    /// Gets or sets the filter condition.
    /// </summary>
    public Func<T, bool> IsMatch
    {
        get => _isMatch;
        set => this.RaiseAndSetIfChanged(ref _isMatch, value);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the filter condition is always satisfied; otherwise, <see langword="false"/>.
    /// </summary>
    public virtual bool IsAlwaysMatch => _isMatch == s_truePredicate;

    /// <summary>
    /// Updates the match predicate.
    /// </summary>
    protected virtual void UpdateMatchPredicate() { }

    /// <inheritdoc cref="IReactiveObjectExtensions.RaiseAndSetIfChanged{TObj, TRet}(TObj, ref TRet, TRet, string?)"/>
    protected TRet SetValue<TRet>(ref TRet backingField, TRet newValue, [CallerMemberName] string? propertyName = null)
    {
        TRet value = this.RaiseAndSetIfChanged(ref backingField, newValue, propertyName);
        UpdateMatchPredicate();
        return value;
    }
}
