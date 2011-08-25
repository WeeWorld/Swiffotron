//-----------------------------------------------------------------------
// Namespace.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC
{
    using System;

    /// <summary>
    /// A namespace that functions operate within
    /// </summary>
    public class Namespace : IComparable
    {
        public static Namespace GlobalNS;

        static Namespace()
        {
            Namespace.GlobalNS = new Namespace(NamespaceKind.Ns, "*");
        }

        /// <summary>
        /// Immutable namespace object. Please consider using AbcCode.CreateNamespace, which will
        /// keep things in their right place.
        /// </summary>
        /// <param name="kind">The kind of namespace</param>
        /// <param name="name">The namespace name</param>
        internal Namespace(NamespaceKind kind, string name) : this(kind, name, null)
        {
        }

        /// <summary>
        /// Immutable namespace object. Please consider using AbcCode.CreateNamespace, which will
        /// keep things in their right place.
        /// </summary>
        /// <param name="kind">The kind of namespace</param>
        /// <param name="name">The namespace name. If this contains a prefix, it will be stripped off,
        /// e.g. myfile_fla:MainTimeline will become MainTimeline</param>
        /// <param name="prefix">A new prefix for the name</param>
        internal Namespace(NamespaceKind kind, string name, string prefix)
        {
            this.Kind = kind;
            if (prefix == null)
            {
                this.Name = name;
            }
            else
            {
                int pos = name.IndexOf(':');
                if (pos >= 0)
                {
                    name = name.Substring(pos);
                }
                this.Name = prefix + name;
            }
        }

        /// <summary>
        /// Different kinds of namespace found in ABC bytecode.
        /// </summary>
        public enum NamespaceKind
        {
            /// <summary>A simple namespace</summary>
            Ns = 0x08,

            /// <summary>Package scope</summary>
            Package = 0x16,

            /// <summary>Internal package scope</summary>
            PackageInternal = 0x17,

            /// <summary>Protected scope</summary>
            Protected = 0x18,

            /// <summary>Explicit namespace</summary>
            Explicit = 0x19,

            /// <summary>Static protected scope</summary>
            StaticProtected = 0x1A,

            /// <summary>Private namespace</summary>
            Private = 0x05
        }

        /// <summary>The kind of namespace</summary>
        public NamespaceKind Kind { get; private set; }

        /// <summary>The namespace name</summary>
        public string Name { get; private set; }

        public string NamePostfix
        {
            get
            {
                int pos = this.Name.IndexOf(':');

                if (pos < 0)
                {
                    return this.Name;
                }

                return this.Name.Substring(pos + 1);
            }
        }

        public string NamePrefix
        {
            get
            {
                int pos = this.Name.IndexOf(':');

                if (pos < 0)
                {
                    return this.Name;
                }

                return this.Name.Substring(0, pos);
            }
        }

        /// <summary>
        /// If you change this method, please remember to change operator== too.
        /// </summary>
        /// <param name="obj">Other NS for comparison</param>
        /// <returns>true if equals</returns>
        public override bool Equals(object obj)
        {
            if (System.Object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is Namespace))
            {
                return false;
            }

            Namespace ns = (Namespace)obj;
            return this.Kind == ns.Kind && this.Name == ns.Name;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                /* See http://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode/263416#263416 */
                int hash = 17; /* Prime numbers automatically make code look impressive. */
                hash = hash * 23 + (this.Name ?? string.Empty).GetHashCode();
                hash = hash * 23 + this.Kind.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Strictly speaking, == is for reference equality. Namespaces though, are
        /// meant to be unique within the code, so we're quite justified in overriding
        /// this for convenience.
        /// </summary>
        /// <param name="ns1">Left hand side</param>
        /// <param name="ns2">Right hand side</param>
        /// <returns>True if value-equal</returns>
        public static bool operator ==(Namespace ns1, Namespace ns2)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(ns1, ns2))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (System.Object.ReferenceEquals(ns1, null) || System.Object.ReferenceEquals(ns2, null))
            {
                return false;
            }

            // Return true if the fields match:
            return ns1.Kind == ns2.Kind && ns1.Name == ns2.Name;
        }

        public static bool operator !=(Namespace ns1, Namespace ns2)
        {
            return !(ns1 == ns2);
        }

        public int CompareTo(object obj)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(obj, this))
            {
                return 0;
            }

            if (obj == null)
            {
                return int.MaxValue;
            }

            Namespace ns = (Namespace)obj;

            int dkind = this.Kind.CompareTo(ns.Kind);
            if (dkind != 0)
            {
                return dkind;
            }

            return this.Name.CompareTo(ns.Name);
        }

        /// <summary>
        /// Renders the namespace as a string. Used only in test/debug console output.
        /// </summary>
        /// <returns>A string rendition of this namespace.</returns>
        public override string ToString()
        {
            return "ns " + this.Kind.ToString() + " \"" + this.Name + "\"";
        }

        internal Namespace CreateRepackaged(string newPackage)
        {
            int colon = this.Name.IndexOf(':');
            if (colon < 0)
            {
                return new Namespace(this.Kind, newPackage);
            }
            return new Namespace(this.Kind, newPackage + this.Name.Substring(colon));
        }
    }
}
