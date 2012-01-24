//-----------------------------------------------------------------------
// ListSet.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Util
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A list of objects with a cheap 'contains' test.
    /// </summary>
    /// <typeparam name="K">The type of things to put in it.</typeparam>
    public class ListSet<K>
    {
        /// <summary>
        /// List view of the data
        /// </summary>
        private LinkedList<K> list;

        /// <summary>
        /// Set view of the data
        /// </summary>
        private HashSet<K> set;

        /// <summary>
        /// Initializes a new instance of the ListSet class
        /// </summary>
        public ListSet()
        {
            this.list = new LinkedList<K>();
            this.set = new HashSet<K>();
        }

        /// <summary>
        /// Gets the number of items in this collection
        /// </summary>
        public int Count
        {
            get
            {
                return this.list.Count;
            }
        }

        /// <summary>
        /// Creates an enumerator over the data
        /// </summary>
        /// <returns>An enumerator</returns>
        public IEnumerator<K> GetEnumerator()
        {
            return this.list.GetEnumerator();
        }

        /// <summary>
        /// Creates an enumerable view of the data
        /// </summary>
        /// <returns>An enumerable</returns>
        public IEnumerable<K> AsEnumerable()
        {
            return this.list.AsEnumerable();
        }

        /// <summary>
        /// Add a new item to the collection
        /// </summary>
        /// <param name="ob">The item to add</param>
        public void Add(K ob)
        {
            this.list.AddLast(ob);
            this.set.Add(ob);
        }

        public void AddIfNotAlredy(K ob)
        {
            if (this.set.Contains(ob))
            {
                return;
            }

            this.Add(ob);
        }

        /// <summary>
        /// Test whether the collection contains an item
        /// </summary>
        /// <param name="ob">The item to test</param>
        /// <returns>True if it contains the item.</returns>
        public bool Contains(K ob)
        {
            return this.set.Contains(ob);
        }

        /// <summary>
        /// Purge all items from the collection.
        /// </summary>
        public void Clear()
        {
            this.list.Clear();
            this.set.Clear();
        }

        internal K[] ToArray()
        {
            return this.list.ToArray();
        }
    }
}

