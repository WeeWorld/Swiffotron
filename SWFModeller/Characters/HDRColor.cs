//-----------------------------------------------------------------------
// HDRColor.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters
{
    using SWFProcessing.ModellingUtils.Util;

    /// <summary>
    /// A rather pretentiously named class that stores colour channels as fixed-point
    /// 8.8 numbers, allowing up to 256% color intensity. Useful for color transforms,
    /// really.
    /// </summary>
    public class HDRColor
    {
        /// <summary>
        /// Initializes a new instance of a colour
        /// </summary>
        /// <param name="red">Red component, from fp0.0 to fp1.0 (Or beyond if you wish)</param>
        /// <param name="green">Green component, from fp0.0 to fp1.0 (Or beyond if you wish)</param>
        /// <param name="blue">Blue component, from fp0.0 to fp1.0 (Or beyond if you wish)</param>
        public HDRColor(UFP88 red, UFP88 green, UFP88 blue) : this(red, green, blue, UFP88.ONE)
        {
            /* Nothing to add. */
        }

        /// <summary>
        /// Initializes a new instance of a colour with an alpha component
        /// </summary>
        /// <param name="red">Red component, from fp0.0 to fp1.0 (Or beyond if you wish)</param>
        /// <param name="green">Green component, from fp0.0 to fp1.0 (Or beyond if you wish)</param>
        /// <param name="blue">Blue component, from fp0.0 to fp1.0 (Or beyond if you wish)</param>
        /// <param name="alpha">Alpha component, from fp0.0 to fp1.0 (Or beyond if you wish)</param>
        public HDRColor(UFP88 red, UFP88 green, UFP88 blue, UFP88 alpha)
        {
            this.Red = red;
            this.Green = green;
            this.Blue = blue;
            this.Alpha = alpha;
        }

        public UFP88 Red { get; set; }

        public UFP88 Green { get; set; }

        public UFP88 Blue { get; set; }

        public UFP88 Alpha { get; set; }

        public bool HasAlpha
        {
            get
            {
                return this.Alpha < UFP88.ONE;
            }
        }

        /// <summary>
        /// Renders the colour as a string. Used only in test/debug console output.
        /// </summary>
        /// <returns>A string rendition of this colour.</returns>
        public override string ToString()
        {
            if (this.Alpha == UFP88.ONE)
            {
                return "[r:" + this.Red + ", g:" + this.Green + ", b:" + this.Blue + "]";
            }
            else
            {
                return "[r:" + this.Red + ", g:" + this.Green + ", b:" + this.Blue + ", a:" + this.Alpha + "]";
            }
        }

        internal HDRColor Clone()
        {
            return new HDRColor(this.Red, this.Green, this.Blue, this.Alpha);
        }
    }
}
