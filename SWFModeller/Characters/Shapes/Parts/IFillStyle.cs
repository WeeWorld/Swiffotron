//-----------------------------------------------------------------------
// IFillStyle.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts
{
    using SWFProcessing.SWFModeller.Characters.Images;

    /// <summary>
    /// Marker interface
    /// </summary>
    public interface IFillStyle
    {
        IImage Bitmap { get; set; }
    }
}
