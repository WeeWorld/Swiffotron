//-----------------------------------------------------------------------
// AbcWriter.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC.IO
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using SWFProcessing.SWFModeller.ABC;
    using SWFProcessing.SWFModeller.ABC.Code;
    using SWFProcessing.SWFModeller.IO;

    internal class AbcWriter
    {
        private IDMarshaller<int> intMarshal;
        private IDMarshaller<uint> uintMarshal;
        private IDMarshaller<string> stringMarshal;
        private IDMarshaller<ulong> doubleMarshal;
        private IDMarshaller<Namespace> nsMarshal;
        private IDMarshaller<NamespaceSet> nsSetMarshal;
        private IDMarshaller<Multiname> multinameMarshal;
        private IDMarshaller<AS3ClassDef> classMarshal;
        private IDMarshaller<Method> methodMarshal;

        private StringBuilder writeLog;

        private bool InsertDebugCodes;

        public void AssembleIfNecessary(DoABC codeTag, bool insertDebugCodes, string mainClassName, StringBuilder writeLog)
        {
            this.InsertDebugCodes = insertDebugCodes;

#if DEBUG
            this.writeLog = writeLog;
            if (this.writeLog == null)
            {
                /* Bit of a kludge. If we passed null for this, we don't want a log. We'd rather not
                 * put null tests everywhere though, so we create this string builder and log stuff
                 * to it knowing full well it will be discarded and is a waste of time. It's convenient,
                 * and it's only the debug build, so we don't really care about the waste. */
                this.writeLog = new StringBuilder();
            }
#endif

            /* Firstly, if any methods are altered, we need to decompile all of them since the
             * table references will all be wrong. Check all the methods for tampering... */

            AbcCode code = codeTag.Code;

            if (!code.IsTampered)
            {
                return;
            }

            /* At this point, all we know is that at least one piece of code
             * needs re-assembly. IsTampered doesn't meant everything is necessarily
             * tampered, although it probably is. */

            /* We've established that we actually do need to do some work */

            MemoryStream buffer = new MemoryStream();
            ABCDataTypeWriter writer = new ABCDataTypeWriter(buffer);

            /* Ok, someone tampered with a method. Now we need to decompile them all and
             * re-build them... */
            foreach (Method m in code.Methods)
            {
                if (!m.Tampered)
                {
                    m.Tampered = true; /* Dissassembles the code and forces re-assembly. */
                }
            }

            /* Another side-effect of such meddling is the need to re-build the tables... */
            this.ReBuildTables(code, mainClassName);

            /* At this point, we will have a set of clean-built or untampered tables, and
             * calls to Method.Bytes will either return pre-built bytecode, or assemble it
             * on-demand for us. In other words, everything can just be dumped into a file. */

            writer.WriteUI16(AbcFileValues.MinorVersion);
            writer.WriteUI16(AbcFileValues.MajorVersion);

            byte[] methodInfoBytes = this.GenerateMethodInfo();
            byte[] metadataBytes = this.GenerateMetadata(code);
            byte[] classInfo = this.GenerateClassInfo();
            byte[] scriptInfo = this.GenerateScriptInfo(code);

            /* When we write out the constant pool, the constants are fixed in the code object with
             * those values. In other words at this point we'd better be damn sure we've added all the
             * constants to the pools. */
            this.WriteConstantPool(writer, code);

            writer.Write(methodInfoBytes, 0, methodInfoBytes.Length);
            writer.Write(metadataBytes, 0, metadataBytes.Length);
            writer.Write(classInfo, 0, classInfo.Length);
            writer.Write(scriptInfo, 0, scriptInfo.Length);

            this.WriteMethodBodies(writer);

            writer.Close(); /* Also closes buffer. */
            codeTag.Bytecode = buffer.ToArray();
        }

        private void RegisterMultiname(Multiname mn)
        {
            this.stringMarshal.Register(mn.Name);

            if (mn.NS != null)
            {
                this.nsMarshal.Register(mn.NS);
            }

            if (!mn.IsEmptySet)
            {
                foreach (Namespace ns in mn.Set)
                {
                    this.nsMarshal.Register(ns);
                }

                this.nsSetMarshal.Register(mn.Set);
            }

            this.multinameMarshal.Register(mn);
        }

        private void AssembleMethod(Method method)
        {
            MemoryStream buf = new MemoryStream();
            ABCDataTypeWriter writer = new ABCDataTypeWriter(buf);

            this.methodMarshal.Register(method);

            using (IEnumerator<ExceptionHandler> i = method.ExceptionHandlers)
            {
                while (i.MoveNext())
                {
                    ExceptionHandler eh = i.Current;
                    this.RegisterMultiname(eh.CatchType);
                    this.RegisterMultiname(eh.VarName);
                }
            }

            bool insertDebugCodes = this.InsertDebugCodes;

            if (insertDebugCodes)
            {
                /* Quick check to see if the code already has them, in which case we
                 * won't bother. */
                foreach (Opcode op in method.Opcodes)
                {
                    if (op.Mnemonic == Opcode.Mnemonics.DebugFile || op.Mnemonic == Opcode.Mnemonics.DebugLine)
                    {
                        insertDebugCodes = false;
                        break;
                    }
                }
            }

            IEnumerable<Opcode> opcodes = method.Opcodes;

            if (insertDebugCodes)
            {
                List<Opcode> debugged = new List<Opcode>();

                debugged.Add(Opcode.CreateDebugFile(method.SourceFile));

                /* Let's just check this string won't break things... */
                uint srcPos = (uint)this.stringMarshal.GetIDFor(method.SourceFile);
                if (srcPos > 255)
                {
                    /* ISSUE 10: Best fix may be to strip out debug opcodes. */
                    throw new SWFModellerException(
                            SWFModellerError.CodeMerge,
                            "Marshaller failed to keep debug string at low index");
                }

                uint lineNumber = 1; /* For our fake debug info. */
                foreach (Opcode op in opcodes)
                {
                    debugged.Add(Opcode.CreateDebugLine(lineNumber++));
                    debugged.Add(op);
                }

                opcodes = debugged;
            }

            Dictionary<Opcode, int> offsetMap = new Dictionary<Opcode, int>();
            Dictionary<Opcode, int> lenMap = new Dictionary<Opcode, int>();
            List<KeyValuePair<Opcode, KeyValuePair<Opcode, int>>> unresolvedOffsets = new List<KeyValuePair<Opcode, KeyValuePair<Opcode, int>>>();

            foreach (Opcode op in opcodes)
            {
                int startOffset = (int)writer.Offset;
                offsetMap.Add(op, startOffset);

                writer.WriteUI8(op.Instruction);

                if (op.Mnemonic == Opcode.Mnemonics.LookupSwitch)
                {
                    for (int i = 0; i < op.Args.Length; i++)
                    {
                        int argOffset = (int)writer.Offset - startOffset;
                        KeyValuePair<Opcode, int> sourceInfo = new KeyValuePair<Opcode, int>(op, argOffset);
                        object argOb = op.Args[i];
                        if (i == 1)
                        {
                            writer.WriteU30Packed((uint)(op.Args.Length - 3));
                        }
                        else
                        {
                            unresolvedOffsets.Add(new KeyValuePair<Opcode, KeyValuePair<Opcode, int>>((Opcode)argOb, sourceInfo));
                            writer.WriteSI24(0x00FFDEAD); /* Because dead beef won't fit in 3 bytes. */
                        }
                    }

                    lenMap.Add(op, 0);
                    continue;
                }

                Opcode.ArgType[] types = op.ArgTypes;
                if (types == null)
                {
                    /* done. */
                    lenMap.Add(op, (int)writer.Offset - startOffset);
                    continue;
                }

                if (types.Length != op.Args.Length)
                {
                    throw new SWFModellerException(
                            SWFModellerError.Internal,
                            "Arg mismatch in op " + op + " (" + types.Length + " expected)");
                }

                for (int i = 0; i < op.Args.Length; i++)
                {
                    object argOb = op.Args[i];
                    Opcode.ArgType argType = types[i];

                    switch (argType)
                    {
                        case Opcode.ArgType.MultinameU30:
                            writer.WriteU30Packed((uint)this.MultinameID((Multiname)argOb));
                            break;

                        case Opcode.ArgType.CountU30:
                        case Opcode.ArgType.UintU30:
                        case Opcode.ArgType.IntU30:
                        case Opcode.ArgType.LineNumberU30:
                        case Opcode.ArgType.RegisterU30:
                        case Opcode.ArgType.ObjectRegisterU30:
                        case Opcode.ArgType.PropertyRegisterU30:
                        case Opcode.ArgType.ShortU30:
                        case Opcode.ArgType.DoubleU30:
                            writer.WriteU30Packed((uint)argOb);
                            break;

                        case Opcode.ArgType.ByteU8:
                        case Opcode.ArgType.DebugU8:
                        case Opcode.ArgType.StackU8:
                            writer.WriteUI8((byte)argOb);
                            break;

                        case Opcode.ArgType.StringU8:
                            uint spos = (uint)this.stringMarshal.GetIDFor((string)argOb);
                            if (spos > 255)
                            {
                                /* ISSUE 10: Best fix may be to strip out debug opcodes. */
                                throw new SWFModellerException(
                                        SWFModellerError.Internal,
                                        "Marshaller failed to keep debug string at low index");
                            }

                            writer.WriteUI8(spos);
                            break;

                        case Opcode.ArgType.StringU30:
                            writer.WriteU30Packed((uint)this.stringMarshal.GetIDFor((string)argOb));
                            break;

                        case Opcode.ArgType.ClassU30:
                            writer.WriteU30Packed((uint)this.classMarshal.GetIDFor((AS3ClassDef)argOb));
                            break;

                        case Opcode.ArgType.ByteS8:
                            writer.WriteSI8((int)argOb);
                            break;

                        case Opcode.ArgType.ShortS30:
                            writer.WriteSI32((int)argOb); /* Spec says this signed value is a U30. Stupid spec. */
                            break;

                        case Opcode.ArgType.OffsetS24:
                            if (argOb is Opcode)
                            {
                                int argOffset = (int)writer.Offset - startOffset;
                                KeyValuePair<Opcode, int> sourceInfo = new KeyValuePair<Opcode, int>(op, argOffset);
                                unresolvedOffsets.Add(new KeyValuePair<Opcode, KeyValuePair<Opcode, int>>((Opcode)argOb, sourceInfo));
                                writer.WriteSI24(0x00FFDEAD); /* Because dead beef won't fit in 3 bytes. */
                            }
                            else
                            {
                                /* ISSUE 73 */
                                throw new SWFModellerException(
                                        SWFModellerError.UnimplementedFeature,
                                        "Unsupported op arg type in " + op.Mnemonic.ToString() + ": " + argType);
                            }

                            break;

                        case Opcode.ArgType.ExceptionU30:
                            writer.WriteU30Packed((uint)argOb); /* Reference into the fixed list of exception handlers. TODO: Make it not so fixed. */
                            break;

                        case Opcode.ArgType.SlotU30:
                            writer.WriteU30Packed((uint)argOb); /* Reference into the fixed list of exception handlers. TODO: Make it not so fixed. */
                            break;

                        case Opcode.ArgType.NamespaceU30:
                        case Opcode.ArgType.MethodU30:
                        case Opcode.ArgType.DebugTypeU30:
                        default:
                            /* ISSUE 73 */
                            throw new SWFModellerException(
                                    SWFModellerError.UnimplementedFeature,
                                    "Unsupported op arg type in "+op.Mnemonic.ToString()+": " + argType);
                    }
                }

                lenMap.Add(op, (int)writer.Offset - startOffset);
            }

            using (IEnumerator<ExceptionHandler> i = method.ExceptionHandlers)
            {
                while (i.MoveNext())
                {
                    ExceptionHandler eh = i.Current;
                    eh.From = offsetMap[eh.From];
                    eh.To = offsetMap[eh.To];
                    eh.Target = offsetMap[eh.Target];
                }
            }

            writer.Close(); /* Also closes the buffer. */

            byte[] byteBuffer = buf.ToArray();

            foreach (KeyValuePair<Opcode, KeyValuePair<Opcode, int>> link in unresolvedOffsets)
            {
                KeyValuePair<Opcode, int> sourceInfo = link.Value;
                int srcOffset = offsetMap[sourceInfo.Key];
                int argOffset = srcOffset + sourceInfo.Value;
                int val = offsetMap[link.Key] - srcOffset - lenMap[sourceInfo.Key];
                new ABCDataTypeWriter(new MemoryStream(byteBuffer, argOffset, 3)).WriteSI24(val);
            }

            method.Bytes = byteBuffer;
        }

        private void WriteMethodBodies(ABCDataTypeWriter writer)
        {
            Method[] methods = this.methodMarshal.ToArray();
            writer.WriteU30Packed((uint)methods.Length);

            foreach (Method m in methods)
            {
                writer.WriteU30Packed((uint)this.methodMarshal.GetExistingIDFor(m));

                writer.WriteU30Packed(m.MaxStack);
                writer.WriteU30Packed(m.LocalCount);
                writer.WriteU30Packed(m.InitScopeDepth);
                writer.WriteU30Packed(m.MaxScopeDepth);

                byte[] bytecode = m.Bytes;

                writer.WriteU30Packed((uint)bytecode.Length);
                writer.Write(bytecode, 0, bytecode.Length);

                /* Exception handlers... */
                /* ISSUE 11: This is a fixed list from the moment the method is loaded. You can't add and
                 * remove from exception handlers since they are referenced from newexception opcodes
                 * by index into this list. Fix this by removing the list and marshalling the references
                 * when required. */
                writer.WriteU30Packed((uint)m.ExceptionHandlerCount);
                using (IEnumerator<ExceptionHandler> i = m.ExceptionHandlers)
                {
                    while (i.MoveNext())
                    {
                        ExceptionHandler eh = i.Current;

                        writer.WriteU30Packed((uint)((int)eh.From));
                        writer.WriteU30Packed((uint)((int)eh.To));
                        writer.WriteU30Packed((uint)((int)eh.Target));

                        if (eh.IsCatchAll)
                        {
                            writer.WriteU30Packed(0);
                        }
                        else
                        {
                            writer.WriteU30Packed((uint)this.multinameMarshal.GetIDFor(eh.CatchType));
                        }

                        writer.WriteU30Packed((uint)this.multinameMarshal.GetIDFor(eh.VarName));
                    }
                }

                /* Method traits... */
                writer.WriteU30Packed((uint)m.TraitCount);
                using (IEnumerator<Trait> i = m.Traits)
                {
                    while (i.MoveNext())
                    {
                        this.WriteTraitInfo(writer, i.Current);
                    }
                }
            }
        }

        private byte[] GenerateScriptInfo(AbcCode code)
        {
            MemoryStream buf = new MemoryStream();
            ABCDataTypeWriter writer = new ABCDataTypeWriter(buf);

            writer.WriteU30Packed((uint)code.ScriptCount);

            foreach (Script s in code.Scripts)
            {
                writer.WriteU30Packed((uint)this.methodMarshal.GetIDFor(s.Method));

                writer.WriteU30Packed((uint)s.TraitCount);
                using (IEnumerator<Trait> i = s.Traits)
                {
                    while (i.MoveNext())
                    {
                        this.WriteTraitInfo(writer, i.Current);
                    }
                }
            }

            writer.Close(); /* Closes the buffer */
            return buf.ToArray();
        }

        private byte[] GenerateClassInfo()
        {
            MemoryStream buf = new MemoryStream();
            ABCDataTypeWriter writer = new ABCDataTypeWriter(buf);

            AS3ClassDef[] classes = this.classMarshal.ToArray();

            writer.WriteU30Packed((uint)classes.Length);
#if DEBUG
            int _cid = 1;
            this.writeLog.AppendLine("Classes count " + classes.Length);
#endif
            /* First, instance info... */
            foreach (AS3ClassDef clazz in classes)
            {
#if DEBUG
                this.writeLog.AppendLine((_cid++) + " Class " + clazz.Name + " (mn ID " + (uint)this.MultinameID(clazz.Name) + ")");
#endif
                writer.WriteU30Packed((uint)this.MultinameID(clazz.Name));
                writer.WriteU30Packed((uint)this.MultinameID(clazz.Supername));
                writer.WriteUI8((uint)clazz.Flags);

                if (clazz.IsProtectedNS)
                {
                    writer.WriteU30Packed((uint)this.NamespaceID(clazz.ProtectedNS));
                }

                writer.WriteU30Packed((uint)clazz.InterfaceCount);

                using (IEnumerator<Multiname> i = clazz.Interfaces)
                {
                    while (i.MoveNext())
                    {
#if DEBUG
                        this.writeLog.AppendLine("    implements " + i.Current + " (mn ID " + (uint)this.MultinameID(i.Current) + ")");
#endif
                        writer.WriteU30Packed((uint)this.MultinameID(i.Current));
                    }
                }
#if DEBUG
                this.writeLog.AppendLine("    Iinit ID " + (uint)this.methodMarshal.GetIDFor(clazz.Iinit));
#endif
                writer.WriteU30Packed((uint)this.methodMarshal.GetIDFor(clazz.Iinit));

                writer.WriteU30Packed((uint)clazz.InstanceTraitCount);
                using (IEnumerator<Trait> i = clazz.InstanceTraits)
                {
                    while (i.MoveNext())
                    {
                        this.WriteTraitInfo(writer, i.Current);
                    }
                }
            }

            /* Second, class info... */
            foreach (AS3ClassDef clazz in classes)
            {
#if DEBUG
                this.writeLog.AppendLine("Class cinit ID for '" + clazz.Name + "' " + (uint)this.methodMarshal.GetIDFor(clazz.Cinit));
#endif
                writer.WriteU30Packed((uint)this.methodMarshal.GetIDFor(clazz.Cinit));

                writer.WriteU30Packed((uint)clazz.ClassTraitCount);
                using (IEnumerator<Trait> i = clazz.ClassTraits)
                {
                    while (i.MoveNext())
                    {
                        this.WriteTraitInfo(writer, i.Current);
                    }
                }
            }

            writer.Close(); /* Closes the buffer */
            return buf.ToArray();
        }

        private void WriteTraitInfo(ABCDataTypeWriter writer, Trait t)
        {
            writer.WriteU30Packed((uint)this.MultinameID(t.Name));
            writer.WriteUI8((uint)t.Kind);

            switch (t.Kind)
            {
                case TraitKind.Slot:
                case TraitKind.Const:
                    SlotTrait st = (SlotTrait)t;
                    writer.WriteU30Packed(st.SlotID);
                    writer.WriteU30Packed((uint)this.MultinameID(st.TypeName));

                    if (st.Val == null)
                    {
                        writer.WriteU30Packed(0);
                    }
                    else
                    {
                        switch (st.ValKind)
                        {
                            case ConstantKind.ConInt:
                                writer.WriteU30Packed((uint)this.intMarshal.GetIDFor((int)st.Val));
                                break;

                            case ConstantKind.ConUInt:
                                writer.WriteU30Packed((uint)this.uintMarshal.GetIDFor((uint)st.Val));
                                break;

                            case ConstantKind.ConDouble:
                                writer.WriteU30Packed((uint)this.doubleMarshal.GetIDFor((ulong)st.Val));
                                break;

                            case ConstantKind.ConUtf8:
                                writer.WriteU30Packed((uint)this.stringMarshal.GetIDFor((string)st.Val));
                                break;

                            case ConstantKind.ConTrue:
                                /* Through observation, this always gets set to 11, I do not know why. It
                                 * seems though that it should be any non-zero number. */
                                writer.WriteU30Packed(11);
                                break;

                            case ConstantKind.ConFalse:
                                /* Through observation, this always gets set to 11, I do not know why. It
                                 * seems though that it should be any non-zero number. */
                                writer.WriteU30Packed(10);
                                break;

                            case ConstantKind.ConNull:
                                /* Through observation, this always gets set to 11, I do not know why. It
                                 * seems though that it should be any non-zero number. */
                                writer.WriteU30Packed(12);
                                break;

                            case ConstantKind.ConUndefined:
                                /* Through observation, true, false and null all seem to have ignored but
                                 * specific values. I haven't seen one for undefined, but I'm guessing it's
                                 * 13. Really want to know what these number are. The don't seem to relate
                                 * the string or multiname tables. */
                                writer.WriteU30Packed(13);
                                break;

                            case ConstantKind.ConNamespace:
                            case ConstantKind.ConPackageNamespace:
                            case ConstantKind.ConPackageInternalNs:
                            case ConstantKind.ConProtectedNamespace:
                            case ConstantKind.ConExplicitNamespace:
                            case ConstantKind.ConStaticProtectedNs:
                            case ConstantKind.ConPrivateNs:
                                writer.WriteU30Packed((uint)this.NamespaceID((Namespace)st.Val));
                                break;
                            default:
                                throw new SWFModellerException(
                                        SWFModellerError.Internal,
                                        "Unsupported constant type: " + st.ValKind.ToString());
                        }

                        writer.WriteUI8((uint)st.ValKind);
                    }

                    break;

                case TraitKind.Method:
                case TraitKind.Getter:
                case TraitKind.Setter:
                    MethodTrait mt = (MethodTrait)t;
                    if (mt.OverriddenMethod != null)
                    {
                        writer.WriteU30Packed((uint)this.methodMarshal.GetIDFor(mt.OverriddenMethod));
                    }
                    else
                    {
                        writer.WriteU30Packed(0);
                    }

                    writer.WriteU30Packed((uint)this.methodMarshal.GetIDFor(mt.Fn));
                    break;

                case TraitKind.Class:
                    ClassTrait ct = (ClassTrait)t;
                    writer.WriteU30Packed((uint)ct.SlotID);
                    writer.WriteU30Packed((uint)this.classMarshal.GetIDFor(ct.As3class));
                    break;

                case TraitKind.Function:
                    FunctionTrait ft = (FunctionTrait)t;
                    writer.WriteU30Packed((uint)ft.SlotID);
                    writer.WriteU30Packed((uint)this.methodMarshal.GetIDFor(ft.Fn));
                    break;

                default:
                    /* ISSUE 73 */
                    throw new SWFModellerException(
                            SWFModellerError.UnimplementedFeature,
                            "Unsupported trait kind: " + t.Kind.ToString());
            }
        }

        private byte[] GenerateMetadata(AbcCode code)
        {
            MemoryStream buf = new MemoryStream();
            ABCDataTypeWriter writer = new ABCDataTypeWriter(buf);

            writer.WriteU30Packed((uint)code.MetadataCount);

            foreach (string key in code.MetadataKeys)
            {
                writer.WriteU30Packed((uint)this.stringMarshal.GetIDFor(key));
                Dictionary<string, string> itemInfo = code.GetMetadata(key);
                writer.WriteU30Packed((uint)itemInfo.Count);
                foreach (string itemKey in itemInfo.Keys)
                {
                    writer.WriteU30Packed((uint)this.stringMarshal.GetIDFor(itemKey));
                    writer.WriteU30Packed((uint)this.stringMarshal.GetIDFor(itemInfo[itemKey]));
                }
            }

            writer.Close(); /* Closes the buffer */
            return buf.ToArray();
        }

        private byte[] GenerateMethodInfo()
        {
            MemoryStream buf = new MemoryStream();
            ABCDataTypeWriter writer = new ABCDataTypeWriter(buf);

            Method[] methods = this.methodMarshal.ToArray();
            writer.WriteU30Packed((uint)methods.Length);
            foreach (Method method in methods)
            {
                writer.WriteU30Packed((uint)method.ParamCount);

                if (method.ReturnType != null)
                {
                    writer.WriteU30Packed((uint)this.MultinameID(method.ReturnType));
                }
                else
                {
                    writer.WriteU30Packed(0);
                }

                using (IEnumerator<Multiname> i = method.ParamTypes)
                {
                    while (i.MoveNext())
                    {
                        writer.WriteU30Packed((uint)this.MultinameID(i.Current));
                    }
                }

                writer.WriteU30Packed((uint)this.stringMarshal.GetIDFor(method.Name));
                writer.WriteUI8((uint)method.Flags);

                if (method.HasOptionalArgs)
                {
                    /* ISSUE 9 */
                    throw new SWFModellerException(
                            SWFModellerError.UnimplementedFeature,
                            "Optional arguments in methods.");
                }

                if (method.HasParamNames)
                {
                    /* ISSUE 12 */
                    throw new SWFModellerException(
                            SWFModellerError.UnimplementedFeature,
                            "Named method parameters");
                }
            }

            writer.Close(); /* Closes the buffer */
            return buf.ToArray();
        }

        /// <summary>
        /// Writes out the constants to the SWF file. This will also re-set the tables in the
        /// code object, so be sure you don't have anything in there that you need.
        /// </summary>
        /// <param name="writer">Where to write the constants to.</param>
        /// <param name="code">The code object with the tables in it, that we'll
        /// overwrite.</param>
        private void WriteConstantPool(ABCDataTypeWriter writer, AbcCode code)
        {
            /* Integer constants */
            int[] ints = this.intMarshal.ToArray();
            writer.WriteU30Packed((uint)(ints.Length == 1 ? 0 : ints.Length));
            for (int i = 1; i < ints.Length; i++) /* Omit value at [0] */
            {
#if DEBUG
                this.writeLog.AppendLine("Const int #" + i + ": " + ints[i]);
#endif

                writer.WriteSI32(ints[i]);
            }

            code.IntConsts = ints;

            /* Unsigned integer constants */
            uint[] uints = this.uintMarshal.ToArray();
            writer.WriteU30Packed((uint)(uints.Length == 1 ? 0 : uints.Length));
            for (int i = 1; i < uints.Length; i++) /* Omit value at [0] */
            {
#if DEBUG
                this.writeLog.AppendLine("Const uint #" + i + ": " + uints[i]);
#endif
                writer.WriteUI32(uints[i]);
            }

            code.UIntConsts = uints;

            /* Double constants */
            ulong[] doubles = this.doubleMarshal.ToArray();
            writer.WriteU30Packed((uint)(doubles.Length == 1 ? 0 : doubles.Length));
            for (int i = 1; i < doubles.Length; i++) /* Omit value at [0] */
            {
#if DEBUG
                this.writeLog.AppendLine("Const double (bits) #" + i + ": " + doubles[i]);
#endif
                /* We hack this here instead of having a U64 type, because it's a hack around not
                 * treating double properly. There's no such thing as U64 in SWF. */
                ulong d = doubles[i];
                uint low = (uint)(d & 0x00000000FFFFFFFF);
                d >>= 32;
                uint high = (uint)(d & 0x00000000FFFFFFFF);

                writer.WriteUI32(high);
                writer.WriteUI32(low);
            }

            code.DoubleConsts = doubles;

            /* String constants */
            string[] strings = this.stringMarshal.ToArray();
            writer.WriteU30Packed((uint)(strings.Length == 1 ? 0 : strings.Length));
            for (int i = 1; i < strings.Length; i++) /* Omit value at [0] */
            {
#if DEBUG
                this.writeLog.AppendLine("Const string #" + i + ": \"" + strings[i] + "\"");
#endif
                writer.WriteString(strings[i]);
            }

            code.StringConsts = strings;

            /* Namespace constants */
            Namespace[] namespaces = this.nsMarshal.ToArray();
            writer.WriteU30Packed((uint)(namespaces.Length == 1 ? 0 : namespaces.Length));
            for (int i = 1; i < namespaces.Length; i++) /* Omit value at [0] */
            {
#if DEBUG
                this.writeLog.AppendLine("Const ns #" + i + ": " + namespaces[i]);
#endif
                Namespace ns = namespaces[i];
                writer.WriteUI8((uint)ns.Kind);
                writer.WriteU30Packed((uint)this.stringMarshal.GetExistingIDFor(ns.Name));
            }

            code.SetNamespaces(namespaces);

            /* Namespace set constants */
            NamespaceSet[] namespaceSets = this.nsSetMarshal.ToArray();
            writer.WriteU30Packed((uint)(namespaceSets.Length == 1 ? 0 : namespaceSets.Length));
            for (int i = 1; i < namespaceSets.Length; i++) /* Omit value at [0] */
            {
#if DEBUG
                this.writeLog.AppendLine("Const ns set #" + i + ": " + namespaceSets[i]);
#endif
                NamespaceSet nss = namespaceSets[i];

                writer.WriteU30Packed((uint)nss.Count);

                foreach (Namespace ns in nss)
                {
                    writer.WriteU30Packed((uint)this.nsMarshal.GetExistingIDFor(ns));
                }
            }

            code.SetNamespaceSets(namespaceSets);

            /* Multiname constants */
            Multiname[] multinames = this.multinameMarshal.ToArray();
            writer.WriteU30Packed((uint)(multinames.Length == 1 ? 0 : multinames.Length));
            for (int i = 1; i < multinames.Length; i++) /* Omit value at [0] */
            {
#if DEBUG
                this.writeLog.AppendLine("Const mn set #" + i + ": " + multinames[i]);
#endif
                Multiname mn = multinames[i];

                writer.WriteUI8((uint)mn.Kind);

                switch (mn.Kind)
                {
                    case Multiname.MultinameKind.QName:
                    case Multiname.MultinameKind.QNameA:
                        uint nsIdx = (uint)this.nsMarshal.GetExistingIDFor(mn.NS);
                        uint nameIdx = (uint)this.stringMarshal.GetExistingIDFor(mn.Name);
                        writer.WriteU30Packed(nsIdx);
                        writer.WriteU30Packed(nameIdx);
                        break;

                    case Multiname.MultinameKind.RTQName:
                    case Multiname.MultinameKind.RTQNameA:
                        writer.WriteU30Packed((uint)this.stringMarshal.GetExistingIDFor(mn.Name));
                        break;

                    case Multiname.MultinameKind.RTQNameL:
                    case Multiname.MultinameKind.RTQNameLA:
                        /* No data */
                        break;

                    case Multiname.MultinameKind.Multiname:
                    case Multiname.MultinameKind.MultinameA:
                        writer.WriteU30Packed((uint)this.stringMarshal.GetExistingIDFor(mn.Name));
                        writer.WriteU30Packed((uint)this.nsSetMarshal.GetExistingIDFor(mn.Set));
                        break;

                    case Multiname.MultinameKind.MultinameL:
                    case Multiname.MultinameKind.MultinameLA:
                        writer.WriteU30Packed((uint)this.nsSetMarshal.GetExistingIDFor(mn.Set));
                        break;

                    default:
                        break;
                }
            }

            code.SetMultinames(multinames);
        }

        private void ReBuildTables(AbcCode code, string mainClassName)
        {
            /* These objects will keep track of the new IDs generated for all sorts of things... */

            this.intMarshal = new IDMarshaller<int>(0, 0);
            this.uintMarshal = new IDMarshaller<uint>(0, 0);
            this.stringMarshal = new IDMarshaller<string>(0, ABCValues.AnyName);
            this.doubleMarshal = new IDMarshaller<ulong>(0, 0L);
            this.nsMarshal = new IDMarshaller<Namespace>(0, Namespace.GlobalNS);
            this.nsSetMarshal = new IDMarshaller<NamespaceSet>(0, NamespaceSet.EmptySet);
            this.multinameMarshal = new IDMarshaller<Multiname>(0, Multiname.GlobalMultiname);
            this.classMarshal = new IDMarshaller<AS3ClassDef>(0);
            this.methodMarshal = new IDMarshaller<Method>(0);

            AS3ClassDef mainClass = null;
            foreach (AS3ClassDef clazz in code.Classes)
            {
                if (clazz.Name.QualifiedName == mainClassName && mainClassName != null)
                {
                    /* To make sure the main class is last.
                     *
                     * Note that we do this out of paranoia and observation, not out of
                     * any kind of understanding that it's necessary. As far as I know, it
                     * probably doesn't matter.
                     *
                     * Note that even without the check for the main class, we'd still need to take
                     * all the classes and register them in the marshal.
                     */
                    mainClass = clazz;
                }
                else
                {
                    this.classMarshal.Register(clazz);
                }
            }

            if (mainClass != null)
            {
                this.classMarshal.Register(mainClass);
            }

            code.SetClasses(this.classMarshal.ToArray());

            foreach (AS3ClassDef clazz in code.Classes)
            {
                this.ProcessClass(clazz);
            }

            foreach (Script s in code.Scripts)
            {
                this.AssembleMethod(s.Method);
                using (IEnumerator<Trait> i = s.Traits)
                {
                    while (i.MoveNext())
                    {
                        this.ProcessTrait(i.Current);
                    }
                }
            }

            code.SetMethods(this.methodMarshal.ToArray());
        }

        private void ProcessClass(AS3ClassDef clazz)
        {
            using (IEnumerator<Trait> i = clazz.ClassTraits)
            {
                while (i.MoveNext())
                {
                    this.ProcessTrait(i.Current);
                }
            }

            using (IEnumerator<Trait> i = clazz.InstanceTraits)
            {
                while (i.MoveNext())
                {
                    this.ProcessTrait(i.Current);
                }
            }

            if (clazz.Iinit != null)
            {
                this.AssembleMethod(clazz.Iinit);
            }

            if (clazz.Cinit != null)
            {
                this.AssembleMethod(clazz.Cinit);
            }
        }

        /// <summary>
        /// This is just for processing whilst rebuilding tables, it doesn't
        /// serialize anything.
        /// </summary>
        /// <param name="t">The trait to process.</param>
        private void ProcessTrait(Trait t)
        {
            switch (t.Kind)
            {
                case TraitKind.Method:
                    Method m = ((MethodTrait)t).Fn;
                    this.AssembleMethod(m);
                    break;

                case TraitKind.Slot:
                    this.ProcessSlot((SlotTrait)t);
                    break;

                case TraitKind.Class:
                    /* Skip this. It will already have been picked up in the class
                     * list in ReBuildTables */
                    break;

                case TraitKind.Getter:
                case TraitKind.Setter:
                case TraitKind.Function:
                case TraitKind.Const:
                default:
                    /* ISSUE 73 */
                    throw new SWFModellerException(
                            SWFModellerError.UnimplementedFeature,
                            "Unsupported assembly of trait kind " + t.Kind.ToString());
            }
        }

        private void ProcessSlot(SlotTrait slotTrait)
        {
            switch (slotTrait.ValKind)
            {
                case ConstantKind.ConInt:
                    this.intMarshal.Register((int)slotTrait.Val);
                    break;

                case ConstantKind.ConUInt:
                    this.uintMarshal.Register((uint)slotTrait.Val);
                    break;

                case ConstantKind.ConDouble:
                    this.doubleMarshal.Register((ulong)slotTrait.Val);
                    break;

                case ConstantKind.ConUtf8:
                    this.stringMarshal.Register((string)slotTrait.Val);
                    break;

                case ConstantKind.ConTrue:
                case ConstantKind.ConFalse:
                case ConstantKind.ConNull:
                case ConstantKind.ConUndefined:
                    /* We need not do anything with these universal constants. */
                    break;

                case ConstantKind.ConNamespace:
                case ConstantKind.ConPackageNamespace:
                case ConstantKind.ConPackageInternalNs:
                case ConstantKind.ConProtectedNamespace:
                case ConstantKind.ConExplicitNamespace:
                case ConstantKind.ConStaticProtectedNs:
                case ConstantKind.ConPrivateNs:
                    /*(void)*/this.NamespaceID((Namespace)slotTrait.Val);
                    break;

                default:
                    /* ISSUE 73 */
                    throw new SWFModellerException(
                            SWFModellerError.UnimplementedFeature,
                            "Unsupported slot value kind " + slotTrait.ValKind.ToString());
            }
        }

        /// <summary>
        /// Gets an ID for a multiname, ensuring that all other dependencies of the multiname
        /// are themselves registered with IDs.
        /// </summary>
        /// <param name="mn">The multiname</param>
        /// <returns>The ID of the multiname</returns>
        private int MultinameID(Multiname mn)
        {
            this.stringMarshal.Register(mn.Name);

            if (mn.NS != null)
            {
                /*(void)*/this.NamespaceID(mn.NS);
            }

            if (mn.Set != null)
            {
                /*(void)*/this.NamespaceSetID(mn.Set);
            }

            return this.multinameMarshal.GetIDFor(mn);
        }

        /// <summary>
        /// Gets an ID for a namespace, ensuring that all other dependencies of the namespace
        /// are themselves registered with IDs.
        /// </summary>
        /// <param name="ns">The namespace</param>
        /// <returns>The ID of the namespace</returns>
        private int NamespaceID(Namespace ns)
        {
            this.stringMarshal.Register(ns.Name);
            return this.nsMarshal.GetIDFor(ns);
        }

        /// <summary>
        /// Gets an ID for a namespace set, ensuring that all other dependencies of the namespace set
        /// are themselves registered with IDs.
        /// </summary>
        /// <param name="set">The namespace set</param>
        /// <returns>The ID of the namespace set</returns>
        private int NamespaceSetID(NamespaceSet set)
        {
            foreach (Namespace ns in set)
            {
                /*(void)*/this.NamespaceID(ns);
            }

            return this.nsSetMarshal.GetIDFor(set);
        }
    }
}
