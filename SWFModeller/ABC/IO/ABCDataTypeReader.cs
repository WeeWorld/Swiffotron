//-----------------------------------------------------------------------
// ABCDataTypeReader.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// A binary reader with methods for reading the atomic units of
    /// a ABC file.
    /// </summary>
    public class ABCDataTypeReader : IDisposable
    {
        /// <summary>
        /// The stream being read.
        /// </summary>
        protected Stream inputStream;
        private int buffer;
        private int bufferedBits = 0;

        /// <summary>
        /// Initializes a new instance of a reader for a ABC data stream
        /// </summary>
        /// <param name="inputStream">The stream to read from</param>
        public ABCDataTypeReader(Stream inputStream)
        {
            this.inputStream = inputStream;
        }

        /// <summary>
        /// The next byte offset to be read
        /// </summary>
        public uint Offset { get; private set; }

        #region IDisposable Members

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.inputStream != null)
            {
                this.inputStream.Close();
            }
        }

        #endregion

        /// <summary>
        /// Align to the next whole byte. If we're already on a byte boundary, the
        /// next byte position does not change.
        /// </summary>
        public void Align8()
        {
            this.bufferedBits = 0;
        }

        /// <summary>
        /// Read an unsigned byte, aligned to the next byte boundary
        /// </summary>
        /// <returns>A byte value in a 32-bit integer which is -1 if at the end
        /// of the stream.</returns>
        public int ReadUI8()
        {
            this.bufferedBits = 0;
            this.Offset++;
            return this.inputStream.ReadByte();
        }

        /// <summary>
        /// Read an unsigned byte, aligned to the next byte boundary
        /// </summary>
        /// <returns>A byte value in a 32-bit integer which is -1 if at the end
        /// of the stream.</returns>
        public int ReadSI8()
        {
            this.bufferedBits = 0;
            this.Offset++;
            int b = this.inputStream.ReadByte();
            if (0 != (b & 0x80000000))
            unchecked
            {
                b |= (int)0xFFFFFF00;
            }
            return b;
        }

        /// <summary>
        /// Read an unsigned short, aligned to the next byte boundary
        /// </summary>
        /// <returns>A 16-bit value</returns>
        public ushort ReadUI16()
        {
            int i;

            this.bufferedBits = 0;

            i = this.inputStream.ReadByte();
            i |= this.inputStream.ReadByte() << 8;

            if (i < 0)
            {
                throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        "Unexpected end of stream");
            }

            this.Offset += 2;

            return (ushort)i;
        }

        /// <summary>
        /// There isn't any 32-bit types in ABC, they're all packed ints. You
        /// probably want ReadSI32 or ReadUI32 instead. This is here to allow us
        /// to fake reading doubles.
        /// </summary>
        /// <returns></returns>
        public int ReadInt32()
        {
            int i, j;

            this.bufferedBits = 0;

            i = this.inputStream.ReadByte();
            i |= this.inputStream.ReadByte() << 8;
            i |= this.inputStream.ReadByte() << 16;
            j = this.inputStream.ReadByte();

            if ((i < 0) || (j < 0))
            {
                throw new SWFModellerException(
                        SWFModellerError.ABCParsing,
                        "Unexpected end of stream");
            }

            this.Offset += 4;

            return i | (j << 24);
        }

        /// <summary>
        /// Read an unsigned int, aligned to the next byte boundary
        /// </summary>
        /// <returns>An integer value</returns>
        public uint ReadUI32()
        {
            /* Pretty sure there must be a cheaper way to write this that
             * involves only unsigned types and liberal use of unchecked{}
             * blocks. */

            int v = this.ReadUI8();

            if (0 == (v & 0x80))
            {
                return (uint)v;
            }

            v = v & 0x7f | this.ReadUI8() << 7;

            if (0 == (v & 0x4000))
            {
                return (uint)v;
            }

            v = v & 0x3fff | this.ReadUI8() << 14;

            if (0 == (v & 0x200000))
            {
                return (uint)v;
            }

            v = v & 0x1fffff | this.ReadUI8() << 21;

            if (0 == (v & 0x10000000))
            {
                return (uint)v;
            }

            v = v | this.ReadUI8() << 28;

            return (uint)v;
        }

        public int ReadSI32()
        {
            /* Pretty sure there must be a cheaper way to write this that
             * involves only unsigned types and liberal use of unchecked{}
             * blocks. */

            int v = this.ReadUI8();

            if (0 == (v & 0x80))
            {
                if (0 != (v & 0x40))
                unchecked
                {
                    /* Sign extend */
                    return v & (int)0xFFFFFFC0;
                }
                return v;
            }

            int next = this.ReadUI8();
            v = v & 0x7f | next << 7;

            if (0 == (v & 0x4000))
            {
                if (0 != (next & 0x40))
                unchecked
                {
                    /* Sign extend */
                    return v & (int)0xFFFFE000;
                }
                return v;
            }

            next = this.ReadUI8();
            v = v & 0x3fff | next << 14;

            if (0 == (v & 0x200000))
            {
                if (0 != (next & 0x40))
                unchecked
                {
                    /* Sign extend */
                    return v & (int)0xFFE00000;
                }
                return v;
            }

            next = this.ReadUI8();
            v = v & 0x1fffff | next << 21;

            if (0 == (v & 0x10000000))
            {
                if (0 != (next & 0x40))
                unchecked
                {
                    /* Sign extend */
                    return v & (int)0xF0000000;
                }
                return v;
            }

            v = v | this.ReadUI8() << 28;

            return v;
        }

        public int ReadSI24()
        {
            int i;

            this.bufferedBits = 0;

            i = this.inputStream.ReadByte() << 8;
            i |= this.inputStream.ReadByte() << 16;
            i |= this.inputStream.ReadByte() << 24;

            i >>= 8;

            this.Offset += 3;

            return i;
        }

        public uint ReadUBits(int numBits)
        {
            if (numBits == 0)
            {
                return 0;
            }

            uint result = 0;

            if (this.bufferedBits == 0)
            {
                this.buffer = this.inputStream.ReadByte();
                this.bufferedBits = 8;

                this.Offset++;
            }

            for (;;)
            {
                int shift = numBits - this.bufferedBits;
                if (shift > 0)
                {
                    result |= (uint)this.buffer << shift;
                    numBits -= this.bufferedBits;

                    this.buffer = this.inputStream.ReadByte();
                    this.bufferedBits = 8;

                    this.Offset++;
                }
                else
                {
                    result |= (uint)this.buffer >> -shift;
                    this.bufferedBits -= numBits;
                    this.buffer &= 0xff >> (8 - this.bufferedBits);

                    return result;
                }
            }
        }

        public int ReadSBits(int numBits)
        {
            int bits = (int)this.ReadUBits(numBits);

            if (bits > 0 && (bits & (1 << (numBits - 1))) != 0)
            {
                bits |= -1 << numBits;
            }

            return (int)bits;
        }

        public byte[] ReadByteBlock(int len)
        {
            if (len < 0)
            {
                throw new SWFModellerException(
                        SWFModellerError.ABCParsing,
                        "Length must be >0");
            }

            byte[] b = new byte[len];

            if (len == 0)
            {
                return b;
            }

            this.ReadFully(b, 0, len);
            this.Offset += (uint)len;
            return b;
        }

        /// <summary>
        /// Reads an aligned 30-bit unsigned integer. The top two bits will be 0. See
        /// ReadEncodedU32 for a 32-bit version. This belongs in the ABC spec; 32-bit values
        /// belong in the SWF spec.
        /// </summary>
        /// <returns>An unsigned 30-bit integer value.</returns>
        public uint ReadU30()
        {
            uint v = this.ReadUI32();

            uint top3 = (v & 0xE0000000);

            /* The type is meant to be unsigned, but it's used in signed values anyway.
             * Oh flash, you decrepit weirdo. This means that the top 3 bits here must either all
             * be 0s, or all 1s. */

            /* It does seem that if the top-30th bit is set, then the next 2 top bits will also
             * be set. */
            if (top3 != 0 && top3 != 0xE0000000)
            {
                throw new SWFModellerException(
                        SWFModellerError.ABCParsing,
                        "Value validation error: 32-bit number is too large for 30-bit value.");
            }

            return v;
        }

        /// <summary>
        /// Reads a UTF8 string, prefixed by a length value stored as a packed int.
        /// </summary>
        /// <returns>The string.</returns>
        public string ReadString()
        {
            int len = (int)this.ReadU30();
            byte[] utf8 = this.ReadByteBlock(len);
            return Encoding.UTF8.GetString(utf8);
        }

        private void ReadFully(byte[] b, int o, int len)
        {
            do
            {
                int read = this.inputStream.Read(b, o, len);
                if (read == -1 || (read == 0 && this.inputStream.Position >= this.inputStream.Length))
                {
                    throw new SWFModellerException(
                        SWFModellerError.ABCParsing,
                        "EOF in byte block");
                }
                len -= read;
                o += read;
            }
            while (len > 0);
        }
    }
}
