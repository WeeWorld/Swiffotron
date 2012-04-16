//-----------------------------------------------------------------------
// Stores.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.Processor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using SWFProcessing.Swiffotron.IO;

    public class Stores
    {
        /// <summary>A map of store names to store implementation instances.</summary>
        private Dictionary<string, ISwiffotronStore> stores;

#if(DEBUG)
        /* We could use MSTest's private access, but we want the express versions
         * to be in on the fun too. Horrible, but there you go. */
        public Dictionary<string, ISwiffotronStore> stores_accessor
        {
            get
            {
                return this.stores;
            }
        }
#endif

        public Configuration conf { get; private set; }

        public Stores(Configuration conf)
        {
            this.conf = conf;
            this.stores = new Dictionary<string, ISwiffotronStore>();
        }

        public void Register(string name, ISwiffotronStore store)
        {
            this.stores.Add(name, store);
        }

        /// <summary>
        /// Get some useful information for debug purposes letting us find out how things
        /// are set up. I should list them all here, really.
        /// </summary>
        /// <param name="info">An accumulating big map of arbitrary string->string data
        /// that you can pick apart and use as you so desire.</param>
        public void Interrogate(Dictionary<string, string> info)
        {
            List<string> storeClasses = new List<string>();
            foreach (KeyValuePair<string, ISwiffotronStore> storeEntry in this.stores)
            {
                storeClasses.Add(storeEntry.Key + "=" + storeEntry.Value.GetType().FullName + "[" + storeEntry.Value.InitialisedWith + "]");
            }

            storeClasses.Sort();

            /* StoreClasses holds a comma-separated list of all the cache classes used by swiffotron
             * as key-value pairs, e.g. db=com.blah.MyOtherClass,fs=com.blah.MyClass
             * sorted into alphabetical order. */
            info.Add("StoreClasses", string.Join(",", storeClasses.ToArray()));
        }


        /// <summary>
        /// Writes a block of data to the store specified in the fully qualified
        /// store key of the form [store name]:[store key]
        /// </summary>
        /// <param name="key">The store key</param>
        /// <param name="data">The data to store as a byte array.</param>
        /// <returns>Null if it was not saved (Saves disabled) or the relative
        /// store path from the store URL, e.g. "store://mystore/things/thing" returns
        /// "things/thing"</returns>
        public string Save(SwiffotronContext ctx, string key, byte[] data)
        {
            Uri storeURI = new Uri(key);

            if (storeURI.Scheme != "store") /* ISSUE 67: Constants, please. */
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML,
                        ctx,
                        @"Store paths should begin with store://");
            }

            string storeId = storeURI.Host;

            key = storeURI.AbsolutePath.Substring(1);

            if (!conf.EnableStoreWrites)
            {
                /* Give up, but return the key we would have used. Used in debug
                 * tests. */
                return key;
            }


            if (!stores.ContainsKey(storeId))
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML,
                        ctx,
                        @"Store '" + storeId + @"' not registered.");
            }

            ISwiffotronStore store = stores[storeId];


            using (Stream s = store.OpenOutput(key))
            {
                s.Write(data, 0, data.Length);
            }

            store.Commit(key);

            return key;
        }


        /// <summary>
        /// Opens a stream from a store by its key string.
        /// </summary>
        /// <param name="key">The key, including store prefix</param>
        /// <returns>A stream, or null if not found.</returns>
        public Stream Open(SwiffotronContext ctx, string key)
        {
            Uri storeURI = new Uri(key);

            if (storeURI.Scheme != "store") /* ISSUE 67: Constants, please. */
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML,
                        ctx,
                        @"Store paths should begin with store://");
            }

            string storeId = storeURI.Host;

            if (!stores.ContainsKey(storeId))
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML,
                        ctx,
                        @"Store '" + storeId + @"' not registered.");
            }

            try
            {
                return stores[storeId].OpenInput(storeURI.LocalPath.Substring(1));
            }
            catch (FileNotFoundException fnfe)
            {
                throw new SwiffotronException(
                        SwiffotronError.BadPathOrID,
                        ctx.Sentinel("FileNotFoundInStore"),
                        "File not found: " + key,
                        fnfe);
            }
        }
    }
}
