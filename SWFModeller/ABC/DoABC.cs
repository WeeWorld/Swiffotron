//-----------------------------------------------------------------------
// DoABC.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC
{
    using System.Diagnostics;
    using System.Text;
    using SWFProcessing.SWFModeller.ABC.Code;
    using SWFProcessing.SWFModeller.ABC.IO;
    using SWFProcessing.SWFModeller.Modelling;
    using SWFProcessing.SWFModeller.Process;

    /// <summary>
    /// Represents a block of binary bytecode in a simple DoABC tag.
    /// </summary>
    public class DoABC
    {
        private byte[] bytecode = null;

        private AbcCode code = null;

        private StringBuilder AbcReadLog;

        /// <summary>
        /// Gets a value indicating whether this code is tampered with (i.e. needs assembly).
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is tampered; otherwise, <c>false</c>.
        /// </value>
        public bool IsTampered
        {
            get
            {
                if (bytecode == null)
                {
                    return true;
                }
                return code.IsTampered;
            }
        }

        /// <summary>
        /// Initializes a new instance of a bytecode block.
        /// </summary>
        /// <param name="lazyInit">Instruct the VM to use lazy initialization on
        /// this block.</param>
        /// <param name="name">The name of the block</param>
        /// <param name="bytecode">The raw bytecode data.</param>
        /// <param name="dbugConstFilter">Ignored in release builds. See AbcReader.DebugConstantFilter
        /// for details.</param>
        public DoABC(bool lazyInit, string name, byte[] bytecode, StringBuilder abcReadLog)
        {
            this.IsLazilyInitialized = lazyInit;
            this.Name = name;
            this.bytecode = bytecode;
#if DEBUG
            this.AbcReadLog = abcReadLog;
#endif
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// The raw bytecode data.
        /// </summary>
        public byte[] Bytecode
        {
            get
            {
                return this.bytecode;
            }

            set
            {
                this.bytecode = value;
                this.code = null;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this code is lazily initialized.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is lazily initialized; otherwise, <c>false</c>.
        /// </value>
        public bool IsLazilyInitialized { get; set; }

        public AbcCode Code
        {
            get
            {
                if (this.code == null)
                {
                    this.code = new AbcReader().Read(this.bytecode, this.AbcReadLog);
                }
                return this.code;
            }
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
            sb.AppendLine(indent + "DoABC " + (this.IsLazilyInitialized ? "(Lazy init)" : "(Immediate init)"));
            sb.AppendLine(indent + "{");
            this.Code.ToStringModelView(nest + 1, sb);
            sb.AppendLine(indent + "}");
        }

        /// <summary>
        /// Disassembles all code. The loaded bytecode will be cached unless you tamper
        /// with the disassembled code in which case it will need re-assembly.
        /// </summary>
        public void Disassemble()
        {
            this.Code.Disassemble();
        }

        /// <summary>
        /// Disassembles all code and marks it as requiring re-assembly (i.e. the
        /// cached bytecode bytes are invalid and can't be written to file).
        /// </summary>
        public void MarkCodeAsTampered()
        {
            this.Code.MarkCodeAsTampered();
        }

        /// <summary>
        /// Merges some code into this code.
        /// </summary>
        /// <param name="abc">The code to merge into this object. Once merged, you should
        /// discard 'abc'.</param>
        internal void Merge(DoABC abc)
        {
            /* Because we want everything to be object references... */

            AbcCode abcCode = abc.Code; /* This is a kludge to force initial parsing of the abc data, if not done already */
            AbcCode thisCode = this.Code;

            abc.Disassemble(); /* This ensures that the code is disassembled into mergable form */
            this.Disassemble();

            foreach (AS3ClassDef clazz in abc.Code.Classes)
            {
                AS3ClassDef classCollision = thisCode.FindClass(clazz.Name);
                if (classCollision != null)
                {
                    /* TODO: We create a dummy context here, which seems wrong somehow. But then
                     * what context do we use? */
                    throw new SWFModellerException(
                            SWFModellerError.CodeMerge,
                            "Class name collision on " + clazz.Name,
                            new SWFContext(string.Empty).Sentinel("ClassNameCollision"));
                }
                thisCode.AddClass(clazz);
            }
        }

        /// <summary>
        /// Generates a main timeline script for a new SWF
        /// </summary>
        /// <param name="qClassName">Qualified class name for the MainTimeline class,
        /// e.g. mygeneratedswf_fla.MainTimeline</param>
        /// <returns>A DoABC tag that can be inserted into a new SWF, which may be the one
        /// from the timeline (And so may already be in the SWF).</returns>
        public static DoABC GenerateDefaultScript(string qClassName, Timeline timeline)
        {
            DoABC abc = timeline.Root.FirstScript;
            if (abc == null)
            {
                abc = new DoABC(true, string.Empty, null, null);
                abc.code = new AbcCode();
            }

            AS3ClassDef classDef = GenerateTimelineClass(abc.code, qClassName);
            timeline.Class = classDef;

            Script s = new Script() { Method = GenerateTimelineScript(abc.code, classDef) };

            abc.code.AddScript(s);

            s.AddTrait(new ClassTrait() { As3class = classDef, Kind = TraitKind.Class, Name = timeline.Class.Name });

            return abc;
        }

        /// <summary>
        /// Process all the methods in this code block.
        /// </summary>
        /// <param name="mp">A delegate function that will process each method somehow.</param>
        internal void MethodProc(AbcCode.MethodProcessor mp)
        {
            this.Code.MethodProc(mp);
        }

        /// <summary>
        /// Factory method for a new timeline script.
        /// </summary>
        /// <param name="abc">The abc object to create the script into.</param>
        /// <param name="timelineClass">A generated timeline class. See GenerateTimelineClass</param>
        /// <returns>The new method</returns>
        private static Method GenerateTimelineScript(AbcCode abc, AS3ClassDef timelineClass)
        {
            Multiname mnMovieClip = timelineClass.Supername;
            Namespace nsFlashDisplay = mnMovieClip.NS;

            Namespace nsEmptyPackage = abc.CreateNamespace(Namespace.NamespaceKind.Package, string.Empty);
            Namespace nsFlashEvents = abc.CreateNamespace(Namespace.NamespaceKind.Package, "flash.events");

            Multiname mnObject = abc.CreateMultiname(Multiname.MultinameKind.QName, "Object", nsEmptyPackage, null);
            Multiname mnEventDispatcher = abc.CreateMultiname(Multiname.MultinameKind.QName, "EventDispatcher", nsFlashEvents, null);
            Multiname mnDisplayObject = abc.CreateMultiname(Multiname.MultinameKind.QName, "DisplayObject", nsFlashDisplay, null);
            Multiname mnInteractiveObject = abc.CreateMultiname(Multiname.MultinameKind.QName, "InteractiveObject", nsFlashDisplay, null);
            Multiname mnDisplayObjectContainer = abc.CreateMultiname(Multiname.MultinameKind.QName, "DisplayObjectContainer", nsFlashDisplay, null);
            Multiname mnSprite = abc.CreateMultiname(Multiname.MultinameKind.QName, "Sprite", nsFlashDisplay, null);

            return abc.CreateMethod("Timeline.abc", 2, 1, 1, 9,

                /* The above magic numbers come from the numbers generated by IDE versions of this function.
                 * I have no real ideal about how I'd work them out for myself, which would obviously be
                 * more ideal. */
                abc.Op(Opcode.Mnemonics.GetLocal0),
                abc.Op(Opcode.Mnemonics.PushScope),
                abc.Op(Opcode.Mnemonics.GetScopeObject, (byte)0),
                abc.Op(Opcode.Mnemonics.GetLex, mnObject),
                abc.Op(Opcode.Mnemonics.PushScope),
                abc.Op(Opcode.Mnemonics.GetLex, mnEventDispatcher),
                abc.Op(Opcode.Mnemonics.PushScope),
                abc.Op(Opcode.Mnemonics.GetLex, mnDisplayObject),
                abc.Op(Opcode.Mnemonics.PushScope),
                abc.Op(Opcode.Mnemonics.GetLex, mnInteractiveObject),
                abc.Op(Opcode.Mnemonics.PushScope),
                abc.Op(Opcode.Mnemonics.GetLex, mnDisplayObjectContainer),
                abc.Op(Opcode.Mnemonics.PushScope),
                abc.Op(Opcode.Mnemonics.GetLex, mnSprite),
                abc.Op(Opcode.Mnemonics.PushScope),
                abc.Op(Opcode.Mnemonics.GetLex, mnMovieClip),
                abc.Op(Opcode.Mnemonics.PushScope),
                abc.Op(Opcode.Mnemonics.GetLex, mnMovieClip),
                abc.Op(Opcode.Mnemonics.NewClass, timelineClass),
                abc.Op(Opcode.Mnemonics.PopScope),
                abc.Op(Opcode.Mnemonics.PopScope),
                abc.Op(Opcode.Mnemonics.PopScope),
                abc.Op(Opcode.Mnemonics.PopScope),
                abc.Op(Opcode.Mnemonics.PopScope),
                abc.Op(Opcode.Mnemonics.PopScope),
                abc.Op(Opcode.Mnemonics.PopScope),
                abc.Op(Opcode.Mnemonics.InitProperty, timelineClass.Name),
                abc.Op(Opcode.Mnemonics.ReturnVoid)
            );
        }

        /// <summary>
        /// Factory method for a new timeline class.
        /// </summary>
        /// <param name="abc">The abc code to create the class within.</param>
        /// <param name="packageName">Name of the fla. You can make this up, since you probably don't have
        /// a fla.</param>
        /// <param name="className">Name of the class.</param>
        /// <returns>A bew timeline class.</returns>
        private static AS3ClassDef GenerateTimelineClass(AbcCode abc, string qClassName)
        {
            int splitPos = qClassName.LastIndexOf('.');
            if (splitPos < 0)
            {
                throw new SWFModellerException(SWFModellerError.CodeMerge,
                        "A generated timeline class must have a package name.",
                        new SWFContext(string.Empty).Sentinel("TimelineDefaultPackage"));
            }
            string packageName = qClassName.Substring(0, splitPos);
            string className = qClassName.Substring(splitPos + 1);

            /* Class name: */
            Namespace flaNS = abc.CreateNamespace(Namespace.NamespaceKind.Package, packageName);
            Multiname classMultinameName = abc.CreateMultiname(Multiname.MultinameKind.QName, className, flaNS, null);

            /* Superclass: */
            Namespace nsFlashDisplay = abc.CreateNamespace(Namespace.NamespaceKind.Package, "flash.display");
            Multiname mnMovieClip = abc.CreateMultiname(Multiname.MultinameKind.QName, "MovieClip", nsFlashDisplay, null);

            AS3ClassDef newClass = abc.CreateClass();

            newClass.Name = classMultinameName;
            newClass.Supername = mnMovieClip;

            Namespace protectedNS = abc.CreateNamespace(Namespace.NamespaceKind.Protected, packageName + ":" + className);

            newClass.ProtectedNS = protectedNS;

            newClass.Cinit = abc.CreateMethod(className + "Constructor.abc", 1, 1, 9, 10,

                /* The above magic numbers come from the numbers generated by IDE versions of this function.
                 * I have no real ideal about how I'd work them out for myself, which would obviously be
                 * more ideal. */

                /* AFAICT, this is always generated by the IDE because the abc file format
                 * doesn't allow for classes with no static initialiser. It doesn't seem
                 * to actually do anything. */

                abc.Op(Opcode.Mnemonics.GetLocal0),
                abc.Op(Opcode.Mnemonics.PushScope),
                abc.Op(Opcode.Mnemonics.ReturnVoid)
            );

            newClass.Iinit = abc.CreateMethod(className + "ClassInit.abc", 1, 1, 10, 11,

                /* The above magic numbers come from the numbers generated by IDE versions of this function.
                 * I have no real ideal about how I'd work them out for myself, which would obviously be
                 * more ideal. */

                abc.Op(Opcode.Mnemonics.GetLocal0),
                abc.Op(Opcode.Mnemonics.PushScope),
                abc.Op(Opcode.Mnemonics.GetLocal0),
                abc.Op(Opcode.Mnemonics.ConstructSuper, 0U),

                /* TODO: Initialization code to be merged here. Supply a mechanism for this please. */

                abc.Op(Opcode.Mnemonics.ReturnVoid)
            );

            return newClass;
        }
    }
}
