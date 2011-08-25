//-----------------------------------------------------------------------
// Multiname.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC
{
    /// <summary>
    /// A multiname, for pinpointing something within all loaded code.
    /// </summary>
    public class Multiname
    {
        public static Multiname GlobalMultiname;

        static Multiname()
        {
            Multiname.GlobalMultiname = new Multiname(MultinameKind.Multiname, "*", null, NamespaceSet.EmptySet);
        }

        /// <summary>
        /// Immutable multiname object.
        /// </summary>
        /// <param name="kind">The kind of multiname</param>
        /// <param name="name">The multiname name</param>
        /// <param name="ns">Optional namespace, dependant on kind. See AVM2 spec</param>
        /// <param name="set">Optional namespace set, dependant on kind. See AVM2 spec</param>
        internal Multiname(MultinameKind kind, string name, Namespace ns, NamespaceSet set)
        {
            this.Kind = kind;
            this.Name = name;
            this.NS = ns;
            this.Set = set;

            /* Sanity checking... */

            switch (kind)
            {
                case MultinameKind.QName:
                case MultinameKind.QNameA:
                    if (ns == null)
                    {
                        throw new SWFModellerException(
                                SWFModellerError.Internal,
                                "A " + kind.ToString() + " requires a namespace");
                    }
                    break;

                case MultinameKind.RTQName:
                case MultinameKind.RTQNameA:
                    break;

                case MultinameKind.RTQNameL:
                case MultinameKind.RTQNameLA:
                    break;

                case MultinameKind.Multiname:
                case MultinameKind.MultinameA:
                    if (set == null)
                    {
                        throw new SWFModellerException(
                                SWFModellerError.Internal,
                                "A " + kind.ToString() + " requires a namespace set");
                    }
                    break;

                case MultinameKind.MultinameL:
                case MultinameKind.MultinameLA:
                    if (set == null)
                    {
                        throw new SWFModellerException(
                                SWFModellerError.Internal,
                                "A " + kind.ToString() + " requires a namespace set");
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Different kinds of multinames
        /// </summary>
        public enum MultinameKind
        {
            /// <summary>Qualified name</summary>
            QName = 0x07,

            /// <summary>Qualified attribute name</summary>
            QNameA = 0x0D,

            /// <summary>Runtime qualified name</summary>
            RTQName = 0x0f,

            /// <summary>Runtime qualified attribute name</summary>
            RTQNameA = 0x10,

            /// <summary>Runtime qualified late-bound name</summary>
            RTQNameL = 0x11,

            /// <summary>Runtime qualified late-bound attribute name</summary>
            RTQNameLA = 0x12,

            /// <summary>A multiname</summary>
            Multiname = 0x09,

            /// <summary>Attribute multiname</summary>
            MultinameA = 0x0E,

            /// <summary>Late-bound multiname</summary>
            MultinameL = 0x1B,

            /// <summary>Late-bound attribute multiname</summary>
            MultinameLA = 0x1C
        }

        /// <summary>The kind of multiname</summary>
        public MultinameKind Kind { get; private set; }

        /// <summary>The multiname name</summary>
        public string Name { get; private set; }

        /// <summary>Optional namespace</summary>
        public Namespace NS { get; private set; }

        /// <summary>Optional namespace set</summary>
        public NamespaceSet Set { get; private set; }

        public string QualifiedName
        {
            get
            {
                if (this.NS == null || this.NS.Name == string.Empty)
                {
                    return this.Name;
                }
                else
                {
                    return this.NS.NamePrefix + "." + this.Name;
                }
            }
        }

        public bool IsEmptySet
        {
            get
            {
                return Set == null || Set.Count == 0;
            }
        }

        /// <summary>
        /// Strictly speaking, == is for reference equality. Multinames though, are
        /// meant to be unique within the code, so we're quite justified in overriding
        /// this for convenience.
        /// </summary>
        /// <param name="mn1">Left hand side</param>
        /// <param name="mn2">Right hand side</param>
        /// <returns>True if value-equal</returns>
        public static bool operator ==(Multiname mn1, Multiname mn2)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(mn1, mn2))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (System.Object.ReferenceEquals(mn1, null) || System.Object.ReferenceEquals(mn2, null))
            {
                return false;
            }

            // Return true if the fields match:
            return mn1.Equals(mn2);
        }

        public static bool operator !=(Multiname mn1, Multiname mn2)
        {
            return !(mn1 == mn2);
        }

        /// <summary>
        /// If you change this method, please remember to change operator== too.
        /// </summary>
        /// <param name="obj">Other MN for comparison</param>
        /// <returns>true if equals</returns>
        public override bool Equals(object obj)
        {
            if (System.Object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is Multiname))
            {
                return false;
            }

            Multiname mn = (Multiname)obj;
            return this.Kind == mn.Kind && this.Name == mn.Name && this.NS == mn.NS && this.Set == mn.Set;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                /* See http://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode/263416#263416 */
                int hash = 17;
                hash = hash * 23 + (this.Name ?? string.Empty).GetHashCode();
                hash = hash * 23 + this.Kind.GetHashCode();

                if (this.NS != null)
                {
                    hash = hash * 23 + this.NS.GetHashCode();
                }

                if (this.Set != null)
                {
                    hash = hash * 23 + this.Set.GetHashCode();
                }

                return hash;
            }
        }

        /// <summary>
        /// Renders the multiname as a string. Used only in test/debug console output.
        /// </summary>
        /// <returns>A string rendition of this multiname.</returns>
        public override string ToString()
        {
            /* TODO: If we catch all the cases above properly, remove this fallback block. */
            string theName = this.Name;
            string theNs = (this.NS == null) ? "ns *" : this.NS.ToString();
            string theSet = (this.Set == null) ? "set *" : this.Set.ToString();
            return "mn " + this.Kind.ToString() + " \"" + theName + "\"; " + theNs + "; " + theSet;
        }
    }
}
