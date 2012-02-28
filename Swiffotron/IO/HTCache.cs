//-----------------------------------------------------------------------
// HTCache.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.IO
{
    using System.Collections.Generic;

    /// <summary>
    /// This is a pretty dumb class. It's useful to use as a cache in unit tests
    /// and so forth, but not much else. The main problem is that nothing ever
    /// gets removed from it. It just grows and grows until things break.
    /// </summary>
    public class HTCache : ISwiffotronCache
    {
        /// <summary>
        /// The hash backed cache of objects.
        /// </summary>
        private Dictionary<string, object> cache;

        /// <summary>
        /// Gets the string that was passed into Initialise
        /// </summary>
        public string InitialisedWith { get; private set; }

        /// <summary>
        /// Called on creation of the cache object.
        /// </summary>
        /// <param name="init">Ignored in this implementation.</param>
        public void Initialise(string init)
        {
            this.InitialisedWith = init;

            /* The init string doesn't do anything here. */
            this.cache = new Dictionary<string, object>();
        }

        /// <summary>
        /// Test if the cache contains some key.
        /// </summary>
        /// <param name="key">The key to test</param>
        /// <returns>true if the object exists.</returns>
        public bool Has(string key)
        {
            return this.cache.ContainsKey(key);
        }

        /// <summary>
        /// Put a new object into the cache. If the object exists it is
        /// overwritten transparently.
        /// </summary>
        /// <param name="key">The key to store against.</param>
        /// <param name="value">The value to store.</param>
        public void Put(string key, object value)
        {
            this.cache[key] = value;
        }

        /// <summary>
        /// Get an object from the cache.
        /// </summary>
        /// <param name="key">The key to the object to retrieve</param>
        /// <returns>The object, or null if it's not there.</returns>
        public object Get(string key)
        {
            if (!this.cache.ContainsKey(key))
            {
                return null;
            }

            return this.cache[key];
        }
    }
}
