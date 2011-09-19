//-----------------------------------------------------------------------
// FillType.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.Parts
{
    using System;

    public class FillTypes
    {
        public static bool IsBitmap(FillType t)
        {
            return (t == FillType.RepeatingBitmap
                    || t == FillType.ClippedBitmap
                    || t == FillType.NonSmoothClippedBitmap
                    || t == FillType.NonSmoothRepeatingBitmap);
        }
    }

    public enum FillType
    {
        Solid = 0x00,

        LinearGradient = 0x10,

        RadialGradient = 0x12,

        FocalGradient = 0x13,

        RepeatingBitmap = 0x40,

        ClippedBitmap = 0x41,

        NonSmoothRepeatingBitmap = 0x42,

        NonSmoothClippedBitmap = 0x43
    }

}
