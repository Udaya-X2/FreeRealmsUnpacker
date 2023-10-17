using DynamicData;
using DynamicData.Binding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;

namespace UnpackerGui.Collections;

/// <summary>
/// An override of <see cref="ObservableCollection{T}"/> which allows for the suspension of notifications.
/// </summary>
/// <seealso cref="ObservableCollectionExtended{T}"/>
/// <typeparam name="T">The type of the item.</typeparam>
public class ReactiveList<T> : ObservableCollection<T>, IObservableCollection<T>, IExtendedList<T>
{
    private readonly List<T> _items;

    private int _notificationSuppressors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveList{T}"/> class.
    /// </summary>
    public ReactiveList()
    {
        _items = (List<T>)Items;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveList{T}"/>
    /// class that contains elements copied from the specified list.
    /// </summary>
    /// <param name="list">The list from which the elements are copied.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="list"/> parameter cannot be null.</exception>
    public ReactiveList(List<T> list)
        : base(list)
    {
        _items = (List<T>)Items;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveList{T}"/>
    /// class that contains elements copied from the specified collection.
    /// </summary>
    /// <param name="collection">The collection from which the elements are copied.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="collection"/> parameter cannot be null.</exception>
    public ReactiveList(IEnumerable<T> collection)
        : base(collection)
    {
        _items = (List<T>)Items;
    }

    /// <inheritdoc cref="List{T}.Capacity"/>
    public int Capacity { get => _items.Capacity; set => _items.Capacity = value; }

    /// <summary>
    /// Gets or sets whether change notifications are enabled.
    /// </summary>
    private bool NotificationsEnabled
    {
        get => _notificationSuppressors == 0;
        set
        {
            if (!value)
            {
                _notificationSuppressors++;
            }
            else if (--_notificationSuppressors == 0)
            {
                base.OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
                base.OnPropertyChanged(EventArgsCache.CountPropertyChanged);
                base.OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);
            }
        }
    }

    /// <inheritdoc/>
    IDisposable INotifyCollectionChangedSuspender.SuspendCount() => throw new NotImplementedException();

    /// <inheritdoc/>
    public IDisposable SuspendNotifications()
    {
        NotificationsEnabled = false;
        return Disposable.Create(() => NotificationsEnabled = true);
    }

    /// <inheritdoc/>
    public void Load(IEnumerable<T> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        CheckReentrancy();
        Clear();
        AddRange(items);
    }

    /// <inheritdoc cref="List{T}.AddRange(IEnumerable{T})"/>
    public void AddRange(IEnumerable<T> collection)
        => DoWhileSuspended(x => x.AddRange(collection));

    /// <inheritdoc cref="List{T}.AsReadOnly"/>
    public void AsReadOnly() => _items.AsReadOnly();

    /// <inheritdoc cref="List{T}.BinarySearch(int, int, T, IComparer{T}?)"/>
    public void BinarySearch(int index, int count, T item, IComparer<T>? comparer)
        => _items.BinarySearch(index, count, item, comparer);

    /// <inheritdoc cref="List{T}.BinarySearch(T)"/>
    public void BinarySearch(T item)
        => _items.BinarySearch(item);

    /// <inheritdoc cref="List{T}.BinarySearch(T, IComparer{T}?)"/>
    public void BinarySearch(T item, IComparer<T>? comparer)
        => _items.BinarySearch(0, Count, item, comparer);

    /// <inheritdoc cref="List{T}.ConvertAll{TOutput}(Converter{T, TOutput})"/>
    public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        => _items.ConvertAll(converter);

    /// <inheritdoc cref="List{T}.CopyTo(int, T[], int, int)"/>
    public void CopyTo(int index, T[] array, int arrayIndex, int count)
        => _items.CopyTo(index, array, arrayIndex, count);

    /// <inheritdoc cref="List{T}.CopyTo(T[])"/>
    public void CopyTo(T[] array)
        => _items.CopyTo(array);

    /// <inheritdoc cref="List{T}.EnsureCapacity(int)"/>
    public int EnsureCapacity(int capacity)
        => _items.EnsureCapacity(capacity);

    /// <inheritdoc cref="List{T}.Exists(Predicate{T})"/>
    public bool Exists(Predicate<T> match)
        => _items.Exists(match);

    /// <inheritdoc cref="List{T}.Find(Predicate{T})"/>
    public T? Find(Predicate<T> match)
        => _items.Find(match);

    /// <inheritdoc cref="List{T}.FindAll(Predicate{T})"/>
    public List<T> FindAll(Predicate<T> match)
        => _items.FindAll(match);

    /// <inheritdoc cref="List{T}.FindIndex(int, int, Predicate{T})"/>
    public int FindIndex(int startIndex, int count, Predicate<T> match)
        => _items.FindIndex(startIndex, count, match);

    /// <inheritdoc cref="List{T}.FindIndex(int, Predicate{T})"/>
    public int FindIndex(int startIndex, Predicate<T> match)
        => _items.FindIndex(startIndex, match);

    /// <inheritdoc cref="List{T}.FindIndex(Predicate{T})"/>
    public int FindIndex(Predicate<T> match)
        => _items.FindIndex(match);

    /// <inheritdoc cref="List{T}.FindLast(Predicate{T})"/>
    public T? FindLast(Predicate<T> match)
        => _items.FindLast(match);

    /// <inheritdoc cref="List{T}.FindLastIndex(int, int, Predicate{T})"/>
    public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        => _items.FindLastIndex(startIndex, count, match);

    /// <inheritdoc cref="List{T}.FindLastIndex(int, Predicate{T})"/>
    public int FindLastIndex(int startIndex, Predicate<T> match)
        => _items.FindLastIndex(startIndex, match);

    /// <inheritdoc cref="List{T}.FindLastIndex(Predicate{T})"/>
    public int FindLastIndex(Predicate<T> match)
        => _items.FindLastIndex(match);

    /// <inheritdoc cref="List{T}.ForEach(Action{T})"/>
    public void ForEach(Action<T> action) => _items.ForEach(action);

    /// <inheritdoc cref="List{T}.GetRange(int, int)"/>
    public List<T> GetRange(int index, int count)
        => _items.GetRange(index, count);

    /// <inheritdoc cref="List{T}.IndexOf(T, int)"/>
    public int IndexOf(T item, int index)
        => _items.IndexOf(item, index);

    /// <inheritdoc cref="List{T}.IndexOf(T, int, int)"/>
    public int IndexOf(T item, int index, int count)
        => _items.IndexOf(item, index, count);

    /// <inheritdoc cref="List{T}.InsertRange(int, IEnumerable{T})"/>
    public void InsertRange(IEnumerable<T> collection, int index)
        => DoWhileSuspended(x => x.InsertRange(index, collection));

    /// <inheritdoc cref="List{T}.LastIndexOf(T)"/>
    public int LastIndexOf(T item)
        => _items.LastIndexOf(item);

    /// <inheritdoc cref="List{T}.LastIndexOf(T, int)"/>
    public int LastIndexOf(T item, int index)
        => _items.LastIndexOf(item, index);

    /// <inheritdoc cref="List{T}.LastIndexOf(T, int, int)"/>
    public int LastIndexOf(T item, int index, int count)
        => _items.LastIndexOf(item, index, count);

    /// <inheritdoc cref="List{T}.RemoveAll(Predicate{T})"/>
    public int RemoveAll(Predicate<T> match)
        => DoWhileSuspended(x => x.RemoveAll(match));

    /// <inheritdoc cref="List{T}.RemoveRange(int, int)"/>
    public void RemoveRange(int index, int count)
        => DoWhileSuspended(x => x.RemoveRange(index, count));

    /// <summary>
    /// Removes the range of elements in the specified collection from the list.
    /// </summary>
    /// <param name="collection">The range of items to remove.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="collection"/> is larger than the list.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The list does not contain the range of elements specified in <paramref name="collection"/>.
    /// </exception>
    public void RemoveRange(IEnumerable<T> collection)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));

        T? firstItem = collection.FirstOrDefault();

        if (firstItem == null) return;

        IList<T> values = collection as IList<T> ?? collection.ToList();
        int maxIndex = Count - values.Count;
        int index = -1;

    NextItem:
        index = IndexOf(firstItem, index + 1);

        if (index == -1 || index > maxIndex) throw new ArgumentException(SR.Argument_RangeNotFound);

        for (int i = 1; i < values.Count; i++)
        {
            if (!Equals(_items[index + i], values[i]))
            {
                goto NextItem;
            }
        }

        RemoveRange(index, values.Count);
    }

    /// <inheritdoc cref="List{T}.Reverse()"/>
    public void Reverse()
        => DoWhileSuspended(x => x.Reverse());

    /// <inheritdoc cref="List{T}.Reverse(int, int)"/>
    public void Reverse(int index, int count)
        => DoWhileSuspended(x => x.Reverse(index, count));

    /// <inheritdoc cref="List{T}.Sort()"/>
    public void Sort()
        => DoWhileSuspended(x => x.Sort());

    /// <inheritdoc cref="List{T}.Sort(Comparison{T})"/>
    public void Sort(Comparison<T> comparison)
        => DoWhileSuspended(x => x.Sort(comparison));

    /// <inheritdoc cref="List{T}.Sort(IComparer{T}?)"/>
    public void Sort(IComparer<T>? comparer)
        => DoWhileSuspended(x => x.Sort(comparer));

    /// <inheritdoc cref="List{T}.Sort(int, int, IComparer{T}?)"/>
    public void Sort(int index, int count, IComparer<T>? comparer)
        => DoWhileSuspended(x => x.Sort(index, count, comparer));

    /// <inheritdoc cref="List{T}.ToArray"/>
    public T[] ToArray()
        => _items.ToArray();

    /// <inheritdoc cref="List{T}.TrimExcess"/>
    public void TrimExcess()
        => _items.TrimExcess();

    /// <inheritdoc cref="List{T}.TrueForAll(Predicate{T})"/>
    public bool TrueForAll(Predicate<T> match)
        => _items.TrueForAll(match);

    /// <inheritdoc/>
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (NotificationsEnabled)
        {
            base.OnCollectionChanged(e);
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (NotificationsEnabled)
        {
            base.OnPropertyChanged(e);
        }
    }

    /// <summary>
    /// Performs the specified action on the list with change notifications suspended during the action.
    /// </summary>
    private void DoWhileSuspended(Action<List<T>> action)
    {
        using (SuspendNotifications())
        {
            action(_items);
        }
    }

    /// <summary>
    /// Returns the result of calling the specified function on the
    /// list with change notifications suspended during the call.
    /// </summary>
    /// <returns>The result of calling the specified function on the list.</returns>
    private TResult DoWhileSuspended<TResult>(Func<List<T>, TResult> selector)
    {
        using (SuspendNotifications())
        {
            return selector(_items);
        }
    }
}
