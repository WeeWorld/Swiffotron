//-----------------------------------------------------------------------
// AS3ClassDef.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC.Code
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    /// A class definition
    /// </summary>
    public class AS3ClassDef : AS3Class
    {
        public List<Multiname> interfaces;

        private Namespace protectedNS;

        private AbcCode code;

        private List<Trait> classTraits;

        private List<Trait> instanceTraits;

        /// <summary>
        /// Please consider using AbcCode.CreateClass instead, for the benefit
        /// of internal integrity.
        /// </summary>
        internal AS3ClassDef(AbcCode code)
        {
            this.interfaces = new List<Multiname>();
            this.classTraits = new List<Trait>();
            this.instanceTraits = new List<Trait>();

            /* Always handy to have out of place references to objects higher up
             * in the hierarchy. It makes bugs so much more interesting to solve. */
            this.code = code;
        }

        /// <summary>
        /// A delagate declaration for processing traits with TraitProc
        /// </summary>
        /// <param name="t">Each trait in the class.</param>
        /// <param name="abc">The abc code to which the trait belongs.</param>
        public delegate void TraitDelegate(ref Trait t, AbcCode abc);

        /// <summary>
        /// See AS3 spec
        /// </summary>
        enum FlagMask
        {
            SealedMask = 0x01,
            FinalMask = 0x02,
            InterfaceMask = 0x04,
            ProtectedNS = 0x08
        }

        /// <summary>
        /// Gets or sets the class flags. Access flag values through other properties. This value
        /// is the one that can be written to a swf file.
        /// </summary>
        public int Flags { get; set; }

        /// <summary>Gets or sets the superclass name</summary>
        public Multiname Supername { get; set; }

        /// <summary>Gets or sets the optional protected namespace. Set to null
        /// if the class does not have a protected namespace.</summary>
        public Namespace ProtectedNS
        {
            get
            {
                return this.protectedNS;
            }

            set
            {
                this.protectedNS = value;

                if (value == null)
                {
                    this.Flags &= ~(int)FlagMask.ProtectedNS;
                }
                else
                {
                    this.Flags |= (int)FlagMask.ProtectedNS;
                }
            }
        }

        /// <summary>Gets or sets the constructor</summary>
        public Method Iinit { get; set; }

        /// <summary>Gets or sets the static initializer</summary>
        public Method Cinit { get; set; }

        /// <summary>
        /// How many traits do instances of this class have?
        /// </summary>
        public int InstanceTraitCount
        {
            get
            {
                return this.instanceTraits.Count;
            }
        }

        /// <summary>
        /// How many static traits does this class have?
        /// </summary>
        public int ClassTraitCount
        {
            get
            {
                return this.classTraits.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this class is sealed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this class is sealed; otherwise, <c>false</c>.
        /// </value>
        public bool IsSealed
        {
            get
            {
                return (this.Flags & (int)FlagMask.SealedMask) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this class is final.
        /// </summary>
        /// <value>
        /// <c>true</c> if this class is final; otherwise, <c>false</c>.
        /// </value>
        public bool IsFinal
        {
            get
            {
                return (this.Flags & (int)FlagMask.FinalMask) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this class is actually an interface.
        /// </summary>
        /// <value>
        /// <c>true</c> if this is an interface; otherwise, <c>false</c>.
        /// </value>
        public bool IsInterface
        {
            get
            {
                return (this.Flags & (int)FlagMask.InterfaceMask) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this class is in a protected NS.
        /// </summary>
        /// <value>
        /// <c>true</c> if this class is in a protected NS; otherwise, <c>false</c>.
        /// </value>
        public bool IsProtectedNS
        {
            get
            {
                return (this.Flags & (int)FlagMask.ProtectedNS) != 0;
            }
        }

        /// <summary>
        /// Gets the interfaces exposed by this class.
        /// </summary>
        public IEnumerator<Multiname> Interfaces
        {
            get
            {
                return this.interfaces.GetEnumerator();
            }
        }

        /// <summary>
        /// Enumeration of all the instance traits belonging to this class.
        /// </summary>
        public IEnumerator<Trait> InstanceTraits
        {
            get
            {
                return this.instanceTraits.GetEnumerator();
            }
        }

        /// <summary>
        /// Enumeration of all the static traits belonging to this class.
        /// </summary>
        public IEnumerator<Trait> ClassTraits
        {
            get
            {
                return this.classTraits.GetEnumerator();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this class is tampered (Needs assembly).
        /// </summary>
        /// <value>
        /// <c>true</c> if this class is tampered; otherwise, <c>false</c>.
        /// </value>
        public bool IsTampered
        {
            get
            {
                return (this.Iinit.Tampered || this.Cinit.Tampered);
            }
        }

        /// <summary>
        /// How many additional interfaces does this class implement?
        /// </summary>
        public int InterfaceCount
        {
            get
            {
                return this.interfaces.Count;
            }
        }

        /// <summary>
        /// Class as a string is its name, for debugger purposes.
        /// </summary>
        /// <returns>The name of the class.</returns>
        public override string ToString()
        {
            return this.Name.ToString();
        }

        /// <summary>
        /// Unit test method present in the debug build. Renders the object as text
        /// that can be written to a diffable output file.
        /// </summary>
        /// <param name="nest">The current nest level in our output</param>
        /// <param name="sb">The buffer that is accumulating output.</param>
        [Conditional("DEBUG")]
        public void ToStringModelView(int nest, StringBuilder sb)
        {
            string indent = new string(' ', nest * 4);

            if (this.IsProtectedNS)
            {
                sb.AppendLine(indent + "protected package " + this.ProtectedNS);
            }
            else
            {
                sb.AppendLine(indent + "package " + this.Name.NS);
            }

            sb.AppendLine(indent + "{");

            nest++;
            indent = new string(' ', nest * 4);

            if (this.IsInterface)
            {
                sb.AppendLine(indent + (this.IsFinal ? "final " : string.Empty) + (this.IsFinal ? "sealed " : string.Empty) + "interface " + this.Name + " extends " + this.Supername);
            }
            else
            {
                sb.AppendLine(indent + (this.IsFinal ? "final " : string.Empty) + (this.IsFinal ? "sealed " : string.Empty) + "class " + this.Name + " extends " + this.Supername);
            }

            sb.AppendLine(indent + "{");

            this.Cinit.ToStringModelView(nest + 1, sb, "static");

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            this.Iinit.ToStringModelView(nest + 1, sb, this.Name.ToString());

            foreach (Trait t in this.classTraits)
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine();
                t.ToStringModelView(nest + 1, sb, "static");
            }

            foreach (Trait t in this.instanceTraits)
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine();
                t.ToStringModelView(nest + 1, sb, string.Empty);
            }

            sb.AppendLine(indent + "}"); /* class */

            nest--;
            indent = new string(' ', nest * 4);

            sb.AppendLine(indent + "}"); /* package */
        }

        /// <summary>
        /// Add an interface to this class by name
        /// </summary>
        /// <param name="iface">The name of the interface</param>
        public void AddInterface(Multiname iface)
        {
            this.interfaces.Add(iface);
        }

        /// <summary>
        /// Gets a class trait by index.
        /// </summary>
        /// <param name="idx">The index.</param>
        /// <returns>The trait, which must exist</returns>
        public Trait GetClassTrait(int idx)
        {
            return this.classTraits[idx];
        }

        /// <summary>
        /// Gets an instance trait by index.
        /// </summary>
        /// <param name="idx">The index.</param>
        /// <returns>The trait, which must exist</returns>
        public Trait GetInstanceTrait(int idx)
        {
            return this.instanceTraits[idx];
        }

        /// <summary>
        /// Adds a new instance trait
        /// </summary>
        /// <param name="trait">The trait to add</param>
        public void AddInstanceTrait(Trait trait)
        {
            this.instanceTraits.Add(trait);
        }

        /// <summary>
        /// Adds a new static trait
        /// </summary>
        /// <param name="trait">The trait to add</param>
        public void AddClassTrait(Trait trait)
        {
            this.classTraits.Add(trait);
        }

        /// <summary>
        /// Process all the class traits with a delegate function.
        /// </summary>
        /// <param name="td">The delegate to call on each trait.</param>
        public void TraitProc(TraitDelegate td)
        {
            for (int i = 0; i < this.instanceTraits.Count; i++)
            {
                Trait t = this.instanceTraits[i];
                td(ref t, this.code);
                this.instanceTraits[i] = t;
            }

            for (int i = 0; i < this.classTraits.Count; i++)
            {
                Trait t = this.classTraits[i];
                td(ref t, this.code);
                this.classTraits[i] = t;
            }
        }

        /// <summary>
        /// Process all the methods with a delegate function.
        /// </summary>
        /// <param name="td">The delegate to call on each method.</param>
        public void MethodProc(AbcCode.MethodProcessor mp)
        {
            foreach (Trait t in this.classTraits)
            {
                if (t is FunctionalTrait)
                {
                    mp(((FunctionalTrait)t).Fn, this);
                }
            }

            foreach (Trait t in this.instanceTraits)
            {
                if (t is FunctionalTrait)
                {
                    mp(((FunctionalTrait)t).Fn, this);
                }
            }

            if (this.Cinit != null)
            {
                mp(this.Cinit, this);
            }

            if (this.Iinit != null)
            {
                mp(this.Iinit, this);
            }
        }

        /// <summary>
        /// Marks the code as tampered. Basically forces reassembly when it's
        /// saved.
        /// </summary>
        internal void MarkCodeAsTampered()
        {
            this.Iinit.Tampered = true;
            this.Cinit.Tampered = true;
        }

        /// <summary>
        /// Disassembles the initializers.
        /// </summary>
        internal void DisassembleInitializers()
        {
            this.Iinit.Disassemble();
            this.Cinit.Disassemble();
        }
    }
}
