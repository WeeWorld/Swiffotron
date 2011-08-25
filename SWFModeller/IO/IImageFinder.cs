//-----------------------------------------------------------------------
// IImageFinder.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.IO
{
    using SWFProcessing.SWFModeller.Characters.Images;

    /// <summary>
    /// Bit of a hack. Shapes have their own parser, but they need to resolve character IDs.
    /// We do this by exposing this interface from the SWFReader class and passing it into a
    /// shape parser.
    /// </summary>
    interface IImageFinder
    {
        IImage FindImage(int cid);
    }
}
