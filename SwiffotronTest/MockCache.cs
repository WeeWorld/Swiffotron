//-----------------------------------------------------------------------
// MockCache.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.Test
{
    using System.Collections.Generic;
    using SWFProcessing.Swiffotron.IO;

    /// <summary>
    /// A mock cache object for unit test classes to use which has the
    /// potential to snitch on strange cache behaviour.
    /// </summary>
    public class MockCache : ISwiffotronCache
    {
        private string initString;

        private Dictionary<string, object> cache;

        private List<string> log;

        public string[] Log
        {
            get
            {
                return this.log.ToArray();
            }
        }

        /// <summary>
        /// The config must pass the string 'mock' for this mock
        /// object to be satisfied that it's initialised properly.
        /// </summary>
        public bool InitialisedProperly
        {
            get { return this.initString == @"mock"; }
        }

        public void Initialise(string init)
        {
            this.initString = init;
            this.cache = new Dictionary<string, object>();
            this.log = new List<string>();
        }

        /// <inheritdoc />
        public bool Has(string key)
        {
            return this.cache.ContainsKey(key);
        }

        /// <inheritdoc />
        public void Put(string key, object value)
        {
            this.cache[key] = value;
            this.log.Add("put " + key);
        }

        /// <inheritdoc />
        public object Get(string key)
        {
            if (!this.cache.ContainsKey(key))
            {
                this.log.Add("miss " + key);
                return null;
            }
            this.log.Add("hit " + key);
            return this.cache[key];
        }
    }
}
