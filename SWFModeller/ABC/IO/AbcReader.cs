//-----------------------------------------------------------------------
// AbcReader.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC.IO
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using SWFProcessing.SWFModeller.ABC.Code;

    /// <summary>
    /// An ABC bytecode parser. Parses high level objects and values. Actual
    /// bytecodes are disassembled separately via the Method class.
    /// </summary>
    public class AbcReader
    {
        private AbcCode code;
        private ABCDataTypeReader abcdtr;

        private Dictionary<object, int> LateResolutions;

        private StringBuilder ReadLog;

        /// <summary>
        /// Turns bytecode into an AbcCode object.
        /// </summary>
        /// <param name="bytecode">The bytecode, as chopped out of a SWF.</param>
        /// <param name="readLog">Ignored in release builds. This logs
        /// on every constant value read for unit test inspection.</param>
        /// <returns>A string rendition of the bytecode.</returns>
        public AbcCode Read(byte[] bytecode, StringBuilder readLog)
        {
#if DEBUG
            this.ReadLog = readLog;
            if (this.ReadLog != null)
            {
                this.ReadLog.AppendLine("\nNew ABC file\n-----------\n");
            }
#endif

            this.code = new AbcCode();
            this.abcdtr = new ABCDataTypeReader(new MemoryStream(bytecode));

            this.LateResolutions = new Dictionary<object, int>();

            int minor = this.abcdtr.ReadUI16();
            int major = this.abcdtr.ReadUI16();
            if (minor != AbcFileValues.MinorVersion || major != AbcFileValues.MajorVersion)
            {
                throw new SWFModellerException(
                        SWFModellerError.ABCParsing,
                        "Unsupported version, or not an ABC file.");
            }

            this.ReadConstantPool();
            this.ReadMethods();
            this.ReadMetadata();
            this.ReadClasses();
            this.ReadScriptDefs();
            this.ReadMethodBodies();

            this.ResolveReferences();

            return this.code;
        }

        private void ResolveReferences()
        {
            foreach (object o in this.LateResolutions.Keys)
            {
                if (o is ClassTrait)
                {
                    ((ClassTrait)o).As3class = this.code.GetClass(this.LateResolutions[o]);
                }
                else
                {
                    throw new SWFModellerException(
                            SWFModellerError.Internal,
                            "Unable to resolve late reference of type " + o);
                }
            }
        }

        /// <summary>
        /// Reads in all the constant values referenced from the bytecode.
        /// </summary>
        private void ReadConstantPool()
        {
            uint intCount = this.abcdtr.ReadU30();
#if DEBUG
            if (this.ReadLog != null)
            {
                this.ReadLog.AppendLine("Constant ints length " + intCount);
            }
#endif
            this.code.SetIntCount(intCount);
            for (int i = 1; i < intCount; i++)
            {
                this.code.IntConsts[i] = this.abcdtr.ReadSI32();
#if DEBUG
                if (this.ReadLog != null)
                {
                    this.ReadLog.AppendLine("int #" + i + ": " + this.code.IntConsts[i]);
                }
#endif
            }

            uint uintCount = this.abcdtr.ReadU30();
#if DEBUG
            if (this.ReadLog != null)
            {
                this.ReadLog.AppendLine("Constant uints length " + uintCount);
            }
#endif
            this.code.SetUintCount(uintCount);
            for (int i = 1; i < uintCount; i++)
            {
                this.code.UIntConsts[i] = this.abcdtr.ReadUI32();
#if DEBUG
                if (this.ReadLog != null)
                {
                    this.ReadLog.AppendLine("uint #" + i + ": " + this.code.UIntConsts[i]);
                }
#endif
            }

            uint double64Count = this.abcdtr.ReadU30();
#if DEBUG
            if (this.ReadLog != null)
            {
                this.ReadLog.AppendLine("Constant d64 length " + double64Count);
            }
#endif
            this.code.SetDoubleCount(double64Count);
            for (int i = 1; i < double64Count; i++)
            {
                /* See the comment on AbcCode.DoubleConsts for the reason for all
                 * of this strange ulong shenanigans. */

                ulong ul = (ulong)this.abcdtr.ReadInt32();
                ul |= ((ulong)this.abcdtr.ReadInt32()) << 32;
                this.code.DoubleConsts[i] = ul;
#if DEBUG
                if (this.ReadLog != null)
                {
                    this.ReadLog.AppendLine("d64 #" + i + ": " + this.code.DoubleConsts[i]);
                }
#endif
            }

            uint stringCount = this.abcdtr.ReadU30();
#if DEBUG
            if (this.ReadLog != null)
            {
                this.ReadLog.AppendLine("Constant string length " + stringCount);
            }
#endif
            this.code.SetStringCount(stringCount);
            for (int i = 1; i < stringCount; i++)
            {
                this.code.StringConsts[i] = this.abcdtr.ReadString();
#if DEBUG
                if (this.ReadLog != null)
                {
                    this.ReadLog.AppendLine("str #" + i + ": " + this.code.StringConsts[i]);
                }
#endif
            }

            uint nspaceCount = this.abcdtr.ReadU30();
#if DEBUG
            if (this.ReadLog != null)
            {
                this.ReadLog.AppendLine("Constant ns length " + nspaceCount);
            }
#endif
            for (int i = 1; i < nspaceCount; i++)
            {
                Namespace.NamespaceKind kind = (Namespace.NamespaceKind)this.abcdtr.ReadUI8();

                Namespace ns = null;
                if (kind == Namespace.NamespaceKind.Private)
                {
                    uint sidx = this.abcdtr.ReadU30();
                    string name = string.Empty;
                    if (sidx > 0)
                    {
                        name = this.code.StringConsts[sidx];
                    }

                    ns = this.code.CreateNamespace(kind, name);
                }
                else
                {
                    ns = this.code.CreateNamespace(kind, this.ReadString());
                }
#if DEBUG
                if (this.ReadLog != null)
                {
                    this.ReadLog.AppendLine("ns #" + i + ": " + ns);
                }
#endif
            }

            uint nssetCount = this.abcdtr.ReadU30();
#if DEBUG
            if (this.ReadLog != null)
            {
                this.ReadLog.AppendLine("Constant ns set length " + nssetCount);
            }
#endif
            for (int i = 1; i < nssetCount; i++)
            {
                uint count = this.abcdtr.ReadU30();
                Namespace[] nsRefs = new Namespace[count];

                for (int j = 0; j < count; j++)
                {
                    nsRefs[j] = this.code.GetNamespace((int)this.abcdtr.ReadU30());
                }

                NamespaceSet nss = this.code.CreateNamespaceSet(nsRefs);
#if DEBUG
                if (this.ReadLog != null)
                {
                    this.ReadLog.AppendLine("nss #" + i + ": " + nss);
                }
#endif
            }

            uint multinameCount = this.abcdtr.ReadU30();
#if DEBUG
            if (this.ReadLog != null)
            {
                this.ReadLog.AppendLine("Constant mname length " + multinameCount);
            }
#endif
            for (int i = 1; i < multinameCount; i++)
            {
                string name = null;
                Namespace ns = null;
                NamespaceSet set = null;

                Multiname.MultinameKind kind = (Multiname.MultinameKind)this.abcdtr.ReadUI8();
                switch (kind)
                {
                    case Multiname.MultinameKind.QName:
                    case Multiname.MultinameKind.QNameA:
                        int nsi = (int)this.abcdtr.ReadU30();
                        ns = this.code.GetNamespace(nsi);
                        name = this.ReadString();
                        break;

                    case Multiname.MultinameKind.RTQName:
                    case Multiname.MultinameKind.RTQNameA:
                        name = this.ReadString();
                        break;

                    case Multiname.MultinameKind.RTQNameL:
                    case Multiname.MultinameKind.RTQNameLA:
                        /* No data */
                        break;

                    case Multiname.MultinameKind.Multiname:
                    case Multiname.MultinameKind.MultinameA:
                        name = this.ReadString();
                        set = this.code.GetNamespaceSet((int)this.abcdtr.ReadU30());
                        break;

                    case Multiname.MultinameKind.MultinameL:
                    case Multiname.MultinameKind.MultinameLA:
                        set = this.code.GetNamespaceSet((int)this.abcdtr.ReadU30());
                        break;

                    default:
                        throw new SWFModellerException(
                                SWFModellerError.ABCParsing,
                                "Bad multiname kind in ABC data.");
                }

                Multiname mn = this.code.CreateMultiname(kind, name, ns, set);
#if DEBUG
                if (this.ReadLog != null)
                {
                    this.ReadLog.AppendLine("mn #" + i + ": " + mn);
                }
#endif
            }
        }

        private string ReadString()
        {
            /* String 0 is meant to be an empty string, but this isn't enough for Adobe,
             * no. They also have empty strings elsewhere in the string table because
             * string 0 has some mystical meaning somewhere deep in the flash player. So
             * we need to jump through guessed-at hoops without the benefit of the ABC spec
             * ever really explaining why. Oh flash, you psychotic halfwit.
             */

            uint sidx = this.abcdtr.ReadU30();

            if (sidx == 0)
            {
                return ABCValues.AnyName;
            }

            return this.code.StringConsts[sidx];
        }

        /// <summary>
        /// Read in all the methods and functions.
        /// </summary>
        private void ReadMethods()
        {
            uint methodCount = this.abcdtr.ReadU30();
#if DEBUG
            if (this.ReadLog != null)
            {
                this.ReadLog.AppendLine("methodCount " + methodCount);
            }
#endif

            while (methodCount --> 0)
            {
                Method m = this.code.CreateMethod("method" + methodCount + ".abc", 0, 0, 0, 0);

                uint pcount = this.abcdtr.ReadU30();
                uint rtype = this.abcdtr.ReadU30();

                if (rtype > 0)
                {
                    m.ReturnType = this.code.GetMultiname((int)rtype);
                }
#if DEBUG
                if (this.ReadLog != null)
                {
                    this.ReadLog.AppendLine("method, rtype " + m.ReturnType);
                }
#endif

                for (int j = 0; j < pcount; j++)
                {
                    uint ptype = this.abcdtr.ReadU30();
                    m.AddParam(this.code.GetMultiname((int)ptype));
#if DEBUG
                    if (this.ReadLog != null)
                    {
                        this.ReadLog.AppendLine("  param " + (j + 1) + " type " + this.code.GetMultiname((int)ptype));
                    }
#endif
                }

                m.Name = this.ReadString();
                m.Flags = this.abcdtr.ReadUI8();
#if DEBUG
                if (this.ReadLog != null)
                {
                    this.ReadLog.AppendLine("  name " + m.Name);
                    this.ReadLog.AppendLine("  flags " + m.Flags);
                }
#endif
                if (m.HasOptionalArgs)
                {
                    /* ISSUE 9: We don't store these at the moment. */
                    uint optionCount = this.abcdtr.ReadU30();
                    while (optionCount-- > 0)
                    {
                        /*(void)*/this.abcdtr.ReadU30();
                        /*(void)*/this.abcdtr.ReadUI8();
                    }
                }

                if (m.HasParamNames)
                {
                    for (int j = 0; j < pcount; j++)
                    {
                        /* ISSUE 12 */
                        /*(void)*/this.abcdtr.ReadU30();
                    }
                }
            }
        }

        /// <summary>
        /// Read in the metadata, if any.
        /// </summary>
        private void ReadMetadata()
        {
            uint mdcount = this.abcdtr.ReadU30();
            for (int i = 0; i < mdcount; i++)
            {
                /* The AVM ignores metadata, so so do we. */
                string group = this.ReadString();
#if DEBUG
                if (this.ReadLog != null)
                {
                    this.ReadLog.AppendLine("Metadata " + group);
                }
#endif
                uint itemCount = this.abcdtr.ReadU30();
                while (itemCount-- > 0)
                {
                    string key = this.ReadString();
                    string val = this.ReadString();
#if DEBUG
                    if (this.ReadLog != null)
                    {
                        this.ReadLog.AppendLine("  " + key + " => " + val);
                    }
#endif
                    this.code.AddMetadata(group, key, val);
                }
            }
        }

        /// <summary>
        /// Read in the class definitions.
        /// </summary>
        private void ReadClasses()
        {
            uint classCount = this.abcdtr.ReadU30();

            for (int i = 0; i < classCount; i++)
            {
                AS3ClassDef c = this.code.CreateClass();
                uint nameIdx = this.abcdtr.ReadU30();
                c.Name = this.code.GetMultiname((int)nameIdx);
                uint superIdx = this.abcdtr.ReadU30();
                c.Supername = this.code.GetMultiname((int)superIdx);
                c.Flags = this.abcdtr.ReadUI8();
#if DEBUG
                if (this.ReadLog != null)
                {
                    this.ReadLog.AppendLine("Class #" + (i + 1) + " name '" + c.Name + "', super '" + c.Supername + "', flags: " + c.Flags);
                }
#endif
                if (c.IsProtectedNS)
                {
                    c.ProtectedNS = this.code.GetNamespace((int)this.abcdtr.ReadU30());
#if DEBUG
                    if (this.ReadLog != null)
                    {
                        this.ReadLog.AppendLine("  protected ns " + c.ProtectedNS);
                    }
#endif
                }

                uint interfaceCount = this.abcdtr.ReadU30();
                while (interfaceCount --> 0)
                {
                    Multiname iName = this.code.GetMultiname((int)this.abcdtr.ReadU30());
                    c.AddInterface(iName);
#if DEBUG
                    if (this.ReadLog != null)
                    {
                        this.ReadLog.AppendLine("  implements " + iName);
                    }
#endif
                }

                c.Iinit = this.code.GetMethod((int)this.abcdtr.ReadU30());
#if DEBUG
                if (this.ReadLog != null)
                {
                    this.ReadLog.AppendLine("  iinit " + c.Iinit);
                }
#endif
                uint traitCount = this.abcdtr.ReadU30();
#if DEBUG
                if (this.ReadLog != null)
                {
                    this.ReadLog.AppendLine("  instance traits: " + traitCount);
                }
#endif
                while (traitCount-- > 0)
                {
                    c.AddInstanceTrait(this.ReadTrait());
                }
            }

            for (int i = 0; i < classCount; i++)
            {
                AS3ClassDef c = this.code.GetClass(i);
                c.Cinit = this.code.GetMethod((int)this.abcdtr.ReadU30());
                uint traitCount = this.abcdtr.ReadU30();
#if DEBUG
                if (this.ReadLog != null)
                {
                    this.ReadLog.AppendLine(" class " + c + " has " + traitCount + " static traits");
                }
#endif
                while (traitCount-- > 0)
                {
                    c.AddClassTrait(this.ReadTrait());
                }
            }
        }

        /// <summary>
        /// Read in a trait, which are like object properties.
        /// </summary>
        /// <returns>A trait.</returns>
        private Trait ReadTrait()
        {
            Trait t = null;
            Multiname traitName = this.code.GetMultiname((int)this.abcdtr.ReadU30());
            int traitCode = this.abcdtr.ReadUI8();
            TraitKind kind = (TraitKind)(traitCode & 0xF);

            switch (kind)
            {
                case TraitKind.Slot:
                case TraitKind.Const:
                    SlotTrait st = new SlotTrait();
                    st.Name = traitName;
                    st.Kind = kind;

                    st.SlotID = this.abcdtr.ReadU30();
                    st.TypeName = this.code.GetMultiname((int)this.abcdtr.ReadU30());

                    uint vindex = this.abcdtr.ReadU30();

                    if (vindex != 0)
                    {
                        st.ValKind = (ConstantKind)this.abcdtr.ReadUI8();
                        switch (st.ValKind)
                        {
                            case ConstantKind.ConInt:
                                st.Val = this.code.IntConsts[vindex];
                                break;

                            case ConstantKind.ConUInt:
                                st.Val = this.code.UIntConsts[vindex];
                                break;

                            case ConstantKind.ConDouble:
                                st.Val = this.code.DoubleConsts[vindex];
                                break;

                            case ConstantKind.ConUtf8:
                                st.Val = this.code.StringConsts[vindex];
                                break;

                            case ConstantKind.ConTrue:
                            case ConstantKind.ConFalse:
                            case ConstantKind.ConNull:
                            case ConstantKind.ConUndefined:
                                break;

                            case ConstantKind.ConNamespace:
                            case ConstantKind.ConPackageNamespace:
                            case ConstantKind.ConPackageInternalNs:
                            case ConstantKind.ConProtectedNamespace:
                            case ConstantKind.ConExplicitNamespace:
                            case ConstantKind.ConStaticProtectedNs:
                            case ConstantKind.ConPrivateNs:
                                st.Val = this.code.GetNamespace((int)vindex);
                                break;

                            default:
                                throw new SWFModellerException(
                                        SWFModellerError.Internal,
                                        "Unsupported constant kind: " + st.ValKind.ToString());
                        }
                    }

                    t = st;
                    break;

                case TraitKind.Class:
                    ClassTrait ct = new ClassTrait();
                    ct.Name = traitName;
                    ct.Kind = kind;

                    ct.SlotID = this.abcdtr.ReadU30();
                    this.LateResolutions.Add(ct, (int)this.abcdtr.ReadU30()); /* We'll resolve the class ref later. */

                    t = ct;
                    break;

                case TraitKind.Function:
                    FunctionTrait ft = new FunctionTrait();
                    ft.Name = traitName;
                    ft.Kind = kind;

                    ft.SlotID = this.abcdtr.ReadU30();
                    ft.Fn = this.code.GetMethod((int)this.abcdtr.ReadU30());

                    t = ft;
                    break;

                case TraitKind.Method:
                case TraitKind.Getter:
                case TraitKind.Setter:
                default:
                    MethodTrait mt = new MethodTrait();
                    mt.Name = traitName;
                    mt.Kind = kind;

                    uint dispID = this.abcdtr.ReadU30();
                    if (dispID != 0)
                    {
                        mt.OverriddenMethod = this.code.GetMethod((int)dispID);
                    }

                    mt.Fn = this.code.GetMethod((int)this.abcdtr.ReadU30());

                    t = mt;
                    break;
            }

            return t;
        }

        /// <summary>
        /// Read in the definitions for the code block.
        /// </summary>
        private void ReadScriptDefs()
        {
            uint scriptCount = this.abcdtr.ReadU30();
            for (int i = 0; i < scriptCount; i++)
            {
                uint idx = this.abcdtr.ReadU30();
                Method m = this.code.GetMethod((int)idx);

                Script s = new Script() { Method = m };

                uint traitCount = this.abcdtr.ReadU30();
                while (traitCount --> 0)
                {
                    s.AddTrait(this.ReadTrait());
                }

                this.code.AddScript(s);
            }
        }

        /// <summary>
        /// Read in the method bytecode bodies
        /// </summary>
        private void ReadMethodBodies()
        {
            uint bodyCount = this.abcdtr.ReadU30();
#if DEBUG
            if (this.ReadLog != null)
            {
                this.ReadLog.AppendLine("Body count: " + bodyCount);
            }
#endif

            while (bodyCount-- > 0)
            {
                Method m = this.code.GetMethod((int)this.abcdtr.ReadU30());

                m.MaxStack = this.abcdtr.ReadU30();
                m.LocalCount = this.abcdtr.ReadU30();
                m.InitScopeDepth = this.abcdtr.ReadU30();
                m.MaxScopeDepth = this.abcdtr.ReadU30();

                m.Bytes = this.abcdtr.ReadByteBlock((int)this.abcdtr.ReadU30());
#if DEBUG
                if (this.ReadLog != null)
                {
                    this.ReadLog.AppendLine("Method: " + m);
                    this.ReadLog.AppendLine("  max stack: " + m.MaxStack);
                    this.ReadLog.AppendLine("  locals: " + m.LocalCount);
                    this.ReadLog.AppendLine("  init scope depth: " + m.InitScopeDepth);
                    this.ReadLog.AppendLine("  max scope depth: " + m.MaxScopeDepth);
                }
#endif
                /* Read exception handlers, but ignore for now */
                uint exceptionCount = this.abcdtr.ReadU30();
                while (exceptionCount --> 0)
                {
                    ExceptionHandler eh = new ExceptionHandler();

                    eh.From = (int)this.abcdtr.ReadU30();
                    eh.To = (int)this.abcdtr.ReadU30();
                    eh.Target = (int)this.abcdtr.ReadU30();

                    eh.CatchType = this.code.GetMultiname((int)this.abcdtr.ReadU30());
                    eh.VarName = this.code.GetMultiname((int)this.abcdtr.ReadU30());

                    m.AddExceptionHandler(eh);
                }

                uint traitCount = this.abcdtr.ReadU30();
                while (traitCount-- > 0)
                {
                    m.AddTrait(this.ReadTrait());
                }
            }
        }
    }
}
