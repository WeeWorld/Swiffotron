//-----------------------------------------------------------------------
// AbcCode.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    using SWFProcessing.SWFModeller.ABC.Code;
    using SWFProcessing.SWFModeller.Characters;

    /// <summary>
    /// Different kinds of constants
    /// </summary>
    public enum ConstantKind
    {
        /// <summary>An integer</summary>
        ConInt = 0x03,

        /// <summary>An unsigned integer</summary>
        ConUInt = 0x04,

        /// <summary>A double floating point</summary>
        ConDouble = 0x06,

        /// <summary>A string value</summary>
        ConUtf8 = 0x01,

        /// <summary>The boolean value true</summary>
        ConTrue = 0x0B,

        /// <summary>The boolean value false</summary>
        ConFalse = 0x0A,

        /// <summary>The null value</summary>
        ConNull = 0x0C,

        /// <summary>The undefined value</summary>
        ConUndefined = 0x00,

        /// <summary>A namespace</summary>
        ConNamespace = 0x08,

        /// <summary>A package namespace</summary>
        ConPackageNamespace = 0x16,

        /// <summary>An internal package</summary>
        ConPackageInternalNs = 0x17,

        /// <summary>A protected namespace</summary>
        ConProtectedNamespace = 0x18,

        /// <summary>An explicit namespace</summary>
        ConExplicitNamespace = 0x19,

        /// <summary>A static namespace</summary>
        ConStaticProtectedNs = 0x1A,

        /// <summary>A private namespace</summary>
        ConPrivateNs = 0x05
    }

    /// <summary>
    /// Represents parsed ABC bytecode, and all that it contains.
    /// </summary>
    public class AbcCode
    {
        /// <summary>The metadata associated with this code.</summary>
        private Dictionary<string, Dictionary<string, string>> metadata = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>The class definitions in this code.</summary>
        private List<AS3ClassDef> classes;

        /// <summary>The script definitions in this code. Basically the bits that get run first and
        /// set things up.</summary>
        private List<Script> scripts;

        /// <summary>Namespace constants</summary>
        private List<Namespace> nsConsts;

        /// <summary>Namespace set constants</summary>
        private List<NamespaceSet> nsSetConsts;

        /// <summary>Gets or sets the table of qualified multinames</summary>
        private List<Multiname> multinameConsts;

        private List<Method> methods;

        /* ISSUE 3: Turn the array members into list members for consistency with the
         * other tables. */

        /// <summary>
        /// Initializes a new instance of the AbcCode class.
        /// </summary>
        public AbcCode()
        {
            this.IntConsts = null;
            this.UIntConsts = null;
            this.StringConsts = null;
            this.DoubleConsts = null;

            this.scripts = new List<Script>();
            this.classes = new List<AS3ClassDef>();
            this.methods = new List<Method>();

            this.nsConsts = new List<Namespace>();
            this.nsConsts.Add(Namespace.GlobalNS);

            this.multinameConsts = new List<Multiname>();
            this.multinameConsts.Add(Multiname.GlobalMultiname);

            this.nsSetConsts = new List<NamespaceSet>();
            this.nsSetConsts.Add(NamespaceSet.EmptySet);
        }

        /// <summary>
        /// A delegate to be passed to the MethodProc method.
        /// </summary>
        /// <param name="m">Callback parameter that recieves each method in turn.</param>
        /// <param name="c">Callback parameter the class for the method, or null if it
        /// doesn't belong to a class.</param>
        public delegate void MethodProcessor(Method m, AS3ClassDef c);

        /// <summary>Gets or sets the constant table for signed integers</summary>
        public int[] IntConsts { get; set; }

        /// <summary>Gets or sets the constant table for unsigned integers</summary>
        public uint[] UIntConsts { get; set; }

        /// <summary>Gets or sets the constant table for strings</summary>
        public string[] StringConsts { get; set; }

        /// <summary>Gets or sets the constant table for 64-bit floats.
        /// This is a sorta hack. We don't store doubles, but we do
        /// store the bits for the 64-bit double data in an ulong. We do this
        /// to avoid the cost and effort of casting it to a double because the
        /// values are actually meaningless to us anyway. We're only ever
        /// interested in the binary representation in the SWF.</summary>
        public ulong[] DoubleConsts { get; set; }

        /// <summary>Gets the number of classes in this code.</summary>
        public int ClassCount
        {
            get
            {
                return this.classes.Count;
            }
        }

        /* ISSUE 4: On a personal note, there's a horrid mix of patterns in this project for
         * exposing iterable data from classes. Here we have a list exposed as an IEnumerable,
         * which seems reasonable, but will permit data alteration if the caller is canny enough
         * to cast it back into a List. We could expose it as an IEnumerator instead, but this
         * forgoes foreach syntax and makes us use overly verbose and inelegant using() blocks
         * wrapped around an iterator. We could expose a read-only form of the list with a call
         * to Select(o =>o) but that seems like an expensive kludge for the sake of some
         * syntactic convenience. C# needs to make this sort of thing easier, really. */

        /// <summary>Gets an enumerable form of the classes</summary>
        public IEnumerable<AS3ClassDef> Classes
        {
            get
            {
                return this.classes;
            }
        }

        /// <summary>Gets an enumerable form of the metadata keys</summary>
        public IEnumerable<string> MetadataKeys
        {
            get
            {
                return this.metadata.Keys;
            }
        }

        /// <summary>Gets a count of the number of metadata items there are.</summary>
        public int MetadataCount
        {
            get
            {
                return this.metadata.Count;
            }
        }

        /// <summary>Gets a count of the number of scripts there are.</summary>
        public int ScriptCount
        {
            get
            {
                return this.scripts.Count;
            }
        }

        /// <summary>Gets an enumerable form of the scripts</summary>
        public IEnumerable<Script> Scripts
        {
            get
            {
                return this.scripts;
            }
        }

        /// <summary>Gets a count of the number of methods there are.</summary>
        public int MethodCount
        {
            get
            {
                return this.methods.Count;
            }
        }

        /// <summary>Gets an enumerable form of the method list</summary>
        public IEnumerable<Method> Methods
        {
            get
            {
                return this.methods;
            }
        }

        /// <summary>Gets a count of the number of sets there are.</summary>
        public int NamespaceSetCount
        {
            get
            {
                return this.nsSetConsts.Count;
            }
        }

        /// <summary>Gets an enumerable form of the set list</summary>
        public IEnumerable<NamespaceSet> NamespaceSets
        {
            get
            {
                return this.nsSetConsts;
            }
        }

        /// <summary>Gets a count of the number of namespaces there are.</summary>
        public int NamespaceCount
        {
            get
            {
                return this.nsConsts.Count;
            }
        }

        /// <summary>Gets an enumerable form of the namespace list</summary>
        public IEnumerable<Namespace> Namespaces
        {
            get
            {
                return this.nsConsts;
            }
        }
        /// <summary>Gets a count of the number of multinames there are.</summary>
        public int MultinameCount
        {
            get
            {
                return this.multinameConsts.Count;
            }
        }

        /// <summary>Gets an enumerable form of the multiname list</summary>
        public IEnumerable<Multiname> Multinames
        {
            get
            {
                return this.multinameConsts;
            }
        }

        /// <summary>
        /// Adds a class to the code.
        /// </summary>
        /// <param name="c">The class to add.</param>
        public void AddClass(AS3ClassDef c)
        {
            this.classes.Add(c);
        }

        /// <summary>
        /// Adds a multiname to the code.
        /// </summary>
        /// <param name="mn">The multiname to add.</param>
        public void AddMultiname(Multiname mn)
        {
            this.multinameConsts.Add(mn);
        }

        /// <summary>
        /// Adds a set to the code.
        /// </summary>
        /// <param name="set">The set to add.</param>
        public void AddNamespaceSet(NamespaceSet set)
        {
            this.nsSetConsts.Add(set);
        }

        /// <summary>
        /// Gets a class from a class index.
        /// </summary>
        /// <param name="idx">The index of the class, usually found as a parameter
        /// on some ABC opcode.</param>
        /// <returns>The class at the specified index</returns>
        public AS3ClassDef GetClass(int idx)
        {
            return this.classes[idx];
        }

        /// <summary>
        /// Gets a set from a set index.
        /// </summary>
        /// <param name="idx">The index of the set, usually found as a parameter
        /// on some ABC opcode.</param>
        /// <returns>The set at the specified index</returns>
        public NamespaceSet GetNamespaceSet(int idx)
        {
            return this.nsSetConsts[idx];
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
            sb.AppendLine(indent + "Constants:");

            for (int i = 0; i < this.IntConsts.Length; i++)
            {
                sb.AppendLine(indent + "int #" + i + "\t" + this.IntConsts[i]);
            }

            for (int i = 0; i < this.UIntConsts.Length; i++)
            {
                sb.AppendLine(indent + "uint #" + i + "\t" + this.UIntConsts[i]);
            }

            for (int i = 0; i < this.DoubleConsts.Length; i++)
            {
                sb.AppendLine(indent + "double #" + i + "\t" + BitConverter.Int64BitsToDouble((long)this.DoubleConsts[i]));
            }

            for (int i = 0; i < this.StringConsts.Length; i++)
            {
                sb.AppendLine(indent + "string #" + i + "\t\"" + this.StringConsts[i] + "\"");
            }

            for (int i = 0; i < this.nsConsts.Count; i++)
            {
                sb.AppendLine(indent + "ns #" + i + "\t" + this.nsConsts[i]);
            }

            for (int i = 0; i < this.nsSetConsts.Count; i++)
            {
                sb.AppendLine(indent + "ns set #" + i + "\t(" + this.nsSetConsts[i].Count + " items)");
                foreach (Namespace ns in this.nsSetConsts[i])
                {
                    sb.AppendLine(indent + "    - " + ns);
                }
            }

            for (int i = 0; i < this.multinameConsts.Count; i++)
            {
                Multiname mn = this.multinameConsts[i];
                sb.AppendLine(indent + "multiname #" + i + "\t" + mn);
            }

            sb.AppendLine(indent + "Metadata:");

            foreach (string group in this.metadata.Keys)
            {
                Dictionary<string, string> dict = this.metadata[group];
                foreach (string k in dict.Keys)
                {
                    sb.AppendLine(group + "." + k + " = \"" + dict[k] + "\"");
                }
            }

            sb.AppendLine(indent + "End of metadata.");

            foreach (AS3ClassDef c in this.Classes)
            {
                c.ToStringModelView(nest, sb);
            }

            foreach (Script s in this.Scripts)
            {
                sb.AppendLine(indent + "Script:");
                sb.AppendLine(indent + "{");
                s.ToStringModelView(nest + 1, sb);
                sb.AppendLine(indent + "}");
            }
        }

        /// <summary>
        /// Add some metadata to this bytecode block.
        /// </summary>
        /// <param name="group">Data group name</param>
        /// <param name="key">Data key name</param>
        /// <param name="value">The data value</param>
        public void AddMetadata(string group, string key, string value)
        {
            Dictionary<string, string> dict = null;

            if (this.metadata.ContainsKey(group))
            {
                dict = this.metadata[group];
            }
            else
            {
                dict = new Dictionary<string, string>();
                this.metadata.Add(group, dict);
            }

            dict[key] = value;
        }

        /// <summary>
        /// Run code on all methods in this code
        /// </summary>
        /// <param name="mp">A delegate to run on each method.</param>
        public void MethodProc(MethodProcessor mp)
        {
            foreach (AS3ClassDef c in this.classes)
            {
                c.MethodProc(mp);
            }
        }

        /// <summary>
        /// Generates a script that binds a class to a clip.
        /// </summary>
        /// <param name="spr">The sprite to create the class for.</param>
        public void GenerateClipClassBindingScript(Sprite spr)
        {
            Namespace nsEmptyPackage = this.CreateNamespace(Namespace.NamespaceKind.Package, string.Empty);
            Namespace nsFlashEvents = this.CreateNamespace(Namespace.NamespaceKind.Package, "flash.events");
            Namespace nsFlashDisplay = this.CreateNamespace(Namespace.NamespaceKind.Package, "flash.display");

            Multiname mnObject = this.CreateMultiname(Multiname.MultinameKind.QName, "Object", nsEmptyPackage, null);
            Multiname mnEventDispatcher = this.CreateMultiname(Multiname.MultinameKind.QName, "EventDispatcher", nsFlashEvents, null);
            Multiname mnDisplayObject = this.CreateMultiname(Multiname.MultinameKind.QName, "DisplayObject", nsFlashDisplay, null);
            Multiname mnInteractiveObject = this.CreateMultiname(Multiname.MultinameKind.QName, "InteractiveObject", nsFlashDisplay, null);
            Multiname mnDisplayObjectContainer = this.CreateMultiname(Multiname.MultinameKind.QName, "DisplayObjectContainer", nsFlashDisplay, null);
            Multiname mnSprite = this.CreateMultiname(Multiname.MultinameKind.QName, "Sprite", nsFlashDisplay, null);
            Multiname mnMovieClip = this.CreateMultiname(Multiname.MultinameKind.QName, "MovieClip", nsFlashDisplay, null);

            Multiname sprQName = null;
            Multiname sprMultiname = null;

            if (spr.Class.Name.Kind == Multiname.MultinameKind.Multiname)
            {
                sprMultiname = spr.Class.Name;
                /* ISSUE 5: Convert a multiname to a QName of the form:
                 * mn QName "MyClassName"; ns Package "com.mypackage"; set *
                 */
                throw new SWFModellerException(
                        SWFModellerError.UnimplementedFeature,
                        "Unsupported sprite class name kind in class binding script generation: " + spr.Class.Name.Kind.ToString());
            }
            else if (spr.Class.Name.Kind == Multiname.MultinameKind.QName)
            {
                sprQName = spr.Class.Name;
                /* Convert to form:
                 * mn Multiname "MyClassName"; ns *; set {ns Package "com.mypackage"}
                 */
                sprMultiname = this.CreateMultiname(
                    Multiname.MultinameKind.Multiname,
                    sprQName.Name,
                    nsEmptyPackage,
                    this.CreateNamespaceSet(new Namespace[] { sprQName.NS }));
            }
            else
            {
                /* ISSUE 73 */
                throw new SWFModellerException(
                        SWFModellerError.UnimplementedFeature,
                        "Unsupported sprite class name kind in class binding script generation: " + spr.Class.Name.Kind.ToString());
            }

            Method bindingMethod = this.CreateMethod(spr.Class.Name.Name + "BindingScript.abc", 2, 1, 1, 9,

                /* The above magic numbers come from the numbers generated by IDE versions of this function.
                 * I have no real ideal about how I'd work them out for myself, which would obviously be
                 * more ideal. */

                /* Line */
                /*  1 */ this.Op(Opcode.Mnemonics.GetLocal0),
                /*  2 */ this.Op(Opcode.Mnemonics.PushScope),
                /*  3 */ this.Op(Opcode.Mnemonics.FindPropStrict, sprMultiname),
                /*  4 */ this.Op(Opcode.Mnemonics.GetLex, mnObject),
                /*  5 */ this.Op(Opcode.Mnemonics.PushScope),
                /*  6 */ this.Op(Opcode.Mnemonics.GetLex, mnEventDispatcher),
                /*  7 */ this.Op(Opcode.Mnemonics.PushScope),
                /*  8 */ this.Op(Opcode.Mnemonics.GetLex, mnDisplayObject),
                /*  9 */ this.Op(Opcode.Mnemonics.PushScope),
                /* 10 */ this.Op(Opcode.Mnemonics.GetLex, mnInteractiveObject),
                /* 11 */ this.Op(Opcode.Mnemonics.PushScope),
                /* 12 */ this.Op(Opcode.Mnemonics.GetLex, mnDisplayObjectContainer),
                /* 13 */ this.Op(Opcode.Mnemonics.PushScope),
                /* 14 */ this.Op(Opcode.Mnemonics.GetLex, mnSprite),
                /* 15 */ this.Op(Opcode.Mnemonics.PushScope),
                /* 16 */ this.Op(Opcode.Mnemonics.GetLex, mnMovieClip),
                /* 17 */ this.Op(Opcode.Mnemonics.PushScope),
                /* 18 */ this.Op(Opcode.Mnemonics.GetLex, mnMovieClip),
                /* 19 */ this.Op(Opcode.Mnemonics.NewClass, spr.Class),
                /* 20 */ this.Op(Opcode.Mnemonics.PopScope),
                /* 21 */ this.Op(Opcode.Mnemonics.PopScope),
                /* 22 */ this.Op(Opcode.Mnemonics.PopScope),
                /* 23 */ this.Op(Opcode.Mnemonics.PopScope),
                /* 24 */ this.Op(Opcode.Mnemonics.PopScope),
                /* 25 */ this.Op(Opcode.Mnemonics.PopScope),
                /* 26 */ this.Op(Opcode.Mnemonics.PopScope),
                /* 27 */ this.Op(Opcode.Mnemonics.InitProperty, sprQName),
                /* 28 */ this.Op(Opcode.Mnemonics.ReturnVoid)
            );

            Trait classTrait = new ClassTrait()
            {
                As3class = (AS3ClassDef)spr.Class,
                Kind = TraitKind.Class,
                Name = sprQName
            };
            bindingMethod.AddTrait(classTrait);

            Script bindScript = new Script()
            {
                Method = bindingMethod,
            };

            bindScript.AddTrait(classTrait);

            this.scripts.Insert(0, bindScript); /* Insert at the start to make sure any timeline script is last. */
        }

        /// <summary>
        /// Creates a new, sized integer constant array
        /// </summary>
        /// <param name="count">The number of integers, as read from the ABC file</param>
        internal void SetIntCount(uint count)
        {
            this.IntConsts = new int[count == 0 ? 1 : count];
            this.IntConsts[0] = 0;
        }

        /// <summary>
        /// Creates a new, sized unsigned integer constant array
        /// </summary>
        /// <param name="count">The number of unsigned integers, as read from the ABC file</param>
        internal void SetUintCount(uint count)
        {
            this.UIntConsts = new uint[count == 0 ? 1 : count];
            this.UIntConsts[0] = 0;
        }

        /// <summary>
        /// Creates a new, sized double constant array
        /// </summary>
        /// <param name="count">The number of doubles, as read from the ABC file</param>
        internal void SetDoubleCount(uint count)
        {
            this.DoubleConsts = new ulong[count == 0 ? 1 : count];
            this.DoubleConsts[0] = 0;
        }

        /// <summary>
        /// Creates a new, sized string constant array
        /// </summary>
        /// <param name="count">The number of strings, as read from the ABC file</param>
        internal void SetStringCount(uint count)
        {
            this.StringConsts = new string[count == 0 ? 1 : count];
            this.StringConsts[0] = ABCValues.AnyName;
        }

        /// <summary>
        /// When reading code, references to things like functions and
        /// methods are all done with indices into arrays. This is a bit restrictive,
        /// so after the code is loaded, a call to this will transform all the
        /// code to use object references. E.g a reference to constant string number
        /// 7, will become a string obejct. This is a first step towards merging
        /// abc files together.
        /// </summary>
        internal void Disassemble()
        {
            foreach (Method m in this.Methods)
            {
                m.Disassemble();
            }

            foreach (AS3ClassDef clazz in this.Classes)
            {
                clazz.DisassembleInitializers();
            }
        }

        /// <summary>
        /// At first glance, this does the same as Disassemble(). The difference
        /// is that merely disassembling the code doesn't clear out the bytecode
        /// that was disassembled. Marking code as tampered disassembles it and then
        /// discards the source bytecode, forcing it to require reassembly.
        /// </summary>
        internal void MarkCodeAsTampered()
        {
            foreach (Method m in this.Methods)
            {
                m.Tampered = true;
            }

            foreach (AS3ClassDef clazz in this.Classes)
            {
                clazz.MarkCodeAsTampered();
            }
        }

        /// <summary>
        /// Is the code tampered? I.e. has it been disassembled?
        /// </summary>
        public bool IsTampered
        {
            get
            {
                foreach (Method m in this.Methods)
                {
                    if (m.Tampered)
                    {
                        return true;
                    }
                }

                foreach (AS3ClassDef clazz in this.Classes)
                {
                    if (clazz.IsTampered)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the metadata associated with a key
        /// </summary>
        /// <param name="key">The key to fetch</param>
        /// <returns>A dictionary of metadata values</returns>
        internal Dictionary<string, string> GetMetadata(string key)
        {
            return this.metadata[key];
        }

        /// <summary>
        /// Adds a script to the code.
        /// </summary>
        /// <param name="s">The script to add</param>
        internal void AddScript(Script s)
        {
            this.scripts.Add(s);
        }

        /// <summary>
        /// Replaces the classes with a new set of classes
        /// </summary>
        /// <param name="as3Classes">An array of classes to set in the code.</param>
        internal void SetClasses(AS3ClassDef[] as3Classes)
        {
            this.classes = new List<AS3ClassDef>();
            this.classes.AddRange(as3Classes);
        }

        /// <summary>
        /// Replaces the methods with a new set of methods
        /// </summary>
        /// <param name="methods">An array of methods to set in the code.</param>
        internal void SetMethods(Method[] methods)
        {
            this.methods = new List<Method>();
            this.methods.AddRange(methods);
        }

        /// <summary>
        /// Gets a method at a given index
        /// </summary>
        /// <param name="idx">The method index</param>
        /// <returns>A method. Will throw an exception if it's not valid.</returns>
        internal Method GetMethod(int idx)
        {
            return this.methods[idx];
        }


        /// <param name="sourceFile">This is used when debug output is enabled. If there's a problem in the code,
        /// then the flash player will claim that there is an error in the named source file. Of course this
        /// file doesn't exist - it just needs to be something useful to help trace it back to the code
        /// that generated it. It only has an effect here if debug output is enabled in the SWF writer options.</param>
        internal Method CreateMethod(string sourceFile, uint maxStack, uint localCount, uint initScopeDepth, uint maxScopeDepth, params Opcode[] ops)
        {
            Method m = new Method(this)
            {
                SourceFile = sourceFile,
                MaxStack = maxStack,
                LocalCount = localCount,
                InitScopeDepth = initScopeDepth,
                MaxScopeDepth = maxScopeDepth
            };

            this.methods.Add(m);

            if (ops.Length != 0)
            {
                m.Opcodes = ops;
            }

            return m;
        }

        /// <summary>
        /// Find a class with a specified name
        /// </summary>
        /// <param name="multiname">The multiname that identifies the class</param>
        /// <returns>A class or null if it wasn't found.</returns>
        internal AS3ClassDef FindClass(Multiname multiname)
        {
            /* ISSUE 6: Optimize this. Maintain a map of multinames->classes
             * Linear searches are for losers. */

            foreach (AS3ClassDef c in this.classes)
            {
                if (c.Name == multiname)
                {
                    return c;
                }
            }

            return null;
        }

        /// <summary>
        /// Technically, from an OO perspective, this isn't the most logical place
        /// for this method. It's a factory method for opcodes used when building up
        /// new methods, bytecode by bytecode. It's here because this is the place
        /// that makes calling it require the least typing, which is good when you're
        /// creating lists of loads of these things.
        /// </summary>
        /// <param name="mnemonic">The opcode mnemonic</param>
        /// <param name="args">The argument list. If the types of the args don't
        /// match the type requirements of the opcode, an exception will be
        /// thrown.</param>
        /// <returns>An opcode</returns>
        internal Opcode Op(Opcode.Mnemonics mnemonic, params object[] args)
        {
            Opcode op = new Opcode(this) { Mnemonic = mnemonic, Args = args.Length == 0 ? null : args };

            if (mnemonic != Opcode.Mnemonics.LookupSwitch)
            {
                if (!Opcode.VerifyArgTypes(mnemonic, args))
                {
                    throw new SWFModellerException(
                            SWFModellerError.Internal,
                            "Invalid arg values or types in opcode " + mnemonic.ToString());
                }
            }
            /* ISSUE 7: There must be some way to verify lookupswitch to some degree.
             * Update: There is. The case count must be correct and the offsets must be references
             * to preceeding Label opcodes. */

            return op;
        }

        internal Namespace CreateNamespace(Namespace.NamespaceKind kind, string name, string prefix = null)
        {
            Namespace ns = new Namespace(kind, name, prefix);
            this.nsConsts.Add(ns);
            return ns;
        }

        internal Namespace GetNamespace(int idx)
        {
            return this.nsConsts[idx];
        }

        internal Multiname GetMultiname(int idx)
        {
            return this.multinameConsts[idx];
        }

        internal void AddNamespace(Namespace ns)
        {
            this.nsConsts.Add(ns);
        }

        internal void SetNamespaces(Namespace[] namespaces)
        {
            this.nsConsts = new List<Namespace>();
            this.nsConsts.Add(Namespace.GlobalNS);
            this.nsConsts.AddRange(namespaces);
        }

        internal void SetNamespaceSets(NamespaceSet[] sets)
        {
            this.nsSetConsts = new List<NamespaceSet>();
            this.nsSetConsts.Add(NamespaceSet.EmptySet);
            this.nsSetConsts.AddRange(sets);
        }

        internal Multiname CreateMultiname(Multiname.MultinameKind kind, string name, Namespace ns, NamespaceSet set)
        {
            Multiname mn = new Multiname(kind, name, ns, set);
            this.multinameConsts.Add(mn);
            return mn;
        }

        internal NamespaceSet CreateNamespaceSet(Namespace[] nsRefs)
        {
            NamespaceSet set = new NamespaceSet(nsRefs);
            this.nsSetConsts.Add(set);
            return set;
        }

        internal void SetMultinames(Multiname[] multinames)
        {
            this.multinameConsts = new List<Multiname>();
            this.multinameConsts.Add(Multiname.GlobalMultiname);
            this.multinameConsts.AddRange(multinames);
        }

        internal AS3ClassDef CreateClass()
        {
            AS3ClassDef c = new AS3ClassDef(this);
            this.classes.Add(c);
            return c;
        }

        internal AS3ClassDef GetClassByName(string className)
        {
            /* ISSUE 8: This is a linear search. I HATE linear searches. */
            foreach (AS3ClassDef c in this.classes)
            {
                if (c.QualifiedName == className)
                {
                    return c;
                }
            }
            return null;
        }
    }
}
