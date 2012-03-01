//-----------------------------------------------------------------------
// ISVGElement.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWF2SVG.Model
{
    using System.Text;

    interface ISVGElement
    {
        void Render(StringBuilder buf);
    }
}
