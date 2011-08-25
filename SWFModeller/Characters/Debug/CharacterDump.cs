//-----------------------------------------------------------------------
// CharacterDump.cs
//
//
//-----------------------------------------------------------------------

#if DEBUG
namespace SWFProcessing.SWFModeller.Characters.Debug
{
    using System;
    using System.Text;
    using SWFProcessing.SWFModeller.Characters;
    using SWFProcessing.SWFModeller.Characters.Shapes;
    using SWFProcessing.SWFModeller.Characters.Text;
    using SWFProcessing.SWFModeller.Characters.Images;

    /// <summary>
    /// A static class that knows how to dump characters so save you the hassle of
    /// working out the type and casting it. All because it's hard to only implement
    /// an interface in a debug build.
    /// </summary>
    public static class CharacterDump
    {
        public static void ToStringModelView(ICharacter c, int nest, StringBuilder sb, out bool oneLiner)
        {
            if (c is Sprite)
            {
                ((Sprite)c).ToStringModelView(nest, sb, out oneLiner);
            }
            else if (c is EditText)
            {
                ((EditText)c).ToStringModelView(nest, sb, out oneLiner);
            }
            else if (c is StaticText)
            {
                ((StaticText)c).ToStringModelView(nest, sb, out oneLiner);
            }
            else if (c is IImage)
            {
                ((IImage)c).ToStringModelView(nest, sb, out oneLiner);
            }
            else if (c is IShape)
            {
                ((IShape)c).ToStringModelView(nest, sb, out oneLiner);
            }
            else
            {
                throw new Exception("*** ERROR: CAN'T DUMP: " + c.ToString());
            }
        }
    }
}
#endif
