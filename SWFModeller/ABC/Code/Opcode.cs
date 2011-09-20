//-----------------------------------------------------------------------
// Opcode.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC.Code
{
    using System.Collections.Generic;
    using System.IO;
    using System;
    using System.Text;
    using System.Diagnostics;

    /// <summary>
    /// A line of bytecode, disassembled from the source bytes.
    /// </summary>
    public class Opcode
    {
        /// <summary>
        /// Opcode argument types and their binary format within bytecode
        /// </summary>
        public enum ArgType
        {
            /* Please note that this array is closely associated with the
             * InternalTypes array, hence the defined enum values which map
             * into that array. If you update this, then you need to update
             * the other. */
            MultinameU30 = 0,
            OffsetS24 = 1, /* Will either be an Opcode, or an integer. An opcode needs resolved to an integer. */
            StringU30 = 2,
            RegisterU30 = 3,
            ObjectRegisterU30 = 4,
            PropertyRegisterU30 = 5,
            ByteU8 = 6,
            ShortU30 = 7,
            IntU30 = 8,
            UintU30 = 9,
            DoubleU30 = 10,
            NamespaceU30 = 11,
            MethodU30 = 12,
            CountU30 = 13,
            ClassU30 = 14,
            ExceptionU30 = 15,
            StackU8 = 16,
            SlotU30 = 17,
            DebugU8 = 18,
            DebugTypeU30 = 19,
            StringU8 = 20,
            LineNumberU30 = 21,
            ShortS30 = 22, /* The spec says U30 everywhere, but it often lies to you. Do not trust the spec. */
            ByteS8 = 23 /* As above. */
        }

        private static Type[] InternalTypes = new Type[]
        {
            typeof(Multiname), /* MultinameU30 = 0 */
            typeof(int), /* OffsetS24 = 1 */
            typeof(string), /* StringU30 = 2 */
            typeof(uint), /* RegisterU30 = 3 */
            typeof(uint), /* ObjectRegisterU30 = 4 */
            typeof(uint), /* PropertyRegisterU30 = 5 */
            typeof(byte), /* ByteU8 = 6 */
            typeof(ushort), /* ShortU30 = 7 */
            typeof(int), /* IntU30 = 8 */
            typeof(uint), /* UintU30 = 9 */
            typeof(ulong), /* DoubleU30 = 10 */
            typeof(Namespace), /* NamespaceU30 = 11 */
            typeof(Method), /* MethodU30 = 12 */
            typeof(uint), /* CountU30 = 13 */
            typeof(AS3ClassDef), /* ClassU30 = 14 */
            typeof(uint), /* ExceptionU30 = 15 */
            typeof(byte), /* StackU8 = 16 */
            typeof(uint), /* SlotU30 = 17 */
            typeof(byte), /* DebugU8 = 18 */
            typeof(uint), /* DebugTypeU30 = 19 */
            typeof(string), /* StringU8 = 20 */
            typeof(uint), /* LineNumberU30 = 21 */
            typeof(int), /* ShortS30 = 22 */
            typeof(int) /* ByteS8 = 23 ; Bytes are signed, so we store it as an int. Remember to check it. */
        };

        struct OpcodeDef
        {
            public ArgType[] Args;
            public Mnemonics Mnemonic;

            public override string ToString()
            {
                return this.Mnemonic.ToString();
            }
        }

        public enum Mnemonics
        {
            Bkpt = 0x01,
            Nop = 0x02,
            Throw = 0x03,
            GetSuper = 0x04,
            SetSuper = 0x05,
            Dxns = 0x06,
            DxnsLate = 0x07,
            Kill = 0x08,
            Label = 0x09,
            IfNlt = 0x0C,
            IfNle = 0x0D,
            IfNgt = 0x0E,
            IfNge = 0x0F,
            Jump = 0x10,
            IfTrue = 0x11,
            IfFalse = 0x12,
            IfEq = 0x13,
            IfNe = 0x14,
            IfLt = 0x15,
            IfLe = 0x16,
            IfGt = 0x17,
            IfGe = 0x18,
            IfStrictEq = 0x19,
            IfStrictNe = 0x1A,
            LookupSwitch = 0x1B,
            PushWith = 0x1C,
            PopScope = 0x1D,
            NextName = 0x1E,
            HasNext = 0x1F,
            PushNull = 0x20,
            PushUndefined = 0x21,
            NextValue = 0x23,
            PushByte = 0x24,
            PushShort = 0x25,
            PushTrue = 0x26,
            PushFalse = 0x27,
            PushNaN = 0x28,
            Pop = 0x29,
            Dup = 0x2A,
            Swap = 0x2B,
            PushString = 0x2C,
            PushInt = 0x2D,
            PushUInt = 0x2E,
            PushDouble = 0x2F,
            PushScope = 0x30,
            PushNamespace = 0x31,
            HasNext2 = 0x32,
            NewFunction = 0x40,
            Call = 0x41,
            Construct = 0x42,
            CallMethod = 0x43,
            CallStatic = 0x44,
            CallSuper = 0x45,
            CallProperty = 0x46,
            ReturnVoid = 0x47,
            ReturnValue = 0x48,
            ConstructSuper = 0x49,
            ConstructProp = 0x4A,
            CallSuperID = 0x4B,
            CallPropLex = 0x4C,
            CallInterface = 0x4D,
            CallSuperVoid = 0x4E,
            CallPropVoid = 0x4F,
            NewObject = 0x55,
            NewArray = 0x56,
            NewActivation = 0x57,
            NewClass = 0x58,
            GetDescendants = 0x59,
            NewCatch = 0x5A,
            FindPropStrict = 0x5D,
            FindProperty = 0x5E,
            FindDef = 0x5F,
            GetLex = 0x60,
            SetProperty = 0x61,
            GetLocal = 0x62,
            SetLocal = 0x63,
            GetGlobalScope = 0x64,
            GetScopeObject = 0x65,
            GetProperty = 0x66,
            InitProperty = 0x68,
            DeleteProperty = 0x6A,
            GetSlot = 0x6C,
            SetSlot = 0x6D,
            GetGlobalSlot = 0x6E,
            SetGlobalSlot = 0x6F,
            ConvertS = 0x70,
            EscXElem = 0x71,
            EscXAttr = 0x72,
            ConvertI = 0x73,
            ConvertU = 0x74,
            ConvertD = 0x75,
            ConvertB = 0x76,
            ConvertO = 0x77,
            CheckFilter = 0x78,
            Coerce = 0x80,
            CoerceB = 0x81,
            CoerceA = 0x82,
            CoerceI = 0x83,
            CoerceD = 0x84,
            CoerceS = 0x85,
            AsType = 0x86,
            AsTypeLate = 0x87,
            CoerceU = 0x88,
            CoerceO = 0x89,
            Negate = 0x90,
            Increment = 0x91,
            IncLocal = 0x92,
            Decrement = 0x93,
            DecLocal = 0x94,
            TypeOf = 0x95,
            Not = 0x96,
            BitNot = 0x97,
            Concat = 0x9A,
            AddD = 0x9B,
            Add = 0xA0,
            Subtract = 0xA1,
            Multiply = 0xA2,
            Divide = 0xA3,
            Modulo = 0xA4,
            LShift = 0xA5,
            RShift = 0xA6,
            URShift = 0xA7,
            BitAnd = 0xA8,
            BitOr = 0xA9,
            BitXor = 0xAA,
            Equals = 0xAB,
            StrictEquals = 0xAC,
            LessThan = 0xAD,
            LessEquals = 0xAE,
            GreaterThan = 0xAF,
            GreaterEquals = 0xB0,
            InstanceOf = 0xB1,
            IsType = 0xB2,
            IsTypeLate = 0xB3,
            In = 0xB4,
            IncrementI = 0xC0,
            DecrementI = 0xC1,
            IncLocalI = 0xC2,
            DecLocalI = 0xC3,
            NegateI = 0xC4,
            AddI = 0xC5,
            SubtractI = 0xC6,
            MultiplyI = 0xC7,
            GetLocal0 = 0xD0,
            GetLocal1 = 0xD1,
            GetLocal2 = 0xD2,
            GetLocal3 = 0xD3,
            SetLocal0 = 0xD4,
            SetLocal1 = 0xD5,
            SetLocal2 = 0xD6,
            SetLocal3 = 0xD7,
            AbsJump = 0xEE,
            Debug = 0xEF,
            DebugLine = 0xF0,
            DebugFile = 0xF1,
            BkptLine = 0xF2,
            Timestamp = 0xF3,
            VerifyPass = 0xF5,
            Alloc = 0xF6,
            Mark = 0xF7,
            Wb = 0xF8,
            Prologue = 0xF9,
            SendEnter = 0xFA,
            DoubleToAtom = 0xFB,
            Sweep = 0xFC,
            CodeGenOp = 0xFD,
            VerifyOp = 0xFE,
            Decode = 0xFF,
        }

        private static OpcodeDef?[] OpcodeTable = new OpcodeDef?[]
        {
            /* 0x00 */ null,
            /* 0x01 */ new OpcodeDef{ Mnemonic = Mnemonics.Bkpt }, /* Not in the specification */
            /* 0x02 */ new OpcodeDef{ Mnemonic = Mnemonics.Nop },
            /* 0x03 */ new OpcodeDef{ Mnemonic = Mnemonics.Throw },
            /* 0x04 */ new OpcodeDef{ Mnemonic = Mnemonics.GetSuper,       Args=new ArgType[] { ArgType.MultinameU30 }},
            /* 0x05 */ new OpcodeDef{ Mnemonic = Mnemonics.SetSuper,       Args=new ArgType[] { ArgType.MultinameU30 }},
            /* 0x06 */ new OpcodeDef{ Mnemonic = Mnemonics.Dxns,           Args=new ArgType[] { ArgType.StringU30 }},
            /* 0x07 */ new OpcodeDef{ Mnemonic = Mnemonics.DxnsLate },
            /* 0x08 */ new OpcodeDef{ Mnemonic = Mnemonics.Kill,           Args=new ArgType[] { ArgType.RegisterU30 }},
            /* 0x09 */ new OpcodeDef{ Mnemonic = Mnemonics.Label },
            /* 0x0A-0x0B */ null, null,
            /* 0x0C */ new OpcodeDef{ Mnemonic = Mnemonics.IfNlt,          Args=new ArgType[] { ArgType.OffsetS24 }},
            /* 0x0D */ new OpcodeDef{ Mnemonic = Mnemonics.IfNle,          Args=new ArgType[] { ArgType.OffsetS24 }},
            /* 0x0E */ new OpcodeDef{ Mnemonic = Mnemonics.IfNgt,          Args=new ArgType[] { ArgType.OffsetS24 }},
            /* 0x0F */ new OpcodeDef{ Mnemonic = Mnemonics.IfNge,          Args=new ArgType[] { ArgType.OffsetS24 }},
            /* 0x10 */ new OpcodeDef{ Mnemonic = Mnemonics.Jump,           Args=new ArgType[] { ArgType.OffsetS24 }},
            /* 0x11 */ new OpcodeDef{ Mnemonic = Mnemonics.IfTrue,         Args=new ArgType[] { ArgType.OffsetS24 }},
            /* 0x12 */ new OpcodeDef{ Mnemonic = Mnemonics.IfFalse,        Args=new ArgType[] { ArgType.OffsetS24 }},
            /* 0x13 */ new OpcodeDef{ Mnemonic = Mnemonics.IfEq,           Args=new ArgType[] { ArgType.OffsetS24 }},
            /* 0x14 */ new OpcodeDef{ Mnemonic = Mnemonics.IfNe,           Args=new ArgType[] { ArgType.OffsetS24 }},
            /* 0x15 */ new OpcodeDef{ Mnemonic = Mnemonics.IfLt,           Args=new ArgType[] { ArgType.OffsetS24 }},
            /* 0x16 */ new OpcodeDef{ Mnemonic = Mnemonics.IfLe,           Args=new ArgType[] { ArgType.OffsetS24 }},
            /* 0x17 */ new OpcodeDef{ Mnemonic = Mnemonics.IfGt,           Args=new ArgType[] { ArgType.OffsetS24 }},
            /* 0x18 */ new OpcodeDef{ Mnemonic = Mnemonics.IfGe,           Args=new ArgType[] { ArgType.OffsetS24 }},
            /* 0x19 */ new OpcodeDef{ Mnemonic = Mnemonics.IfStrictEq,     Args=new ArgType[] { ArgType.OffsetS24 }},
            /* 0x1A */ new OpcodeDef{ Mnemonic = Mnemonics.IfStrictNe,     Args=new ArgType[] { ArgType.OffsetS24 }},
            /* 0x1B */ new OpcodeDef{ Mnemonic = Mnemonics.LookupSwitch }, /* This has values S24,U30,S24... but are variable, so needs to be treated as a special case */
            /* 0x1C */ new OpcodeDef{ Mnemonic = Mnemonics.PushWith },
            /* 0x1D */ new OpcodeDef{ Mnemonic = Mnemonics.PopScope },
            /* 0x1E */ new OpcodeDef{ Mnemonic = Mnemonics.NextName },
            /* 0x1F */ new OpcodeDef{ Mnemonic = Mnemonics.HasNext },
            /* 0x20 */ new OpcodeDef{ Mnemonic = Mnemonics.PushNull },
            /* 0x21 */ new OpcodeDef{ Mnemonic = Mnemonics.PushUndefined },
            /* 0x22 */ null,
            /* 0x23 */ new OpcodeDef{ Mnemonic = Mnemonics.NextValue },
            /* 0x24 */ new OpcodeDef{ Mnemonic = Mnemonics.PushByte,       Args=new ArgType[] { ArgType.ByteS8 }},
            /* 0x25 */ new OpcodeDef{ Mnemonic = Mnemonics.PushShort,      Args=new ArgType[] { ArgType.ShortS30 }},
            /* 0x26 */ new OpcodeDef{ Mnemonic = Mnemonics.PushTrue },
            /* 0x27 */ new OpcodeDef{ Mnemonic = Mnemonics.PushFalse },
            /* 0x28 */ new OpcodeDef{ Mnemonic = Mnemonics.PushNaN },
            /* 0x29 */ new OpcodeDef{ Mnemonic = Mnemonics.Pop },
            /* 0x2A */ new OpcodeDef{ Mnemonic = Mnemonics.Dup },
            /* 0x2B */ new OpcodeDef{ Mnemonic = Mnemonics.Swap },
            /* 0x2C */ new OpcodeDef{ Mnemonic = Mnemonics.PushString,     Args=new ArgType[] { ArgType.StringU30 } },
            /* 0x2D */ new OpcodeDef{ Mnemonic = Mnemonics.PushInt,        Args=new ArgType[] { ArgType.IntU30 } },
            /* 0x2E */ new OpcodeDef{ Mnemonic = Mnemonics.PushUInt,       Args=new ArgType[] { ArgType.UintU30 } },
            /* 0x2F */ new OpcodeDef{ Mnemonic = Mnemonics.PushDouble,     Args=new ArgType[] { ArgType.DoubleU30 } },
            /* 0x30 */ new OpcodeDef{ Mnemonic = Mnemonics.PushScope },
            /* 0x31 */ new OpcodeDef{ Mnemonic = Mnemonics.PushNamespace },
            /* 0x32 */ new OpcodeDef{ Mnemonic = Mnemonics.HasNext2 },
            /* 0x33-0x3F */ null, null, null, null, null, null, null, null, null, null, null, null, null,
            /* 0x40 */ new OpcodeDef{ Mnemonic = Mnemonics.NewFunction,    Args=new ArgType[] { ArgType.MethodU30 } },
            /* 0x41 */ new OpcodeDef{ Mnemonic = Mnemonics.Call,           Args=new ArgType[] { ArgType.CountU30 } },
            /* 0x42 */ new OpcodeDef{ Mnemonic = Mnemonics.Construct,      Args=new ArgType[] { ArgType.CountU30 } },
            /* 0x43 */ new OpcodeDef{ Mnemonic = Mnemonics.CallMethod,     Args=new ArgType[] { ArgType.MethodU30, ArgType.CountU30 } },
            /* 0x44 */ new OpcodeDef{ Mnemonic = Mnemonics.CallStatic,     Args=new ArgType[] { ArgType.MethodU30, ArgType.CountU30 } },
            /* 0x45 */ new OpcodeDef{ Mnemonic = Mnemonics.CallSuper,      Args=new ArgType[] { ArgType.MultinameU30, ArgType.CountU30 } },
            /* 0x46 */ new OpcodeDef{ Mnemonic = Mnemonics.CallProperty,   Args=new ArgType[] { ArgType.MultinameU30, ArgType.CountU30 } },
            /* 0x47 */ new OpcodeDef{ Mnemonic = Mnemonics.ReturnVoid },
            /* 0x48 */ new OpcodeDef{ Mnemonic = Mnemonics.ReturnValue },
            /* 0x49 */ new OpcodeDef{ Mnemonic = Mnemonics.ConstructSuper, Args=new ArgType[] { ArgType.CountU30 } },
            /* 0x4A */ new OpcodeDef{ Mnemonic = Mnemonics.ConstructProp,  Args=new ArgType[] { ArgType.MultinameU30, ArgType.CountU30 } },
            /* 0x4B */ new OpcodeDef{ Mnemonic = Mnemonics.CallSuperID },   /* Not in the specification */
            /* 0x4C */ new OpcodeDef{ Mnemonic = Mnemonics.CallPropLex,    Args=new ArgType[] { ArgType.MultinameU30, ArgType.CountU30 } },
            /* 0x4D */ new OpcodeDef{ Mnemonic = Mnemonics.CallInterface }, /* Not in the specification */
            /* 0x4E */ new OpcodeDef{ Mnemonic = Mnemonics.CallSuperVoid,  Args=new ArgType[] { ArgType.MultinameU30, ArgType.CountU30 } },
            /* 0x4F */ new OpcodeDef{ Mnemonic = Mnemonics.CallPropVoid,   Args=new ArgType[] { ArgType.MultinameU30, ArgType.CountU30 } },
            /* 0x50-0x54 */null, null, null, null, null,
            /* 0x55 */ new OpcodeDef{ Mnemonic = Mnemonics.NewObject,      Args=new ArgType[] { ArgType.CountU30 } },
            /* 0x56 */ new OpcodeDef{ Mnemonic = Mnemonics.NewArray,       Args=new ArgType[] { ArgType.CountU30 } },
            /* 0x57 */ new OpcodeDef{ Mnemonic = Mnemonics.NewActivation },
            /* 0x58 */ new OpcodeDef{ Mnemonic = Mnemonics.NewClass,       Args=new ArgType[] { ArgType.ClassU30 } },
            /* 0x59 */ new OpcodeDef{ Mnemonic = Mnemonics.GetDescendants, Args=new ArgType[] { ArgType.MultinameU30 } },
            /* 0x5A */ new OpcodeDef{ Mnemonic = Mnemonics.NewCatch,       Args=new ArgType[] { ArgType.ExceptionU30 } },
            /* 0x5B-0x5C */ null, null,
            /* 0x5D */ new OpcodeDef{ Mnemonic = Mnemonics.FindPropStrict, Args=new ArgType[] { ArgType.MultinameU30 } },
            /* 0x5E */ new OpcodeDef{ Mnemonic = Mnemonics.FindProperty,   Args=new ArgType[] { ArgType.MultinameU30 } },
            /* 0x5F */ new OpcodeDef{ Mnemonic = Mnemonics.FindDef }, /* Not in the specification */
            /* 0x60 */ new OpcodeDef{ Mnemonic = Mnemonics.GetLex,   Args=new ArgType[] { ArgType.MultinameU30 } },
            /* 0x61 */ new OpcodeDef{ Mnemonic = Mnemonics.SetProperty,   Args=new ArgType[] { ArgType.MultinameU30 } },
            /* 0x62 */ new OpcodeDef{ Mnemonic = Mnemonics.GetLocal,   Args=new ArgType[] { ArgType.RegisterU30 } },
            /* 0x63 */ new OpcodeDef{ Mnemonic = Mnemonics.SetLocal,   Args=new ArgType[] { ArgType.RegisterU30 } },
            /* 0x64 */ new OpcodeDef{ Mnemonic = Mnemonics.GetGlobalScope },
            /* 0x65 */ new OpcodeDef{ Mnemonic = Mnemonics.GetScopeObject,   Args=new ArgType[] { ArgType.StackU8 } },
            /* 0x66 */ new OpcodeDef{ Mnemonic = Mnemonics.GetProperty,   Args=new ArgType[] { ArgType.MultinameU30 } },
            /* 0x67 */ null,
            /* 0x68 */ new OpcodeDef{ Mnemonic = Mnemonics.InitProperty,   Args=new ArgType[] { ArgType.MultinameU30 } },
            /* 0x69 */ null,
            /* 0x6A */ new OpcodeDef{ Mnemonic = Mnemonics.DeleteProperty,   Args=new ArgType[] { ArgType.MultinameU30 } },
            /* 0x6B */ null,
            /* 0x6C */ new OpcodeDef{ Mnemonic = Mnemonics.GetSlot,   Args=new ArgType[] { ArgType.SlotU30 } },
            /* 0x6D */ new OpcodeDef{ Mnemonic = Mnemonics.SetSlot,   Args=new ArgType[] { ArgType.SlotU30 } },
            /* 0x6E */ new OpcodeDef{ Mnemonic = Mnemonics.GetGlobalSlot,   Args=new ArgType[] { ArgType.SlotU30 } },
            /* 0x6F */ new OpcodeDef{ Mnemonic = Mnemonics.SetGlobalSlot,   Args=new ArgType[] { ArgType.SlotU30 } },
            /* 0x70 */ new OpcodeDef{ Mnemonic = Mnemonics.ConvertS },
            /* 0x71 */ new OpcodeDef{ Mnemonic = Mnemonics.EscXElem },
            /* 0x72 */ new OpcodeDef{ Mnemonic = Mnemonics.EscXAttr },
            /* 0x73 */ new OpcodeDef{ Mnemonic = Mnemonics.ConvertI },
            /* 0x74 */ new OpcodeDef{ Mnemonic = Mnemonics.ConvertU },
            /* 0x75 */ new OpcodeDef{ Mnemonic = Mnemonics.ConvertD },
            /* 0x76 */ new OpcodeDef{ Mnemonic = Mnemonics.ConvertB },
            /* 0x77 */ new OpcodeDef{ Mnemonic = Mnemonics.ConvertO },
            /* 0x78 */ new OpcodeDef{ Mnemonic = Mnemonics.CheckFilter },
            /* 0x79-0x7F */ null, null, null, null, null, null, null,
            /* 0x80 */ new OpcodeDef{ Mnemonic = Mnemonics.Coerce,   Args=new ArgType[] { ArgType.MultinameU30 } },
            /* 0x81 */ new OpcodeDef{ Mnemonic = Mnemonics.CoerceB }, /* Not in the specification */
            /* 0x82 */ new OpcodeDef{ Mnemonic = Mnemonics.CoerceA },
            /* 0x83 */ new OpcodeDef{ Mnemonic = Mnemonics.CoerceI }, /* Not in the specification */
            /* 0x84 */ new OpcodeDef{ Mnemonic = Mnemonics.CoerceD }, /* Not in the specification */
            /* 0x85 */ new OpcodeDef{ Mnemonic = Mnemonics.CoerceS },
            /* 0x86 */ new OpcodeDef{ Mnemonic = Mnemonics.AsType,   Args=new ArgType[] { ArgType.MultinameU30 } },
            /* 0x87 */ new OpcodeDef{ Mnemonic = Mnemonics.AsTypeLate },
            /* 0x88 */ new OpcodeDef{ Mnemonic = Mnemonics.CoerceU }, /* Not in the specification */
            /* 0x89 */ new OpcodeDef{ Mnemonic = Mnemonics.CoerceO }, /* Not in the specification */
            /* 0x8A-0x8F */ null, null, null, null, null, null,
            /* 0x90 */ new OpcodeDef{ Mnemonic = Mnemonics.Negate },
            /* 0x91 */ new OpcodeDef{ Mnemonic = Mnemonics.Increment },
            /* 0x92 */ new OpcodeDef{ Mnemonic = Mnemonics.IncLocal,   Args=new ArgType[] { ArgType.RegisterU30 } },
            /* 0x93 */ new OpcodeDef{ Mnemonic = Mnemonics.Decrement },
            /* 0x94 */ new OpcodeDef{ Mnemonic = Mnemonics.DecLocal,   Args=new ArgType[] { ArgType.RegisterU30 } },
            /* 0x95 */ new OpcodeDef{ Mnemonic = Mnemonics.TypeOf },
            /* 0x96 */ new OpcodeDef{ Mnemonic = Mnemonics.Not },
            /* 0x97 */ new OpcodeDef{ Mnemonic = Mnemonics.BitNot },
            /* 0x98-0x99 */ null, null,
            /* 0x9A */ new OpcodeDef{ Mnemonic = Mnemonics.Concat }, /* Not in the specification */
            /* 0x9B */ new OpcodeDef{ Mnemonic = Mnemonics.AddD }, /* Not in the specification */
            /* 0x9C-0x9F */ null, null, null, null,
            /* 0xA0 */ new OpcodeDef{ Mnemonic = Mnemonics.Add },
            /* 0xA1 */ new OpcodeDef{ Mnemonic = Mnemonics.Subtract },
            /* 0xA2 */ new OpcodeDef{ Mnemonic = Mnemonics.Multiply },
            /* 0xA3 */ new OpcodeDef{ Mnemonic = Mnemonics.Divide },
            /* 0xA4 */ new OpcodeDef{ Mnemonic = Mnemonics.Modulo },
            /* 0xA5 */ new OpcodeDef{ Mnemonic = Mnemonics.LShift },
            /* 0xA6 */ new OpcodeDef{ Mnemonic = Mnemonics.RShift },
            /* 0xA7 */ new OpcodeDef{ Mnemonic = Mnemonics.URShift },
            /* 0xA8 */ new OpcodeDef{ Mnemonic = Mnemonics.BitAnd },
            /* 0xA9 */ new OpcodeDef{ Mnemonic = Mnemonics.BitOr },
            /* 0xAA */ new OpcodeDef{ Mnemonic = Mnemonics.BitXor },
            /* 0xAB */ new OpcodeDef{ Mnemonic = Mnemonics.Equals },
            /* 0xAC */ new OpcodeDef{ Mnemonic = Mnemonics.StrictEquals },
            /* 0xAD */ new OpcodeDef{ Mnemonic = Mnemonics.LessThan },
            /* 0xAE */ new OpcodeDef{ Mnemonic = Mnemonics.LessEquals },
            /* 0xAF */ new OpcodeDef{ Mnemonic = Mnemonics.GreaterThan },
            /* 0xB0 */ new OpcodeDef{ Mnemonic = Mnemonics.GreaterEquals },
            /* 0xB1 */ new OpcodeDef{ Mnemonic = Mnemonics.InstanceOf },
            /* 0xB2 */ new OpcodeDef{ Mnemonic = Mnemonics.IsType,   Args=new ArgType[] { ArgType.MultinameU30 } },
            /* 0xB3 */ new OpcodeDef{ Mnemonic = Mnemonics.IsTypeLate },
            /* 0xB4 */ new OpcodeDef{ Mnemonic = Mnemonics.In },
            /* 0xB5-0xBF */ null, null, null, null, null, null, null, null, null, null, null,
            /* 0xC0 */ new OpcodeDef{ Mnemonic = Mnemonics.IncrementI },
            /* 0xC1 */ new OpcodeDef{ Mnemonic = Mnemonics.DecrementI },
            /* 0xC2 */ new OpcodeDef{ Mnemonic = Mnemonics.IncLocalI,   Args=new ArgType[] { ArgType.RegisterU30 } },
            /* 0xC3 */ new OpcodeDef{ Mnemonic = Mnemonics.DecLocalI,   Args=new ArgType[] { ArgType.RegisterU30 } },
            /* 0xC4 */ new OpcodeDef{ Mnemonic = Mnemonics.NegateI },
            /* 0xC5 */ new OpcodeDef{ Mnemonic = Mnemonics.AddI },
            /* 0xC6 */ new OpcodeDef{ Mnemonic = Mnemonics.SubtractI },
            /* 0xC7 */ new OpcodeDef{ Mnemonic = Mnemonics.MultiplyI },
            /* 0xC8-0xCF */ null, null, null, null, null, null, null, null,
            /* 0xD0 */ new OpcodeDef{ Mnemonic = Mnemonics.GetLocal0 },
            /* 0xD1 */ new OpcodeDef{ Mnemonic = Mnemonics.GetLocal1 },
            /* 0xD2 */ new OpcodeDef{ Mnemonic = Mnemonics.GetLocal2 },
            /* 0xD3 */ new OpcodeDef{ Mnemonic = Mnemonics.GetLocal3 },
            /* 0xD4 */ new OpcodeDef{ Mnemonic = Mnemonics.SetLocal0 },
            /* 0xD5 */ new OpcodeDef{ Mnemonic = Mnemonics.SetLocal1 },
            /* 0xD6 */ new OpcodeDef{ Mnemonic = Mnemonics.SetLocal2 },
            /* 0xD7 */ new OpcodeDef{ Mnemonic = Mnemonics.SetLocal3 },
            /* 0xD8-0xED */ null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            /* 0xEE */ new OpcodeDef{ Mnemonic = Mnemonics.AbsJump }, /* Not in the specification */
            /* 0xEF */ new OpcodeDef{ Mnemonic = Mnemonics.Debug,   Args=new ArgType[] { ArgType.DebugU8, ArgType.DebugTypeU30, ArgType.StringU8, ArgType.RegisterU30 } },
            /* 0xF0 */ new OpcodeDef{ Mnemonic = Mnemonics.DebugLine,   Args=new ArgType[] { ArgType.LineNumberU30 } },
            /* 0xF1 */ new OpcodeDef{ Mnemonic = Mnemonics.DebugFile,   Args=new ArgType[] { ArgType.StringU30 } },
            /* 0xF2 */ new OpcodeDef{ Mnemonic = Mnemonics.BkptLine }, /* Not in the specification */
            /* 0xF3 */ new OpcodeDef{ Mnemonic = Mnemonics.Timestamp }, /* Not in the specification */
            /* 0xF4 */ null,
            /* 0xF5 */ new OpcodeDef{ Mnemonic = Mnemonics.VerifyPass }, /* Not in the specification */
            /* 0xF6 */ new OpcodeDef{ Mnemonic = Mnemonics.Alloc }, /* Not in the specification */
            /* 0xF7 */ new OpcodeDef{ Mnemonic = Mnemonics.Mark }, /* Not in the specification */
            /* 0xF8 */ new OpcodeDef{ Mnemonic = Mnemonics.Wb }, /* Not in the specification */
            /* 0xF9 */ new OpcodeDef{ Mnemonic = Mnemonics.Prologue }, /* Not in the specification */
            /* 0xFA */ new OpcodeDef{ Mnemonic = Mnemonics.SendEnter }, /* Not in the specification */
            /* 0xFB */ new OpcodeDef{ Mnemonic = Mnemonics.DoubleToAtom }, /* Not in the specification */
            /* 0xFC */ new OpcodeDef{ Mnemonic = Mnemonics.Sweep }, /* Not in the specification */
            /* 0xFD */ new OpcodeDef{ Mnemonic = Mnemonics.CodeGenOp }, /* Not in the specification */
            /* 0xFE */ new OpcodeDef{ Mnemonic = Mnemonics.VerifyOp }, /* Not in the specification */
            /* 0xFF */ new OpcodeDef{ Mnemonic = Mnemonics.Decode }, /* Not in the specification */
        };

        private AbcCode abc;

        public Opcode(AbcCode abc)
        {
            this.abc = abc;
        }

        public bool HasOffsets
        {
            get
            {
                if (this.Mnemonic == Mnemonics.LookupSwitch)
                {
                    return true; /* Oh switch, you special exception you. */
                }

                ArgType[] args = this.ArgTypes;

                if (args == null)
                {
                    return false;
                }

                foreach (ArgType t in args)
                {
                    if (t == ArgType.OffsetS24)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

#if(DEBUG)
        /// <summary>
        /// Set this so that it shows up in ToString on debug output, so show up
        /// jump targets properly.
        /// </summary>
        public int numberLabel = -1;
#endif

        public uint Instruction { get; private set; }
        public object[] Args { get; set; }
        public ArgType[] ArgTypes
        {
            get
            {
                OpcodeDef def = (OpcodeDef)OpcodeTable[this.Instruction];
                return def.Args;
            }
        }

        public Mnemonics Mnemonic
        {
            get
            {
                return (Mnemonics)this.Instruction;
            }

            set
            {
                this.Instruction = (uint)value;
            }
        }

        public static bool VerifyArgTypes(Mnemonics mnemonic, object[] args)
        {
            OpcodeDef? defRef = OpcodeTable[(uint)mnemonic];
            if (defRef == null)
            {
                return false;
            }

            OpcodeDef def = defRef.Value;

            if (def.Args == null)
            {
                return (args.Length == 0);
            }

            if (def.Args.Length != args.Length)
            {
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                Type expected = Opcode.InternalTypes[(int)def.Args[i]];
                Type passed = args[i].GetType();

                if (expected != passed)
                {
                    return false;
                }

                if (def.Args[i] == ArgType.ByteS8)
                {
                    if ((int)args[i] > 127 || (int)args[i] < -128)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Convenience method for debug file opcodes, which can be inserted sneakily at the
        /// last moment when serialising the bytecode, and so need to be created in this lightweight
        /// way.
        /// </summary>
        public static Opcode CreateDebugFile(string fileName)
        {
            Opcode op = new Opcode(null);
            op.Instruction = (uint)Mnemonics.DebugFile;
            op.Args = new object[] { fileName };
            return op;
        }

        /// <summary>
        /// Convenience method for debug line opcodes, which can be inserted sneakily at the
        /// last moment when serialising the bytecode, and so need to be created in this lightweight
        /// way.
        /// </summary>
        public static Opcode CreateDebugLine(uint line)
        {
            Opcode op = new Opcode(null);
            op.Instruction = (uint)Mnemonics.DebugLine;
            op.Args = new object[] { line };
            return op;
        }

        /// <summary>
        /// Convenience method for creating an opcode from a position in a byte array.
        /// Returns an opcode object with references resolved and advances the position
        /// to the next place in the byte array.
        /// </summary>
        /// <param name="reader">Where to read the next opcode from</param>
        /// <param name="abc">The code within which we're reading</param>
        /// <returns>A new Opcode object, or null if there was no more data to be read</returns>
        public static Opcode BuildOpcode(ABCDataTypeReader reader, AbcCode abc)
        {
            Opcode op = new Opcode(abc);

            int code;
            code = reader.ReadUI8();
            if (code == -1)
            {
                return null;
            }
            op.Instruction = (uint)code;

            if (OpcodeTable[code] == null)
            {
                throw new SWFModellerException(
                        SWFModellerError.Internal,
                        "Bad opcode 0x" + code.ToString("X") + " at @" + (reader.Offset - 1));
            }
            OpcodeDef info = (OpcodeDef)OpcodeTable[code];

            List<object> args = new List<object>();

            if (info.Mnemonic == Mnemonics.LookupSwitch)
            {
                /* Special case: Has a variable arg */
                args.Add(reader.ReadSI24()); /* default offset */
                uint caseCount = reader.ReadU30();
                args.Add(caseCount);

                for (int i=0; i < caseCount + 1; i++)
                {
                    args.Add(reader.ReadSI24());
                }
            }
            else
            {
                if (info.Args != null)
                {
                    foreach (ArgType type in info.Args)
                    {
                        switch (type)
                        {
                            case ArgType.MultinameU30:
                                args.Add(abc.GetMultiname((int)reader.ReadU30()));
                                break;

                            case ArgType.OffsetS24:
                                args.Add(reader.ReadSI24());
                                break;

                            case ArgType.StringU30:
                                args.Add(abc.StringConsts[reader.ReadU30()]);
                                break;

                            case ArgType.RegisterU30:
                            case ArgType.ObjectRegisterU30:
                            case ArgType.PropertyRegisterU30:
                                args.Add(reader.ReadU30());
                                break;

                            case ArgType.ByteU8:
                                args.Add((byte)reader.ReadUI8());
                                break;

                            case ArgType.ShortU30:
                            case ArgType.IntU30:
                            case ArgType.UintU30:
                            case ArgType.DoubleU30:
                                args.Add(reader.ReadU30());
                                break;

                            case ArgType.ShortS30:
                                args.Add((int)reader.ReadU30());
                                break;

                            case ArgType.ByteS8:
                                args.Add(reader.ReadSI8());
                                break;

                            case ArgType.NamespaceU30:
                                args.Add(abc.GetNamespace((int)reader.ReadU30()));
                                break;

                            case ArgType.MethodU30:
                                args.Add(abc.GetMethod((int)reader.ReadU30()));
                                break;

                            case ArgType.CountU30:
                                args.Add(reader.ReadU30());
                                break;

                            case ArgType.ClassU30:
                                args.Add(abc.GetClass((int)reader.ReadU30()));
                                break;

                            case ArgType.ExceptionU30:
                                args.Add(reader.ReadU30());
                                break;

                            case ArgType.StackU8:
                                args.Add((byte)reader.ReadUI8());
                                break;

                            case ArgType.SlotU30:
                                args.Add(reader.ReadU30());
                                break;

                            case ArgType.DebugU8:
                                args.Add((byte)reader.ReadUI8());
                                break;

                            case ArgType.DebugTypeU30:
                                args.Add(reader.ReadU30());
                                break;

                            case ArgType.StringU8:
                                args.Add(abc.StringConsts[reader.ReadUI8()]);
                                break;

                            case ArgType.LineNumberU30:
                                args.Add(reader.ReadU30());
                                break;

                            default:
                                /* TODO */
                                throw new SWFModellerException(
                                        SWFModellerError.UnimplementedFeature,
                                        "Oops. Not done " + type.ToString());
                        }
                    }
                }
            }

            op.Args = args.ToArray();

            return op;
        }

        public override string ToString()
        {
#if(DEBUG)
            OpcodeDef? def = OpcodeTable[this.Instruction];
            if (def == null)
            {
                throw new SWFModellerException(
                        SWFModellerError.Internal,
                        "Broken disassembled bytecode. Oh dear.");
            }

            string[] args = new string[this.Args == null ? 0 : this.Args.Length];
            if (def.Value.Mnemonic == Mnemonics.LookupSwitch)
            {
                /* Lookup switches have variable arg lengths, so we make them a special case as there's
                 * no type information for the args. They're all just numbers though. */
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = this.Args[i].ToString();
                }
            }
            else
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (def.Value.Args[i] == ArgType.StringU30 || def.Value.Args[i] == ArgType.StringU8)
                    {
                        args[i] = "\"" + this.Args[i].ToString() + "\"";
                    }
                    else if (this.Args[i] is Opcode)
                    {
                        args[i] = "-> " + this.Args[i].ToString();
                    }
                    else
                    {
                        args[i] = this.Args[i].ToString();
                    }
                }
            }

            string defString = def.ToString();

            if (numberLabel != -1)
            {
                defString = "#" + numberLabel + " " + defString;
            }

            return defString + "(" + string.Join(", ", args) + ")";
#else
            return base.ToString();
#endif

        }

        /// <summary>
        /// Used for error messages to say what argument types an opcode was expecting.
        /// </summary>
        /// <returns></returns>
        internal string ExpectedArgsString()
        {
            OpcodeDef? defRef = OpcodeTable[(int)this.Mnemonic];
            StringBuilder sb = new StringBuilder();
            foreach (object arg in this.Args)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(arg.GetType().ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets passed an arg which has it's type marked as OffsetSI24, and may
        /// or may not be an offset, or another Opcode.
        /// </summary>
        /// <param name="arg">The existing arg</param>
        /// <returns>The new arg</returns>
        public delegate object OffsetProcMethod(object arg);

        public void OffsetProc(OffsetProcMethod opm)
        {
            if (Mnemonic == Opcode.Mnemonics.LookupSwitch)
            {
                for (int i = 0; i < Args.Length; i++)
                {
                    if (i != 1)
                    {
                        Args[i] = opm(Args[i]);
                    }
                }
                return;
            }

            Opcode.ArgType[] types = ArgTypes;
            for (int i = 0; i < Args.Length; i++)
            {
                Opcode.ArgType argType = types[i];
                if (argType == Opcode.ArgType.OffsetS24)
                {
                    Args[i] = opm(Args[i]);
                }
            }
        }
    }
}
