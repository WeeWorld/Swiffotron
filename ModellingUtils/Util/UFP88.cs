//-----------------------------------------------------------------------
// UFP88.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.ModellingUtils.Util
{
    using System;

    /// <summary>
    /// Represents a fixed point 8.8 number. These are stored as uints rather
    /// than ushorts because it's more efficient to store things in the natural
    /// word size. It's a struct rather than a class so the value can be copied into
    /// method parameters as though it was a primitive type.
    /// </summary>
    public struct UFP88 : IComparable
    {
        /// <summary>
        /// Gets the value 1.0, in 8.8 fixed point form.
        /// </summary>
        public static UFP88 ONE { get; private set; }

        /// <summary>
        /// Initialise static values. For the life of me I can't remember why I did
        /// it this way but I guess I must have had a damned good reason.
        /// </summary>
        static UFP88()
        {
            UFP88.ONE = 0x00000100;
        }

        private uint value;

        /// <summary>
        /// Initializes a new instance of a fixed point number from a bunch of
        /// preformatted bits.
        /// </summary>
        /// <param name="fixed88">The bits in uint form.</param>
        private UFP88(uint fixed88)
        {
            this.value = fixed88;
        }

        public static implicit operator UFP88(double d)
        {
            return new UFP88((uint)(d * 256.0d));
        }

        public static explicit operator double(UFP88 fixed88)
        {
            return ((double)fixed88.value) / 256.0d;
        }

        public static implicit operator UFP88(float f)
        {
            return new UFP88((uint)(f * 256.0f));
        }

        public static explicit operator float(UFP88 fixed88)
        {
            return ((float)fixed88.value) / 256.0f;
        }

        public static implicit operator UFP88(int i)
        {
            return new UFP88((uint)i);
        }

        public static explicit operator int(UFP88 fixed88)
        {
            return (int)fixed88.value;
        }

        public static bool operator <(UFP88 lhs, UFP88 rhs)
        {
            return lhs.value < rhs.value;
        }

        public static bool operator >(UFP88 lhs, UFP88 rhs)
        {
            return lhs.value > rhs.value;
        }

        public static bool operator <=(UFP88 lhs, UFP88 rhs)
        {
            return lhs.value <= rhs.value;
        }

        public static bool operator >=(UFP88 lhs, UFP88 rhs)
        {
            return lhs.value >= rhs.value;
        }

        public static bool operator ==(UFP88 lhs, UFP88 rhs)
        {
            return lhs.value == rhs.value;
        }

        public static bool operator !=(UFP88 lhs, UFP88 rhs)
        {
            return lhs.value != rhs.value;
        }

        /// <summary>
        /// Renders the number as a string. Used only in test/debug console output.
        /// </summary>
        /// <returns>A string rendition of this number.</returns>
        public override string ToString()
        {
            return string.Empty + (float)this;
        }

        /// <summary>
        /// Compares a fixed point number for equality
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>True if equal</returns>
        public override bool Equals(object obj)
        {
            return value.Equals(obj);
        }

        /// <summary>
        /// Gets a hashcode for the value
        /// </summary>
        /// <returns>A hashcode</returns>
        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            return value.CompareTo(obj);
        }

        #endregion
    }
}
