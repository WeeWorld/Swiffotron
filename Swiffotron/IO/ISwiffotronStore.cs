//-----------------------------------------------------------------------
// ISwiffotronStore.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.IO
{
    using System.IO;

    /// <summary>
    /// Interface for a store that can be created via a swiffotron config
    /// XML file.
    /// </summary>
    public interface ISwiffotronStore
    {
        /// <summary>
        /// Called on creation of the store object.
        /// </summary>
        /// <param name="init">An implementation-specific initialisation
        /// string.</param>
        void Initialise(string init);

        /// <summary>
        /// To save data, first call this method, then write all your
        /// data, then call <see cref="Commit"/> to complete the
        /// save.
        /// </summary>
        /// <param name="id">A string identifier that uniquely identifies
        /// some data, and which is meaningful to the store implementation,
        /// e.g. a full path on a file store, or a database ID on a DB
        /// store.</param>
        /// <returns>A stream to write to.</returns>
        Stream OpenOutput(string id);

        /// <summary>
        /// Once you've stored data stored with OpenOutput, call this method
        /// to complete the operation.
        /// </summary>
        /// <param name="id">The store ID passed to the OpenOutput method
        /// that you are committing.</param>
        void Commit(string id);

        /// <summary>
        /// Open a stream input from the store.
        /// </summary>
        /// <param name="id">The store ID to read from. Note for implementors: This
        /// will be a local path from a URI, so will need to be URL encoded if you
        /// intend to treat it as a URL.</param>
        /// <returns>A stream to read from, or null of the stored object doesn't
        /// exist.</returns>
        Stream OpenInput(string id);
    }
}
