//-----------------------------------------------------------------------
// IDMarshaller.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.IO
{
    using System.Collections.Generic;
    using SWFProcessing.SWFModeller.Util;

    /// <summary>
    /// A class that assigns IDs to objects as we write them to the
    /// output stream.
    /// </summary>
    /// <typeparam name="V">The type of object we're marshalling.</typeparam>
    internal class IDMarshaller<V>
    {
        private Dictionary<V, int> ids;
        private List<V> list;
        private int baseValue;

        /// <summary>
        /// Initializes a new instance of an IDMarshaller
        /// </summary>
        /// <param name="baseValue">This is the base value of the integers. Normally
        /// you'd pick 0 or 1, i.e. do the IDs start at 0, or 1?</param>
        public IDMarshaller(int baseValue, params V[] init)
        {
            this.baseValue = baseValue;
            this.ids = new Dictionary<V, int>();
            this.list = new List<V>();

            foreach (V item in init)
            {
                /* Pre-populated value... */
                this.Register(item);
            }
        }

        /// <summary>
        /// Gets an ID for an object.
        /// </summary>
        /// <param name="val">The object to marshal</param>
        /// <returns>An ID unique to the object.</returns>
        public int GetIDFor(V val)
        {
            if (this.ids.ContainsKey(val))
            {
                return this.ids[val];
            }
            int id = this.ids.Count + this.baseValue;
            this.ids.Add(val, id);
            this.list.Add(val);
            return id;
        }

        /// <summary>
        /// For when you want to add a bunch of stuff and check the IDs later, use
        /// this instead of GetIDFor.
        /// </summary>
        /// <param name="val">The value to register. If you pass null, this will
        /// silently return.</param>
        public void Register(V val)
        {
            if (val == null)
            {
                return;
            }

            if (this.ids.ContainsKey(val))
            {
                return;
            }

            this.ids.Add(val, this.ids.Count + this.baseValue);
            this.list.Add(val);
        }

        public V GetItemFromID(int id)
        {
            return this.list[id];
        }

        /// <summary>
        /// This is just like GetIDFor in that it returns the ID for
        /// a value, but this will only get IDs for values it has already
        /// seen. Unregistered values will throw an error.
        /// </summary>
        /// <param name="val">The object to find the ID for.</param>
        /// <returns>An ID.</returns>
        internal int GetExistingIDFor(V val)
        {
            if (!this.ids.ContainsKey(val))
            {
                throw new SWFModellerException(
                        SWFModellerError.Internal,
                        "Object not registered in marshaller.");
            }

            return this.ids[val];
        }

        internal bool HasMarshalled(V val)
        {
            return this.ids.ContainsKey(val);
        }

        internal V[] ToArray()
        {
            return this.list.ToArray();
        }
    }
}
