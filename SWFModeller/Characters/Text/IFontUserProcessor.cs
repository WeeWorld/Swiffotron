//-----------------------------------------------------------------------
// IFontProcessor.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Text
{
    public delegate void FontUserProcessor(IFontUser fu);

    public interface IFontUserProcessor
    {
        void FontUserProc(FontUserProcessor fup);
    }
}
