//-----------------------------------------------------------------------
// ValueSetDict.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Util
{
    using System.Collections.Generic;

    /// <summary>
    /// A dictionary class that lets you determine wether it contains a value
    /// without the cost of a linear search.
    /// </summary>
    /// <typeparam name="K">Type of key</typeparam>
    /// <typeparam name="V">Type of value</typeparam>
    class ValueSetDict<K, V>
    {
        private HashSet<V> stored = new HashSet<V>();
        private Dictionary<K, V> map = new Dictionary<K, V>();

        public V this[K key]
        {
            get
            {
                return this.map[key];
            }
            set
            {
                this.stored.Add(value);
                this.map.Add(key, value);
            }
        }

        public bool ContainsValue(V val)
        {
            return this.stored.Contains(val);
        }
    }
}
