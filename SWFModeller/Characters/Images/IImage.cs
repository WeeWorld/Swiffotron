//-----------------------------------------------------------------------
// IImage.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Images
{
    using System.Text;

    public interface IImage : ICharacter
    {
#if DEBUG
        void ToStringModelView(int nest, StringBuilder sb, out bool oneLiner);
#endif
    }
}
