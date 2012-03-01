//-----------------------------------------------------------------------
// SWFDataTypeWriter.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller
{
    using System;
    using System.IO;
    using System.Text;
    using SWFProcessing.SWFModeller.Characters;
    using SWFProcessing.ModellingUtils.Geom;

    /// <summary>
    /// A writer that can write an assortment of atomic SWF types to a stream.
    /// </summary>
    internal class SWFDataTypeWriter : IDisposable
    {
        private static readonly uint[] LOG2_MASKS = { 0x2, 0xC, 0xF0, 0xFF00, 0xFFFF0000 };
        private static readonly int[] LOG2_POW = { 1, 2, 4, 8, 16 };

        private int buffer = 0;
        private int bitCount = 0;
        private int offset = 0;
        private Stream outputStream;

        /// <summary>
        /// Initializes a new instance of a SWF data writer
        /// </summary>
        /// <param name="outputStream">The stream to write SWF data to.</param>
        public SWFDataTypeWriter(Stream outputStream)
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

        /// <summary>
        /// Writes a rectangle to the stream
        /// </summary>
        /// <remarks>WARNING: Bear in mind that the writer will probably not be
        /// byte-aligned after calling this.</remarks>
        /// <param name="r">The rectangle to write.</param>
        public void WriteRect(Rect r)
        {
            int xmin = (int)(r.XMin * SWFValues.TwipsFactor);
            int xmax = (int)(r.XMax * SWFValues.TwipsFactor);
            int ymin = (int)(r.YMin * SWFValues.TwipsFactor);
            int ymax = (int)(r.YMax * SWFValues.TwipsFactor);

            int nbits = Math.Max(RequiredBits(xmin),
                    Math.Max(RequiredBits(xmax),
                            Math.Max(RequiredBits(ymin), RequiredBits(ymax))));

            this.WriteUBits((uint)nbits, 5);

            this.WriteSBits(xmin, nbits);
            this.WriteSBits(xmax, nbits);
            this.WriteSBits(ymin, nbits);
            this.WriteSBits(ymax, nbits);
        }

        private int RequiredBits(int v)
        {
            uint uv;
            if (v >= 0)
            {
                uv = (uint)v;
            }
            else
            {
                uv = (uint)(-v);
            }

            // http://graphics.stanford.edu/~seander/bithacks.html#IntegerLog

            int r = 0;
            for (int i = 4; i >= 0; i--)
            {
                if ((uv & LOG2_MASKS[i]) != 0)
                {
                    uv >>= LOG2_POW[i];
                    r |= LOG2_POW[i];
                } 
            }

            r += 2;

            return Math.Min(31, r);
        }

        public void WriteFIXED8(float v)
        {
            this.Align8();

            double fraction = v - Math.Floor(v);
            this.outputStream.WriteByte((byte)(fraction * 256));
            this.outputStream.WriteByte((byte)v);
        }

        public void WriteUI16(uint v)
        {
            this.Align8();

            this.outputStream.WriteByte((byte)v);
            v >>= 8;
            this.outputStream.WriteByte((byte)v);
        }

        public void WriteSI32(int v)
        {
            this.WriteUI32((uint)v);
        }

        public void WriteSI16(int v)
        {
            this.WriteUI16((uint)v);
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

        public void WriteUI32(uint v)
        {
            this.Align8();

            this.outputStream.WriteByte((byte)v);
            v >>= 8;
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

        public void Align8()
        {
            if (this.bitCount > 0)
            {
                this.bitCount = 0;
                this.outputStream.WriteByte((byte)this.buffer);
                this.buffer = 0;
            }
        }

        /// <summary>
        /// Call this only if you know bitCount to be >0
        /// </summary>
        public void Align8Unchecked()
        {
            this.bitCount = 0;
            this.outputStream.WriteByte((byte)this.buffer);
            this.buffer = 0;
        }

        public void Write(byte[] b, int off, int len)
        {
            this.Align8();
            this.outputStream.Write(b, off, len);
            this.offset += len;
        }

        public void Write(byte b)
        {
            this.Align8();
            this.outputStream.WriteByte(b);
            this.offset++;
        }

        public void WriteUBits(uint value, int numBits)
        {
            int bitPos = 8 - this.bitCount;
            int bitNum = numBits;

            while (bitNum > 0)
            {
                while (bitPos > 0 && bitNum > 0)
                {
                    if ((value & (1 << (bitNum - 1))) != 0)
                    {
                        this.buffer = this.buffer | (1 << (bitPos - 1));
                    }

                    bitNum--;
                    bitPos--;
                    this.bitCount++;
                }

                if (bitPos == 0)
                {
                    this.Align8();
                    if (bitNum > 0)
                    {
                        bitPos = 8;
                    }
                }
            }
        }

        public void WriteSBits(int value, int numBits)
        {
            this.WriteUBits((uint)value, numBits);
        }

        public void WriteBit(bool bit)
        {
            int bitPos = 7 - this.bitCount;

            if (bit)
            {
                this.buffer = this.buffer | (1 << bitPos);
            }

            this.bitCount++;

            if (bitPos == 0)
            {
                this.Align8Unchecked();
            }
        }

        public void WriteRGBA(int v)
        {
            this.Align8();

            this.outputStream.WriteByte((byte)(v >> 16));
            this.outputStream.WriteByte((byte)(v >> 8));
            this.outputStream.WriteByte((byte)v);
            this.outputStream.WriteByte((byte)(v >> 24));
        }

        public void WriteRGB(int v)
        {
            this.Align8();

            this.outputStream.WriteByte((byte)(v >> 16));
            this.outputStream.WriteByte((byte)(v >> 8));
            this.outputStream.WriteByte((byte)v);
        }

        public void WriteBytes(byte[] bytes)
        {
            this.Align8();
            this.outputStream.Write(bytes, 0, bytes.Length);
        }

        public void WriteFB(float value, int nbits)
        {
            int value1616 = (int)(value * 65536);
            this.WriteSBits(value1616, nbits);
        }

        private void WriteVarlenFBPair(float a, float b)
        {
            int a1616 = (int)(a * 65536);
            int b1616 = (int)(b * 65536);
            int nbits = Math.Max(RequiredBits(a1616), RequiredBits(b1616));
            this.WriteUBits((uint)nbits, 5);
            this.WriteSBits(a1616, nbits);
            this.WriteSBits(b1616, nbits);
        }

        public void WriteMatrix(Matrix m)
        {
            this.WriteBit(m.HasScale);
            if (m.HasScale)
            {
                WriteVarlenFBPair(m.ScaleX, m.ScaleY);
            }

            this.WriteBit(m.HasSkew);
            if (m.HasSkew)
            {
                WriteVarlenFBPair(m.SkewX, m.SkewY);
            }

            /* In the following code, you'd think you'd be able to inline it all. If you
             * inline it though, you get weird rounding errors messing things
             * up... (http://stackoverflow.com/questions/2509576/why-is-my-number-being-rounded-incorrectly) */
            float floatTwipsX = m.TransX * SWFValues.TwipsFactor;
            int intTwipsX = (int)floatTwipsX;
            float floatTwipsY = m.TransY * SWFValues.TwipsFactor;
            int intTwipsY = (int)floatTwipsY;

            int nbits = Math.Max(RequiredBits(intTwipsX), RequiredBits(intTwipsY));
            this.WriteUBits((uint)nbits, 5);

            this.WriteSBits(intTwipsX, nbits);
            this.WriteSBits(intTwipsY, nbits);
        }

        /// <summary>
        /// Strings in SWF are zero-terminated.
        /// </summary>
        /// <param name="s">The string to write</param>
        /// <param name="write8BitLen">Some weird parts of SWF have a zero-terminated
        /// string prefixed by a length. Yeah, I know.</param>
        public void WriteString(string s, bool write8BitLen = false)
        {
            this.Align8();
            byte[] utf8 = Encoding.UTF8.GetBytes(s);
            if (write8BitLen)
            {
                this.WriteUI8((uint)utf8.Length + 1);
            }
            this.Write(utf8, 0, utf8.Length);
            this.Write(0);
        }

        public void WriteColorTransform(ColorTransform cxform, bool withAlpha)
        {
            this.Align8();

            this.WriteBit(cxform.HasAdd);
            this.WriteBit(cxform.HasMult);

            int numBits = 10; /* 10-bits for 9-bit 1.00 fixed 8.8 signed value */
            this.WriteUBits(10, 4);

            if (cxform.HasMult)
            {
                HDRColor m = cxform.Mult;
                this.WriteColorComponent((int)m.Red, numBits);
                this.WriteColorComponent((int)m.Green, numBits);
                this.WriteColorComponent((int)m.Blue, numBits);
                if (withAlpha)
                {
                    this.WriteColorComponent((int)m.Alpha, numBits);
                }
            }

            if (cxform.HasAdd)
            {
                HDRColor a = cxform.Add;
                this.WriteColorComponent((int)a.Red, numBits);
                this.WriteColorComponent((int)a.Green, numBits);
                this.WriteColorComponent((int)a.Blue, numBits);
                if (withAlpha)
                {
                    this.WriteColorComponent((int)a.Alpha, numBits);
                }
            }
        }

        private void WriteColorComponent(int value, int numBits)
        {
            float fv = (float)value / 256.0f;
            double intComp = Math.Floor(fv);
            double frac = 256 * (fv - intComp);

            int valfp88 = (((int)intComp) << 8) | (int)frac;

            this.WriteUBits((uint)valfp88, numBits);
        }
    }
}
