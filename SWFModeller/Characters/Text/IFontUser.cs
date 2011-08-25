//-----------------------------------------------------------------------
// IFontUser.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Text
{
    using SWFProcessing.SWFModeller.Text;

    /// <summary>
    /// This marks characters as being users of fonts so that they can be easily
    /// picked out by SWF.FontProc
    /// </summary>
    public interface IFontUser
    {
        SWFFont Font { get; set; }

        bool HasFont { get; }
    }
}
