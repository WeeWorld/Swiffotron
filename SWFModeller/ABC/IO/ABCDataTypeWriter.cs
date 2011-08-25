//-----------------------------------------------------------------------
// ABCDataTypeWriter.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller
{
    using System;
    using System.IO;
    using System.Text;
    using SWFProcessing.SWFModeller.ABC;

    /// <summary>
    /// A writer that can write an assortment of atomic ABC types to a stream.
    /// </summary>
    internal class ABCDataTypeWriter : IDisposable
    {
        private uint buffer = 0;
        private int bitCount = 0;
        private int offset = 0;
        private Stream outputStream;

        /// <summary>
        /// Initializes a new instance of a ABC data writer
        /// </summary>
        /// <param name="outputStream">The stream to write ABC data to.</param>
        public ABCDataTypeWriter(Stream outputStream)
        {
            this.outputStream = outputStream;
        }

        /// <summary>
        /// This is different to our internal offset into our own written data.
        /// </summary>
        public long Offset { get { return this.outputStream.Position; } }

        #region IDisposable Members

        public void Dispose()
        {
            this.Close();
        }

        #endregion

        public void Close()
        {
            this.Align8(); /* Flush any remaining partial bytes */
            this.outputStream.Close();
        }

        public void WriteUI16(uint v)
        {
            this.Align8();

            this.outputStream.WriteByte((byte)v);
            v >>= 8;
            this.outputStream.WriteByte((byte)v);
        }

        public void WriteSI16(int v)
        {
            this.Align8();

            this.outputStream.WriteByte((byte)v);
            v >>= 8;
            this.outputStream.WriteByte((byte)v);
        }

        public void WriteSI32(int sv)
        {
            uint mask = (uint)sv;
            if (sv < 0)
            {
                mask = (~mask) << 1;
                if (mask == 0)
                {
                    mask = 0xFFFFFFFFU;
                }
            }
            WriteUI32((uint)sv, mask);
        }

        public void WriteSI24(int v)
        {
            this.WriteUI24((uint)v);
        }

        /// <summary>
        /// TODO: Exposing this mask part on a public interface is an abhorrent
        /// thing to do. Don't ever ever do that again. Naughty me.
        /// </summary>
        public void WriteUI32(uint v, uint mask = 0)
        {
            if (mask == 0)
            {
                mask = v;
            }

            if (v < 128U)
            {
                this.WriteUI8(v);
            }
            else if (v < 16384U)
            {
                this.WriteUI8((v & 127) | 128);
                this.WriteUI8((v >> 7) & 127);
            }
            else if (v < 2097152U)
            {
                this.WriteUI8((v & 127) | 128);
                this.WriteUI8((v >> 7) | 128);
                this.WriteUI8((v >> 14) & 127);
            }
            else if (v < 268435456U)
            {
                this.WriteUI8((v & 127) | 128);
                this.WriteUI8((v >> 7) | 128);
                this.WriteUI8((v >> 14) | 128);
                this.WriteUI8((v >> 21) & 127);
            }
            else
            {
                this.WriteUI8((v & 127) | 128);
                this.WriteUI8((v >> 7) | 128);
                this.WriteUI8((v >> 14) | 128);
                this.WriteUI8((v >> 21) | 128);
                this.WriteUI8((v >> 28) & 15);
            }
        }

        public void WriteUI24(uint v)
        {
            this.Align8();

            this.outputStream.WriteByte((byte)v);
            v >>= 8;
            this.outputStream.WriteByte((byte)v);
            v >>= 8;
            this.outputStream.WriteByte((byte)v);
        }

        public void WriteUI8(uint v)
        {
            this.Write((byte)v);
        }

        public void WriteSI8(int v)
        {
            /* We assume that opcode validation elsewhere will permanently give us
             * protection from bad values here. */
            this.Write((byte)v);
        }

        public void Align8()
        {
            if (this.bitCount > 0)
            {
                this.bitCount = 0;
                this.outputStream.WriteByte((byte)this.buffer);
                this.buffer = 0;
            }
        }

        public void Write(byte[] b, int off, int len)
        {
            this.Align8();
            this.outputStream.Write(b, off, len);
            this.offset += len;
        }

        public void Write(byte b)
        {
            if (this.bitCount > 0)
            {
                this.Align8();
            }
            this.outputStream.WriteByte(b);
            this.offset++;
        }

        public void WriteUBits(uint value, int numBits)
        {
            /* This is not the fastest way to do this, but right now I just need
             * to write something that works, dammit. */

            while (numBits --> 0)
            {
                uint bit = value & 1;
                value >>= 1;
                bit <<= this.bitCount++;
                this.buffer |= bit;
                if (this.bitCount == 8)
                {
                    this.Align8();
                }
            }
        }

        public void WriteBit(bool boolBit)
        {
            if (boolBit)
            {
                uint bit = 1U << this.bitCount;
                this.buffer |= bit;
            }

            if (++this.bitCount == 8)
            {
                this.Align8();
            }
        }

        /// <summary>
        /// Writes a lengthed string for ABC data. For SWF data, you should use WriteStringZ instead.
        /// </summary>
        /// <param name="s">The string to write.</param>
        public void WriteString(string s)
        {
            if (s == ABCValues.AnyName)
            {
                s = string.Empty;
            }

            this.Align8();
            byte[] utf8 = Encoding.UTF8.GetBytes(s);
            this.WriteU30Packed((uint)utf8.Length);
            this.Write(utf8, 0, utf8.Length);
        }

        public void WriteU30Packed(uint v)
        {
            this.Align8();
            this.WriteUBits(v, 7);
            v >>= 7;
            if (v == 0)
            {
                this.WriteBit(false);
                return;
            }
            this.WriteBit(true);

            this.WriteUBits(v, 7);
            v >>= 7;
            if (v == 0)
            {
                this.WriteBit(false);
                return;
            }
            this.WriteBit(true);

            this.WriteUBits(v, 7);
            v >>= 7;
            if (v == 0)
            {
                this.WriteBit(false);
                return;
            }
            this.WriteBit(true);

            this.WriteUBits(v, 7);
            v >>= 7;
            if (v == 0)
            {
                this.WriteBit(false);
                return;
            }
            this.WriteBit(true);

            if (v > 3)
            {
                throw new SWFModellerException(
                        SWFModellerError.Internal,
                        "Can't write a 32-bit value into a U30 field");
            }

            this.WriteUI8(v);
        }
    }
}
