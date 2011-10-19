//-----------------------------------------------------------------------
// ISwiffotronCache.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.IO
{
    /// <summary>
    /// Interface for a cache that can be created via a swiffotron config
    /// XML file.
    /// </summary>
    public interface ISwiffotronCache
    {
        /// <summary>
        /// Called on creation of the cache object.
        /// </summary>
        /// <param name="init">An implementation-specific initialisation
        /// string. The class should record this
        /// string for debug interrogation later.</param>
        void Initialise(string init);

        /// <summary>
        /// Test if the cache contains some key.
        /// </summary>
        /// <param name="key">The key to test</param>
        /// <returns>true if the object exists.</returns>
        bool Has(string key);

        /// <summary>
        /// Put a new object into the cache. If the object exists it is
        /// overwritten transparently.
        /// </summary>
        /// <param name="key">The key to store against.</param>
        /// <param name="value">The value to store.</param>
        void Put(string key, object value);

        /// <summary>
        /// Get an object from the cache.
        /// </summary>
        /// <param name="key">The key to the object to retrieve</param>
        /// <returns>The object, or null if it's not there.</returns>
        object Get(string key);

        /// <summary>
        /// Returns the string that was passed into Initialise
        /// </summary>
        string InitialisedWith { get; }
    }
}
