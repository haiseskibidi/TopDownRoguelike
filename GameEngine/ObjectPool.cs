using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GunVault.GameEngine
{
    /// <summary>
    /// A generic object pool to reuse objects and reduce garbage collection pressure.
    /// </summary>
    /// <typeparam name="T">The type of object to pool.</typeparam>
    public class ObjectPool<T> where T : class
    {
        private readonly ConcurrentBag<T> _pool = new ConcurrentBag<T>();
        private readonly Func<T> _factory;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;

        /// <summary>
        /// Initializes a new instance of the ObjectPool class.
        /// </summary>
        /// <param name="factory">The function to create a new object when the pool is empty.</param>
        /// <param name="onGet">An optional action to perform on an object when it is retrieved from the pool.</param>
        /// <param name="onReturn">An optional action to perform on an object when it is returned to the pool.</param>
        public ObjectPool(Func<T> factory, Action<T> onGet = null, Action<T> onReturn = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onGet = onGet;
            _onReturn = onReturn;
        }

        /// <summary>
        /// Gets an object from the pool. If the pool is empty, a new object is created using the factory.
        /// </summary>
        /// <returns>An object of type T.</returns>
        public T Get()
        {
            if (!_pool.TryTake(out T item))
            {
                item = _factory();
            }

            _onGet?.Invoke(item);
            return item;
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        /// <param name="item">The object to return.</param>
        public void Return(T item)
        {
            _onReturn?.Invoke(item);
            _pool.Add(item);
        }
    }
} 