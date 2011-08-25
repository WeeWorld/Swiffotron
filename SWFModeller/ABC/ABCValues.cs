//-----------------------------------------------------------------------
// ABCValues.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC
{
    /// <summary>
    /// Constants found when reading ABC files.
    /// </summary>
    public abstract class ABCValues
    {
        /// <summary>
        /// Should the code initialize lazily?
        /// </summary>
        public const int AbcFlagLazyInitialize = 0x00000001;

        /// <summary>
        /// Internal value for 'string 0' which is an empty string in some contexts,
        /// and the 'any' name in others. We use this unlikely token and then convert
        /// it to an empty string when we save the file.
        /// </summary>
        public const string AnyName = "_@ny~Name#";
    }
}
