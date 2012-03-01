//-----------------------------------------------------------------------
// SWFDataTypeReader.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Text;
    using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
    using SWFProcessing.SWFModeller.Characters;
    using SWFProcessing.ModellingUtils.Geom;

    /// <summary>
    /// A binary reader with methods for reading the atomic units of
    /// a SWF file.
    /// </summary>
    public class SWFDataTypeReader : IDisposable
    {
        private const int CHUNK_LENGTH = 2048;

        /// <summary>
        /// This is only used when skipping data so it's safe to make it static. If different threads
        /// overwrite data then we don't really care.
        /// </summary>
        private static byte[] chunk = new byte[2048];

#if DEBUG
        /// <summary>
        /// This is useful for inspecting data in the debugger which would otherwise be obscured within
        /// a MemoryStream object. Hence why this is in the debug build only.
        /// </summary>
        private byte[] uncompressedSWFData;
#endif

        /// <summary>
        /// The stream being read.
        /// </summary>
        protected Stream inputStream;
        private int buffer;
        private int bufferedBits = 0;

        /// <summary>
        /// Initializes a new instance of a reader for a SWF data stream
        /// </summary>
        /// <param name="inputStream">The stream to read from</param>
        public SWFDataTypeReader(Stream inputStream)
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
        /// Once switched, you can't switch back again. In a compressed
        /// SWF, it suddenly switches to a deflate stream halfway through the
        /// file header. To accomodate this, we use a badly named BitReader
        /// (ZInputStream) from a third party lib to inflate the rest of the
        /// file into a byte array. We then substitute the original stream
        /// with a memory stream reading from this array and fudge the offsets
        /// so that the parent class is none the wiser and just reads it as
        /// though it was never compressed in the first place.
        /// </summary>
        public void SwitchToDeflateMode(uint fileLen)
        {
            this.Align8();
            byte[] data = new byte[fileLen - this.Offset];

            uint compressedOffset = this.Offset;

            InflaterInputStream zis = new InflaterInputStream(this.inputStream);

            int dataPos = 0;
            do
            {
                int read = zis.Read(data, dataPos, data.Length - dataPos);
                dataPos += read;
            }
            while (dataPos < data.Length);

#if DEBUG
            this.uncompressedSWFData = data;
#endif

            /* Some smoke and mirrors... */
            this.inputStream = new MemoryStream(data);
            this.Offset = compressedOffset;
        }

        /// <summary>
        /// Read an unsigned byte, aligned to the next byte boundary
        /// </summary>
        /// <returns>A byte value</returns>
        public byte ReadUI8()
        {
            int i;

            this.bufferedBits = 0;

            i = this.inputStream.ReadByte();

            if (i < 0)
            {
                throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        "Unexpected end of stream");
            }

            this.Offset++;

            return (byte)i;
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
        /// Read a 24-bit unsigned int, aligned to the next byte boundary
        /// </summary>
        /// <returns>An integer value</returns>
        public int ReadUI24()
        {
            int i;

            this.bufferedBits = 0;

            i = this.inputStream.ReadByte();
            i |= this.inputStream.ReadByte() << 8;
            i |= this.inputStream.ReadByte() << 16;

            if (i < 0)
            {
                throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        "Unexpected end of stream");
            }

            this.Offset += 3;

            return i;
        }

        /// <summary>
        /// Read an unsigned int, aligned to the next byte boundary
        /// </summary>
        /// <returns>An integer value</returns>
        public uint ReadUI32()
        {
            return (uint)this.ReadSI32();
        }

        public int ReadSI32()
        {
            int i, j;

            this.bufferedBits = 0;

            i = this.inputStream.ReadByte();
            i |= this.inputStream.ReadByte() << 8;
            i |= this.inputStream.ReadByte() << 16;
            j = this.inputStream.ReadByte();

            if (i < 0 || j < 0)
            {
                throw new SWFModellerException(SWFModellerError.SWFParsing, "Unexpected end of stream");
            }

            this.Offset += 4;

            return i | (j << 24);
        }

        public int ReadSI24()
        {
            int i;

            this.bufferedBits = 0;

            i = this.inputStream.ReadByte() << 8;
            i |= this.inputStream.ReadByte() << 16;
            i |= this.inputStream.ReadByte() << 24;

            i /= 256;

            this.Offset += 3;

            return i;
        }

        public int ReadSI16()
        {
            int i;

            this.bufferedBits = 0;

            i = this.inputStream.ReadByte() << 16;
            i |= this.inputStream.ReadByte() << 24;

            i /= 65536;

            this.Offset += 2;

            return i;
        }

        /// <summary>
        /// Read a 16-bit fixed-point 8.8 value, aligned to the next byte boundary
        /// </summary>
        /// <returns>A floating point equivalent to the fixed point value.</returns>
        public float ReadFIXED8()
        {
            this.bufferedBits = 0;

            float frac = this.inputStream.ReadByte();
            float sig = this.inputStream.ReadByte();

            if (frac < 0 || sig < 0)
            {
                throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        "Unexpected end of stream");
            }

            this.Offset += 2;

            return sig + (frac / 256);
        }

        public uint ReadUBits(int numBits)
        {
            if (numBits > 64)
            {
                throw new SWFModellerException(SWFModellerError.SWFParsing, "Unseemly bit count: "+numBits);
            }

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

        /// <summary>
        /// Reads a compact rectangle value from the SWF stream.
        /// </summary>
        /// <remarks>WARNING: Bear in mind that the reader will probably not be
        /// byte-aligned after calling this.</remarks>
        /// <returns>A rectangle object with its boundaries in pixels</returns>
        public Rect ReadRect()
        {
            this.Align8();
            int numBits = (int)this.ReadUBits(5);
            float xmin = ((float)this.ReadSBits(numBits)) / SWFValues.TwipsFactor;
            float xmax = ((float)this.ReadSBits(numBits)) / SWFValues.TwipsFactor;
            float ymin = ((float)this.ReadSBits(numBits)) / SWFValues.TwipsFactor;
            float ymax = ((float)this.ReadSBits(numBits)) / SWFValues.TwipsFactor;

            return new Rect(xmin, xmax, ymin, ymax);
        }

        public float ReadFB(int nbits)
        {
            int fixed1616 = this.ReadSBits(nbits);

            float frac = (float)(fixed1616 & 0x0000FFFF) / 65536;

            int whole = fixed1616 >> 16;

            return whole + frac;
        }

        public bool ReadBit()
        {
            return this.ReadUBits(1) != 0;
        }

        public Matrix ReadMatrix()
        {
            Matrix m = new Matrix();

            int nbits;

            this.Align8();

            if (this.ReadBit())
            {
                /* Has scale */

                nbits = (int)this.ReadUBits(5);

                m.ScaleX = this.ReadFB(nbits);
                m.ScaleY = this.ReadFB(nbits);
            }

            if (this.ReadBit())
            {
                /* Has rotate (skew) */

                nbits = (int)this.ReadUBits(5);

                m.SkewX = this.ReadFB(nbits);
                m.SkewY = this.ReadFB(nbits);
            }

            nbits = (int)this.ReadUBits(5);

            m.TransX = ((float)this.ReadSBits(nbits)) / SWFValues.TwipsFactor;
            m.TransY = ((float)this.ReadSBits(nbits)) / SWFValues.TwipsFactor;

            return m;
        }

        public void ReadRECORDHEADER(out int type, out uint followingOffset)
        {
            uint bits = this.ReadUI16();

            uint len = bits & 0x3F;

            type = (int)(bits >> 6);

            if (len == 0x3f)
            {
                len = (uint)this.ReadSI32();
            }

            followingOffset = this.Offset + len;
        }

        public Color ReadRGB()
        {
            int i;

            this.bufferedBits = 0;

            i = this.inputStream.ReadByte() << 16;
            i |= this.inputStream.ReadByte() << 8;
            i |= this.inputStream.ReadByte();

            if (i < 0)
            {
                throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        "Unexpected end of stream");
            }

            this.Offset += 3;

            return Color.FromArgb(i);
        }

        public Color ReadRGBA()
        {
            int i;

            this.bufferedBits = 0;

            i = this.inputStream.ReadByte() << 16;
            i |= this.inputStream.ReadByte() << 8;
            i |= this.inputStream.ReadByte();

            int a = this.inputStream.ReadByte();
            i |= a << 24;

            if (a < 0)
            {
                throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        "Unexpected end of stream");
            }

            this.Offset += 4;

            return Color.FromArgb(i);
        }

        /// <summary>
        /// Reads a block of bytes from the file.
        /// </summary>
        /// <param name="buf">An array to read the bytes into. buf.Length bytes will be
        /// read. If there aren't enough bytes, an exception will be thrown.</param>
        public void ReadByteBlock(byte[] buf)
        {
            this.ReadFully(buf, 0, buf.Length);
            this.Offset += (uint)buf.Length;
        }

        public void ReadByteBlock(byte[] buf, int off, int len)
        {
            this.ReadFully(buf, off, len);
            this.Offset += (uint)len;
        }

        public byte[] ReadByteBlock(int len)
        {
            if (len < 0)
            {
                throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
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

        /* ISSUE 34: This Skip stuff, with chunks and all that may be overly complicated.
         * Check the interfaces on C# streams to see if any of this can be simplified. */

        /// <summary>
        /// Skip a given number of bytes. Note that the pointer is aligned to the next
        /// byte boundary before skipping, even if the skip value is 0.
        /// </summary>
        /// <param name="n">The number of whole bytes to skip</param>
        /// <returns>The number of bytes skipped.</returns>
        public int Skip(uint n)
        {
            this.Offset += n;

            int skipCount = (int)n;

            int bytesRead = 0;
            int totalBytesRead = 0;
            while ((bytesRead = this.inputStream.Read(chunk, 0, Math.Min(skipCount - totalBytesRead, CHUNK_LENGTH))) > 0)
            {
                totalBytesRead += bytesRead;
            }

            if (totalBytesRead < n)
            {
                throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        "Unexpected end of stream");
            }

            return totalBytesRead;
        }

        /// <summary>
        /// Skips forward to the given file offset. If the file position is
        /// on or after the desired offset, then no change in file position
        /// occurs. It only skips forward, and never looks back.
        /// </summary>
        /// <param name="newOffset">The file offset you want to skip to.</param>
        public void SkipForwardTo(uint newOffset)
        {
            /* ISSUE 34: Use Seek() */
            this.bufferedBits = 0;

            newOffset -= this.Offset;

            if (newOffset > 0)
            {
                this.Skip(newOffset);
            }
            else if (newOffset < 0)
            {
                throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        "Unexpected end of stream");
            }
        }

        /// <summary>
        /// Reads a zero-terminated string from the WF stream. Assumes >v6 SWF, which
        /// means we assume it's UTF-8.
        /// </summary>
        /// <returns>A string.</returns>
        public string ReadString()
        {
            MemoryStream ms = new MemoryStream();
            byte b;
            do
            {
                b = this.ReadUI8();
                if (b != 0)
                {
                    ms.WriteByte(b);
                }
            }
            while (b != 0);

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        /// <summary>
        /// Reads a packed 32-bit value from the file.
        /// </summary>
        /// <returns>An unsigned 32-bit value.</returns>
        public uint ReadEncodedU32()
        {
            uint result = this.ReadUI8();

            if ((result & 0x00000080) == 0)
            {
                return result;
            }

            result = (result & 0x0000007f) | (uint)this.ReadUI8() << 7;

            if ((result & 0x00004000) == 0)
            {
                return result;
            }

            result = (result & 0x00003fff) | (uint)this.ReadUI8() << 14;

            if ((result & 0x00200000) == 0)
            {
                return result;
            }

            result = (result & 0x001fffff) | (uint)this.ReadUI8() << 21;

            if ((result & 0x10000000) == 0)
            {
                return result;
            }

            return (result & 0x0fffffff) | (uint)this.ReadUI8() << 28;
        }


        public ColorTransform ReadColorTransform()
        {
            this.Align8();

            bool hasAdd = this.ReadBit();
            bool hasMult = this.ReadBit();

            HDRColor add = null;
            HDRColor mult = null;

            int numBits = (int)this.ReadUBits(4);

            if (hasMult)
            {
                mult = new HDRColor(this.ReadSBits(numBits), this.ReadSBits(numBits), this.ReadSBits(numBits));
            }

            if (hasAdd)
            {
                add = new HDRColor(this.ReadSBits(numBits), this.ReadSBits(numBits), this.ReadSBits(numBits));
            }

            return new ColorTransform(add, mult);
        }

        public ColorTransform ReadColorTransformWithAlpha()
        {
            this.Align8();

            bool hasAdd = this.ReadBit();
            bool hasMult = this.ReadBit();

            HDRColor add = null;
            HDRColor mult = null;

            int numBits = (int)this.ReadUBits(4);

            if (hasMult)
            {
                int reds = this.ReadSBits(numBits);
                mult = new HDRColor(reds, this.ReadSBits(numBits), this.ReadSBits(numBits), this.ReadSBits(numBits));
            }

            if (hasAdd)
            {
                add = new HDRColor(this.ReadSBits(numBits), this.ReadSBits(numBits), this.ReadSBits(numBits), this.ReadSBits(numBits));
            }

            return new ColorTransform(add, mult);
        }

        private void ReadFully(byte[] b, int o, int len)
        {
            do
            {
                int read = this.inputStream.Read(b, o, len);
                if (read == -1 || (read == 0 && this.inputStream.Position >= this.inputStream.Length))
                {
                    throw new SWFModellerException(SWFModellerError.SWFParsing, "EOF in byte block");
                }
                len -= read;
                o += read;
            }
            while (len > 0);
        }

        internal string ReadLengthedUTF8(int length)
        {
            byte[] bytes = ReadByteBlock(length);
            string text = Encoding.UTF8.GetString(bytes);
            text = text.TrimEnd('\0'); /* Because flash really is that mental. */
            return text;
        }
    }
}
