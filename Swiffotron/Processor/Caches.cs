//-----------------------------------------------------------------------
// Caches.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.Processor
{
    using System.Collections.Generic;
    using SWFProcessing.Swiffotron.IO;

    public class Caches
    {
        /// <summary>A map of cache names to cache implementation instances.</summary>
        private Dictionary<string, ISwiffotronCache> caches;

#if(DEBUG)
        /* We could use MSTest's private access, but we want the express versions
         * to be in on the fun too. Horrible, but there you go. */
        public Dictionary<string, ISwiffotronCache> caches_accessor
        {
            get
            {
                return this.caches;
            }
        }
#endif

        public Caches()
        {
            this.caches = new Dictionary<string, ISwiffotronCache>();
        }

        public void Register(string name, ISwiffotronCache cache)
        {
            this.caches.Add(name, cache);
        }

        /// <summary>
        /// Get some useful information for debug purposes letting us find out how things
        /// are set up. I should list them all here, really.
        /// </summary>
        /// <param name="info">An accumulating big map of arbitrary string->string data
        /// that you can pick apart and use as you so desire.</param>
        public void Interrogate(Dictionary<string, string> info)
        {
            List<string> cacheClasses = new List<string>();
            foreach (KeyValuePair<string, ISwiffotronCache> cacheEntry in this.caches)
            {
                cacheClasses.Add(cacheEntry.Key + "=" + cacheEntry.Value.GetType().FullName + "[" + cacheEntry.Value.InitialisedWith + "]");
            }

            cacheClasses.Sort();

            /* CacheClasses holds a comma-separated list of all the cache classes used by swiffotron
             * as key-value pairs, e.g. membase=com.blah.MyOtherClass,memcache=com.blah.MyClass
             * sorted into alphabetical order. */
            info.Add("CacheClasses", string.Join(",", cacheClasses.ToArray()));
        }

        /// <summary>
        /// Retrieves a cached object given its fully qualified cache key of the form
        /// [cache name]:[cache key]
        /// </summary>
        /// <param name="key">Full cache path</param>
        /// <returns>The cached object, or null.</returns>
        public object Get(SwiffotronContext ctx, string key)
        {
            int pos = key.IndexOf(':');
            if (pos < 0)
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML,
                        ctx,
                        @"Bad cache key (Requires prefix): " + key);
            }

            string cacheId = key.Substring(0, pos);
            key = key.Substring(pos + 1);

            if (!caches.ContainsKey(cacheId))
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML,
                        ctx,
                        @"Cache '" + cacheId + @"' not registered.");
            }

            return this.caches[cacheId].Get(key);
        }

        /// <summary>
        /// Caches an object under a key in a given cache, specified as part of the
        /// cache key. <see cref="GetCacheItem"/>
        /// </summary>
        /// <param name="key">Full cache path</param>
        /// <param name="v">The object to cache</param>
        public void Put(SwiffotronContext ctx, string key, object v)
        {
            int pos = key.IndexOf(':');
            if (pos < 0)
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML,
                        ctx,
                        @"Bad cache key (Requires prefix): " + key);
            }

            string cacheId = key.Substring(0, pos);
            key = key.Substring(pos + 1);

            if (!caches.ContainsKey(cacheId))
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML,
                        ctx,
                        @"Cache '" + cacheId + @"' not registered.");
            }

            this.caches[cacheId].Put(key, v);
        }
    }
}
