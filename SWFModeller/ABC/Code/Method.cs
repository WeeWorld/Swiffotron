//-----------------------------------------------------------------------
// Method.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC.Code
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A method, which may also be a global function.
    /// </summary>
    public class Method
    {
        private byte[] serializedCode = null;

        private Opcode[] opcodes;

        private List<ExceptionHandler> exceptionHandlers;

        private List<Trait> traits;

        private List<Multiname> paramTypes;

#if(DEBUG)
        /// <summary>
        /// For debug output, it's handy to put a unique number next to opcodes
        /// so that we can see where the jump targets are.
        /// </summary>
        private int opcodeIndex;
#endif

        /// <summary>
        /// Create a method object. Please consider using AbcCode.CreateMethod instead to
        /// ensure it's recorded in the right tables.
        /// </summary>
        /// <param name="code">The code that containts this method</param>
        internal Method(AbcCode code)
        {
            this.Code = code;
            this.exceptionHandlers = new List<ExceptionHandler>();
            this.traits = new List<Trait>();
            this.paramTypes = new List<Multiname>();
            this.Name = ABCValues.AnyName; /* Basically the empty string, in this context. */
        }

        /// <summary>
        /// A delegate function to call on each opcode. See OpcodeFilter
        /// </summary>
        /// <param name="op">The opcode.</param>
        /// <param name="abc">The abc within which the opcode resides.</param>
        public delegate void OpcodeDelegate(ref Opcode op, AbcCode abc);

        /// <summary>
        /// Gets or sets the opcodes. Also clears any cached serialized bytecode.
        /// </summary>
        /// <value>
        /// The opcodes.
        /// </value>
        public Opcode[] Opcodes
        {
            get
            {
                return this.opcodes;
            }

            set
            {
                this.opcodes = value;
                this.serializedCode = null;
            }
        }

        public uint MaxStack { get; set; }

        public uint LocalCount { get; set; }

        public uint InitScopeDepth { get; set; }

        public uint MaxScopeDepth { get; set; }

        public string SourceFile { get; set; }

        /// <summary>
        /// If you disassemble a method and also alter the code, this flag will be raised. If
        /// you wish to pretend you've tampered with the code to force re-assembly, set this
        /// property to true.
        /// </summary>
        public bool Tampered
        {
            get
            {
                return this.serializedCode == null;
            }

            set
            {
                if (value)
                {
                    if (this.serializedCode != null)
                    {
                        this.Disassemble();
                        this.serializedCode = null;
                    }
                }
                else
                {
                    throw new SWFModellerException(
                            SWFModellerError.Internal,
                            "You can't untamper a method this way. Call the Method.Assemble() method instead.");
                }
            }
        }

        /// <summary>
        /// The number of exception handlers in this method.
        /// </summary>
        public int ExceptionHandlerCount
        {
            get
            {
                return this.exceptionHandlers.Count;
            }
        }

        /// <summary>
        /// The list of all exception handlers in this method.
        /// </summary>
        public IEnumerator<ExceptionHandler> ExceptionHandlers
        {
            get
            {
                return this.exceptionHandlers.GetEnumerator();
            }
        }

        public int TraitCount
        {
            get
            {
                return this.traits.Count;
            }
        }

        public IEnumerator<Trait> Traits
        {
            get
            {
                return this.traits.GetEnumerator();
            }
        }

        /// <summary>
        /// The body of code to which this method belongs.
        /// </summary>
        public AbcCode Code { get; set; }

        /// <summary>Gets or sets the optional method return type.</summary>
        public Multiname ReturnType { get; set; }

        /// <summary>Gets or sets the method name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the method flags.</summary>
        public int Flags { get; set; }

        /// <summary>
        /// Gets a count of the method's parameters.
        /// </summary>
        public int ParamCount
        {
            get
            {
                return this.paramTypes.Count;
            }
        }

        /// <summary>
        /// Gets an enumerator over the method's parameter types.
        /// </summary>
        public IEnumerator<Multiname> ParamTypes
        {
            get
            {
                return this.paramTypes.GetEnumerator();
            }
        }

        /// <summary>
        /// The method as compiled bytes, ready to be dumped to an ABC file.
        /// </summary>
        public byte[] Bytes
        {
            get
            {
                return this.serializedCode;
            }

            set
            {
                this.serializedCode = value;
                if (this.serializedCode != null)
                {
                    this.opcodes = null;
                }
            }
        }

        /// <summary>Gets a value indicating whether this method needs arguments.</summary>
        public bool NeedsArgs
        {
            get
            {
                return (this.Flags & 0x01) != 0;
            }
        }

        /// <summary>Gets a value indicating whether this method needs activation.</summary>
        public bool NeedsActivation
        {
            get
            {
                return (this.Flags & 0x02) != 0;
            }
        }

        /// <summary>Gets a value indicating whether this method accepts extra parameters.</summary>
        public bool NeedsRest
        {
            get
            {
                return (this.Flags & 0x04) != 0;
            }
        }

        /// <summary>Gets a value indicating whether any arguments are optional</summary>
        public bool HasOptionalArgs
        {
            get
            {
                return (this.Flags & 0x08) != 0;
            }
        }

        /// <summary>Gets a value indicating whether the method use dxns or dxnslate opcodes</summary>
        public bool UsesDxns
        {
            get
            {
                return (this.Flags & 0x40) != 0;
            }
        }

        /// <summary>Gets a value indicating whether the parameters have names</summary>
        public bool HasParamNames
        {
            get
            {
                return (this.Flags & 0x80) != 0;
            }
        }

        /// <summary>
        /// Adds an exception handler to this method
        /// </summary>
        /// <param name="eh">The exception handler.</param>
        public void AddExceptionHandler(ExceptionHandler eh)
        {
            this.exceptionHandlers.Add(eh);
        }

        /// <summary>
        /// Adds a trait to this method.
        /// </summary>
        /// <param name="t">The trait to add.</param>
        public void AddTrait(Trait t)
        {
            this.traits.Add(t);
        }

        /// <summary>
        /// Unit test method present in the debug build. Renders the object as text
        /// that can be written to a diffable output file.
        /// </summary>
        /// <param name="nest">The current nest level in our output</param>
        /// <param name="sb">The buffer that is accumulating output.</param>
        /// <param name="modifiers">The method modifier, e.g. "public"</param>
        [Conditional("DEBUG")]
        public void ToStringModelView(int nest, StringBuilder sb, string modifiers)
        {
            string indent = new string(' ', nest * 4);

            if (modifiers.Length > 0)
            {
                modifiers += " ";
            }

            sb.AppendLine(indent + modifiers + "(" + this.ToString() + ") : " + (this.ReturnType == null ? "Void" : this.ReturnType.ToString()));
            sb.AppendLine(indent + "{");

            sb.AppendLine(indent + "    [MaxStack:" + this.MaxStack + ", LocalCount:" + this.LocalCount + ", InitScopeDepth:" + this.InitScopeDepth + ", MaxScopeDepth:" + this.MaxScopeDepth + "]\n");

            this.Disassemble();
            foreach (ExceptionHandler eh in this.exceptionHandlers)
            {
                sb.AppendLine(indent + "    [Exception handler '" + eh.CatchType + "', from:" + eh.From + " to:" + eh.To + " target:" + eh.Target + "]\n");
            }

            foreach (Opcode op in this.opcodes)
            {
                sb.AppendLine(indent + "    " + op.ToString());
            }

            sb.AppendLine(indent + "}");
        }

        /// <summary>
        /// Renders the method as some sort of signature string so that it's identifiable in
        /// the debugger.
        /// </summary>
        /// <returns>A string with the parameter types in it.</returns>
        public override string ToString()
        {
            string[] paramDefs = new string[this.paramTypes.Count];
            for (int i = 0; i < this.paramTypes.Count; i++)
            {
                /* ISSUE 12: Once we store parameter names, we can show those too. */
                paramDefs[i] = this.paramTypes[i].ToString() + " param" + (i + 1);
            }

            return string.Join(", ", paramDefs);
        }

        /// <summary>
        /// Add a parameter to this method.
        /// </summary>
        /// <param name="type">The parameter type as a multiname</param>
        public void AddParam(Multiname type)
        {
            this.paramTypes.Add(type);
        }

        /// <summary>
        /// Converts the method's bytecode into parsed opcodes, if it hasn't been
        /// disassembled already.
        /// </summary>
        /// <returns>Returns this method, for call chaining.</returns>
        public Method Disassemble()
        {
            if (this.opcodes != null)
            {
                // Already disassembled
                return this;
            }

            ABCDataTypeReader reader = new ABCDataTypeReader(new MemoryStream(this.Bytes));

            List<Opcode> ops = new List<Opcode>();

            Dictionary<int, Opcode> offsetToOpcode = new Dictionary<int, Opcode>();
            Dictionary<Opcode, int> opcodeToOffset = new Dictionary<Opcode, int>();
            Dictionary<Opcode, int> opcodeLen = new Dictionary<Opcode, int>();

            Opcode op = null;
            do
            {
                int offset = (int)reader.Offset;

                op = Opcode.BuildOpcode(reader, this.Code);

                if (op != null)
                {
#if(DEBUG)
                    op.NumberLabel = ++this.opcodeIndex;
#endif
                    int len = op.Mnemonic == Opcode.Mnemonics.LookupSwitch ? 0 : (int)reader.Offset - offset;

                    offsetToOpcode.Add(offset, op);
                    opcodeToOffset.Add(op, offset);
                    opcodeLen.Add(op, len);
                    ops.Add(op);
                }
            }
            while (op != null);

            foreach (ExceptionHandler eh in this.exceptionHandlers)
            {
                eh.From = offsetToOpcode[eh.From];
                eh.To = offsetToOpcode[eh.To];
                eh.Target = offsetToOpcode[eh.Target];
            }

            this.opcodes = ops.ToArray();

            foreach (Opcode hasOffsets in this.opcodes.Where(o => o.HasOffsets))
            {
                hasOffsets.OffsetProc(delegate(object arg)
                {
                    int offset = (int)arg;
                    int current = opcodeToOffset[hasOffsets];
                    int len = opcodeLen[hasOffsets];
                    int jumpTo = current + offset + len;
                    return offsetToOpcode[jumpTo];
                });
            }

            return this;
        }

        /// <summary>
        /// Calls a delegate over all opcodes in this method.
        /// </summary>
        /// <param name="od">The delegate to call.</param>
        public void OpcodeFilter(OpcodeDelegate od)
        {
            this.Disassemble();

            for (int i = 0; i < this.opcodes.Length; i++)
            {
                od(ref this.opcodes[i], this.Code);
            }
        }
    }
}
