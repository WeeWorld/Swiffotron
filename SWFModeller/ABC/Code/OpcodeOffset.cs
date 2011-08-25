//-----------------------------------------------------------------------
// OpcodeOffset.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC.Code
{
    public class OpcodeOffset
    {
        /* TODO: This class is used in exception handlers. I wonder if it'd work in jumps and switches too?.. */

        public Opcode Opcode { get; private set; }

        public int? Offset { get; private set; }

        public static implicit operator OpcodeOffset(int uintOffset)
        {
            return new OpcodeOffset() { Offset = uintOffset, Opcode = null };
        }

        public static implicit operator OpcodeOffset(Opcode opcodeRef)
        {
            return new OpcodeOffset() { Offset = null, Opcode = opcodeRef };
        }

        public static implicit operator int(OpcodeOffset oo)
        {
            if (oo.Offset == null)
            {
                throw new SWFModellerException(SWFModellerError.Internal, "Please reassemble class before evaluating offset");
            }

            return oo.Offset.Value;
        }

        public static implicit operator Opcode(OpcodeOffset oo)
        {
            if (oo.Opcode == null)
            {
                throw new SWFModellerException(SWFModellerError.Internal, "Please disassemble class before evaluating opcode offset");
            }

            return oo.Opcode;
        }

        public override string ToString()
        {
            if (Offset == null)
            {
                return "[@" + this.Opcode + "]";
            }
            return "[@" + this.Offset + "]";
        }
    }
}
