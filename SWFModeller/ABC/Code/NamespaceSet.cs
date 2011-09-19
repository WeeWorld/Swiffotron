//-----------------------------------------------------------------------
// NamespaceSet.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// A set of namespaces
    /// </summary>
    public class NamespaceSet : IEnumerable
    {
        public static NamespaceSet EmptySet;

        /// <summary>
        /// The array of namespaces in the set.
        /// </summary>
        private Namespace[] NSSet;

        /// <summary>
        /// Since this set is immutable, we can cache a hashset of the
        /// namespaces in our equality test, for speed.
        /// </summary>
        private HashSet<Namespace> NSSetEqualityCache = null;

        static NamespaceSet()
        {
            NamespaceSet.EmptySet = new NamespaceSet(new Namespace[0]);
        }

        /// <summary>
        /// Immutable set of namespaces. Please consider using AbcCode.CreateNamespaceSet
        /// instead for the benefit of internal integrity.
        /// </summary>
        /// <param name="set"></param>
        internal NamespaceSet(Namespace[] set)
        {
            this.NSSet = set;
        }

        public int Count { get { return this.NSSet.Length; } }

        /// <summary>
        /// Compares two sets for equality. Order does not matter.
        /// </summary>
        /// <param name="obj">Other set for comparison</param>
        /// <returns>true if equals</returns>
        public override bool Equals(object obj)
        {
            if (System.Object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is NamespaceSet))
            {
                return false;
            }

            NamespaceSet nss = (NamespaceSet)obj;

            if (this.NSSet.Length != nss.NSSet.Length)
            {
                return false;
            }

            if (this.NSSet.Length == 0)
            {
                return true;
            }

            if (this.NSSetEqualityCache == null)
            {
                this.NSSetEqualityCache = new HashSet<Namespace>(this.NSSet);
            }

            /* This cunning device only works if you can guarantee that there are no
             * duplicates in the set. For our purposes though, if there are duplicates, and this
             * still returns true, then we can treat the sets as equivalent, so we don't care.
             */
            return this.NSSetEqualityCache.SetEquals(nss.NSSet);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                /* See http://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode/263416#263416 */
                int hash = 17;
                foreach (Namespace ns in this.NSSet)
                {
                    hash = hash * 23 + ns.GetHashCode();
                }
                return hash;
            }
        }

        /// <summary>
        /// Strictly speaking, == is for reference equality. Namespace sets though, are
        /// meant to be unique within the code, so we're quite justified in overriding
        /// this for convenience.
        /// </summary>
        /// <param name="nss1">Left hand side</param>
        /// <param name="nss2">Right hand side</param>
        /// <returns>True if value-equal</returns>
        public static bool operator ==(NamespaceSet nss1, NamespaceSet nss2)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(nss1, nss2))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (System.Object.ReferenceEquals(nss1, null) || System.Object.ReferenceEquals(nss2, null))
            {
                return false;
            }

            // Return true if the fields match:
            return nss1.Equals(nss2);
        }

        public static bool operator !=(NamespaceSet nss1, NamespaceSet nss2)
        {
            return !(nss1 == nss2);
        }


        /// <summary>
        /// Renders the set as a string. Used only in test/debug console output.
        /// </summary>
        /// <returns>A string rendition of this set.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (this.NSSet != null)
            {
                foreach (Namespace ns in this.NSSet)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(ns.ToString());
                }
            }

            sb.Append("}");

            return "set {" + sb.ToString();
        }

        public Namespace GetAt(int idx)
        {
            /* ISSUE 17: Wouldn't an index accessor be sexier? */
            return this.NSSet[idx];
        }

        public IEnumerator GetEnumerator()
        {
            return this.NSSet.GetEnumerator();
        }
    }
}
