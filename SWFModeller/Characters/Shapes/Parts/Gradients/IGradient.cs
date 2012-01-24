//-----------------------------------------------------------------------
// IGradient.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts.Gradients
{
    public enum GradientSpread
    {
        Pad = 0,
        Reflect = 0,
        Repeat = 2
    }

    public enum GradientInterpolation
    {
        Normal = 0,
        Linear = 1
    }

    /// <summary>
    /// Marker interface
    /// </summary>
    public interface IGradient
    {
    }
}
