//-----------------------------------------------------------------------
// FileStore.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.IO
{
    using System.IO;

    /// <summary>
    /// A store class that stores SWF data on the file system.
    /// </summary>
    public class FileStore : ISwiffotronStore
    {
        /// <summary>
        /// The base folder under which to store things.
        /// </summary>
        private string basePath = string.Empty;

        public string InitialisedWith { get; private set; }

        /// <summary>
        /// Called when the store is created
        /// </summary>
        /// <param name="init">An initialisation string. Normally this comes from
        /// an XML config file.</param>
        public void Initialise(string init)
        {
            InitialisedWith = init;

            if (init != null)
            {
                this.basePath = init;
            }
        }

        /// <summary>
        /// Open a store object to write data to. If the object already exists, it will
        /// be overwritten.
        /// </summary>
        /// <param name="id">The identifier of the object.</param>
        /// <returns>A stream which can be written to. The caller is responsible for
        /// closing this stream.</returns>
        public Stream OpenOutput(string id)
        {
            return File.OpenWrite(this.basePath + id);
        }

        /// <summary>
        /// Some stores require commit calls to close output. This implementation
        /// will require that the stream is closed, but you should still call
        /// Commit in order to comply with the interface contract.
        /// </summary>
        /// <param name="id">The object you're commiting.</param>
        public void Commit(string id)
        {
            /* No need for commit handling. Caller is responsible for
             * calling Close on the stream, and that's all we care about
             * on writes. */
        }

        /// <summary>
        /// Open a stored object for input
        /// </summary>
        /// <param name="id">The string identifier for the object</param>
        /// <returns>An open input stream</returns>
        public Stream OpenInput(string id)
        {
            return File.OpenRead(this.basePath + id);
        }
    }
}
