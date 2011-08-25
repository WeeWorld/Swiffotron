//-----------------------------------------------------------------------
// IShape.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes
{
    using System.Text;
    using SWFProcessing.SWFModeller.Characters.Geom;
    using SWFProcessing.SWFModeller.Characters.Images;

    /// <summary>
    /// A shape object that can be placed onto the stage.
    /// </summary>
    public interface IShape : ICharacter
    {
        void SetOriginalBytes(byte[] shapeData, Tag format);

        byte[] OriginalBytes { get; set; }

        Tag OriginalFormat { get; set; }

        /// <summary>
        /// A shape should have a bounding box.
        /// </summary>
        Rect Bounds { get; set; }

        /// <summary>
        /// If the character references bitmap(s), this will get them.
        /// </summary>
        /// <returns>An image array, or null if it doesn't reference any.</returns>
        IImage[] GetImages();

#if DEBUG
        void ToStringModelView(int nest, StringBuilder sb, out bool oneLiner);
#endif
    }
}
