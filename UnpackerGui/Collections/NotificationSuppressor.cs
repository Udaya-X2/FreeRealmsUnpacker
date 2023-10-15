using System;

namespace UnpackerGui.Collections;

public partial class RangeObservableCollection<T>
{
    /// <summary>
    /// An object that, until disposed, suppresses change notifications
    /// for a <see cref="RangeObservableCollection{T}"/>.
    /// </summary>
    private class NotificationSuppressor<U> : IDisposable
    {
        private readonly RangeObservableCollection<U> _collection;

        private bool _disposed;

        /// <summary>
        /// Suppresses change notifications for the specified <see cref="RangeObservableCollection{T}"/>
        /// until the object is disposed.
        /// </summary>
        public NotificationSuppressor(RangeObservableCollection<U> collection)
        {
            _collection = collection;
            _collection.SuppressNotifications = true;
        }

        /// <summary>
        /// Enables change notifications.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _collection.SuppressNotifications = false;
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
