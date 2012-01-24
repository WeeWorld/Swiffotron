//-----------------------------------------------------------------------
// ShapeParser.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters.Shapes.IO
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using SWFProcessing.SWFModeller.Characters.Geom;
    using SWFProcessing.SWFModeller.Characters.Shapes.Parts;
    using SWFProcessing.SWFModeller.Characters.Shapes.Parts.Gradients;
    using SWFProcessing.SWFModeller.IO;
    using System;

    internal class ShapeParser
    {
        private IImageFinder ImageFinder { get; set; }

        internal IShape Parse(Tag tag, byte[] data, IImageFinder imageFinder)
        {
            this.ImageFinder = imageFinder;

            SWFDataTypeReader shapeReader = new SWFDataTypeReader(new MemoryStream(data));

            IShape newShape = null;

            switch (tag)
            {
                case Tag.DefineShape:
                case Tag.DefineShape2:
                case Tag.DefineShape3:
                    newShape = this.ParseDefineShapeN(shapeReader, tag);
                    break;

                case Tag.DefineFont3:
                    /* We do some hackery magic here. Because we happen to know that the only reason
                     * we parse shapes is to get bitmap references out of them, we skip the actual parsing
                     * of glyphs because they can't ever have bitmaps in them. We do this by creating
                     * a fundamentally broken shape which contains nothing but the original shape bytes.
                     * Oh, for shame. */
                    return new Shape() { OriginalBytes = data, OriginalFormat = tag };

                case Tag.DefineShape4:
                    newShape = this.ParseDefineShape4(shapeReader);
                    break;

                case Tag.DefineMorphShape:
                case Tag.DefineMorphShape2:
                    newShape = this.ParseDefineMorphShape(shapeReader, tag);
                    break;

                default:
                    throw new SWFModellerException(SWFModellerError.Internal, "Can't parse shapes with tag " + tag.ToString());
            }

            newShape.SetOriginalBytes(data, tag);
            return newShape;
        }

        private IShape ParseDefineMorphShape(SWFDataTypeReader shapeReader, Tag format)
        {
            Rect startBounds = shapeReader.ReadRect();
            shapeReader.Align8();
            Rect endBounds = shapeReader.ReadRect();
            shapeReader.Align8();

            Rect startEdgeBounds = null;
            Rect endEdgeBounds = null;
            bool usesNonScalingStrokes = false;
            bool usesScalingStrokes = false;

            if (format == Tag.DefineMorphShape2)
            {
                startEdgeBounds = shapeReader.ReadRect();
                shapeReader.Align8();
                endEdgeBounds = shapeReader.ReadRect();
                shapeReader.Align8();

                /*(void)*/shapeReader.ReadUBits(6); /* Reserved. Assume 0 */

                usesNonScalingStrokes = shapeReader.ReadBit();
                usesScalingStrokes = shapeReader.ReadBit();
            }

            /*(void)*/shapeReader.ReadUI32(); /* end edges offset. We don't need this */

            MorphFillStyle[] mfsa = this.ReadMorphFillStyleArray(shapeReader);

            MorphLineStyle[] mlsa = this.ReadMorphLineStyleArray(shapeReader, format);

            ShapeDef startShape = this.ReadShapeDef(shapeReader, format, false, mfsa, mlsa);
            ShapeDef endShape = this.ReadShapeDef(shapeReader, format, false, mfsa, mlsa);

            return new MorphShape()
            {
                Bounds = startBounds,
                EndBounds = endBounds,
                StartEdgeBounds = startEdgeBounds,
                EndEdgeBounds = endEdgeBounds,
                UsesNonScalingStrokes = usesNonScalingStrokes,
                UsesScalingStrokes = usesScalingStrokes,
                StartShape = startShape,
                EndShape = endShape
            };
        }

        private MorphLineStyle[] ReadMorphLineStyleArray(SWFDataTypeReader shapeReader, Tag format)
        {
            int lineCount = shapeReader.ReadUI8();
            if (lineCount == 0xFF)
            {
                lineCount = shapeReader.ReadUI16();
            }

            MorphLineStyle[] lineStyles = new MorphLineStyle[lineCount];
            if (format == Tag.DefineMorphShape)
            {
                for (int i = 0; i < lineCount; i++)
                {
                    lineStyles[i] = this.ReadMorphLineStyle(shapeReader);
                }
            }
            else /* Else Tag.DefineMorphShape2 */
            {
                for (int i = 0; i < lineCount; i++)
                {
                    lineStyles[i] = this.ReadMorphLineStyle2(shapeReader);
                }
            }

            return lineStyles;
        }

        private MorphLineStyle ReadMorphLineStyle(SWFDataTypeReader shapeReader)
        {
            return new MorphLineStyle()
            {
                StartWidth = shapeReader.ReadUI16(),
                EndWidth = shapeReader.ReadUI16(),
                StartColour = shapeReader.ReadRGBA(),
                EndColour = shapeReader.ReadRGBA()
            };
        }

        private MorphLineStyle ReadMorphLineStyle2(SWFDataTypeReader shapeReader)
        {
            int startwidth = shapeReader.ReadUI16();
            int endwidth = shapeReader.ReadUI16();

            CapStyle startCap = (CapStyle)shapeReader.ReadUBits(2);
            JoinStyle join = (JoinStyle)shapeReader.ReadUBits(2);

            bool hasFill = shapeReader.ReadBit();
            bool noHScaling = shapeReader.ReadBit();
            bool noVScaling = shapeReader.ReadBit();
            bool hasPixelHints = shapeReader.ReadBit();
            shapeReader.ReadUBits(5); // Reserved: 0
            bool noClose = shapeReader.ReadBit();

            CapStyle endCap = (CapStyle)shapeReader.ReadUBits(2);

            int? miterLimit = null;
            if (join == JoinStyle.Miter)
            {
                miterLimit = shapeReader.ReadUI16();
            }

            Color? startColour = null;
            Color? endColour = null;
            MorphFillStyle fs = null;

            if (hasFill)
            {
                fs = this.ReadMorphFillStyle(shapeReader);
            }
            else
            {
                startColour = shapeReader.ReadRGBA();
                endColour = shapeReader.ReadRGBA();
            }

            return new MorphLineStyle()
            {
                StartWidth = startwidth,
                EndWidth = endwidth,
                StartColour = startColour,
                EndColour = endColour,
                StartCap = startCap,
                EndCap = endCap,
                Join = join,
                HasFill = hasFill,
                NoHScaling = noHScaling,
                NoVScaling = noVScaling,
                HasPixelHints = hasPixelHints,
                FillStyle = fs,
                MiterLimit = miterLimit
            };
        }

        private IShape ParseDefineShape4(SWFDataTypeReader shapeReader)
        {
            Rect bounds = shapeReader.ReadRect();
            shapeReader.Align8();
            Rect edgeBounds = shapeReader.ReadRect();
            shapeReader.Align8();

            shapeReader.ReadUBits(5); /* Reserved: 0 */

            bool usesFillWinding = shapeReader.ReadBit();
            bool usesNonScalingStrokes = shapeReader.ReadBit();
            bool usesScalingStrokes = shapeReader.ReadBit();

            ShapeDef sws = this.ReadShapeDef(shapeReader, Tag.DefineShape4, true, null, null);

            return new Shape()
            {
                ShapeDef = sws,
                Bounds = bounds,
                UsesScalingStrokes = usesScalingStrokes,
                UsesNonScalingStrokes = usesNonScalingStrokes,
                UsesFillWinding = usesFillWinding
            };
        }

        private IShape ParseDefineShapeN(SWFDataTypeReader shapeReader, Tag format)
        {
            Rect bounds = shapeReader.ReadRect();
            shapeReader.Align8();
            ShapeDef sws = this.ReadShapeDef(shapeReader, format, true, null, null);

            return new Shape()
            {
                ShapeDef = sws,
                Bounds = bounds
            };
        }

        private MorphFillStyle[] ReadMorphFillStyleArray(SWFDataTypeReader shapeReader)
        {
            int fillCount = shapeReader.ReadUI8();
            if (fillCount == 0xFF)
            {
                fillCount = shapeReader.ReadUI16();
            }

            MorphFillStyle[] fillStyles = new MorphFillStyle[fillCount];
            for (int i = 0; i < fillCount; i++)
            {
                fillStyles[i] = this.ReadMorphFillStyle(shapeReader);
            }

            return fillStyles;
        }

        private MorphFillStyle ReadMorphFillStyle(SWFDataTypeReader shapeReader)
        {
            MorphFillStyle style = new MorphFillStyle();

            style.Type = (FillType)shapeReader.ReadUI8();

            if (style.Type == FillType.Solid)
            {
                style.StartColour = shapeReader.ReadRGBA();
                style.EndColour = shapeReader.ReadRGBA();
            }

            if (style.Type == FillType.LinearGradient
                    || style.Type == FillType.RadialGradient)
            {
                style.StartFillMatrix = shapeReader.ReadMatrix();
                shapeReader.Align8();

                style.EndFillMatrix = shapeReader.ReadMatrix();
                shapeReader.Align8();

                if (style.Type == FillType.LinearGradient
                        || style.Type == FillType.RadialGradient)
                {
                    style.Gradient = this.ReadMorphGradient(shapeReader);
                }
            }

            if (FillTypes.IsBitmap(style.Type))
            {
                int cid = shapeReader.ReadUI16();
                /* Some fills have this magic number in them which seems to deliberately not
                 * reference a bitmap. The spec is silent on the matter. Oh flash, you
                 * bumbling simian. */
                if (cid != 0x0000FFFF)
                {
                    style.Bitmap = this.ImageFinder.FindImage(cid);
                }

                style.StartFillMatrix = shapeReader.ReadMatrix();
                shapeReader.Align8();

                style.EndFillMatrix = shapeReader.ReadMatrix();
                shapeReader.Align8();
            }

            return style;
        }

        private MorphGradient ReadMorphGradient(SWFDataTypeReader shapeReader)
        {
            int numGrads = shapeReader.ReadUI8();

            MorphGradient mg = new MorphGradient()
            {
                Records = new MorphGradientRecord[numGrads]
            };

            for (int i = 0; i < numGrads; i++)
            {
                mg.Records[i] = this.ReadMorphGradientRecord(shapeReader);
            }

            return mg;
        }

        private MorphGradientRecord ReadMorphGradientRecord(SWFDataTypeReader shapeReader)
        {
            MorphGradientRecord mgr = new MorphGradientRecord();
            mgr.StartRatio = shapeReader.ReadUI8();
            mgr.StartColour = shapeReader.ReadRGBA();
            mgr.EndRatio = shapeReader.ReadUI8();
            mgr.EndColour = shapeReader.ReadRGBA();
            return mgr;
        }

        /* ISSUE 20: Instead of passing the format and reader around, perhaps they should be
         * members? Perhaps? Hmm? */
        private FillStyle[] ReadFillStyleArray(SWFDataTypeReader shapeReader, Tag format)
        {
            int fillCount = shapeReader.ReadUI8();
            if (fillCount == 0xFF && (format == Tag.DefineShape3 || format == Tag.DefineShape2))
            {
                fillCount = shapeReader.ReadUI16();
            }

            FillStyle[] fillStyles = new FillStyle[fillCount];
            for (int i = 0; i < fillCount; i++)
            {
                fillStyles[i] = this.ReadFillStyle(shapeReader, format);
            }

            return fillStyles;
        }

        private LineStyle[] ReadLineStyleArray(SWFDataTypeReader shapeReader, Tag format)
        {
            int lineCount = shapeReader.ReadUI8();
            if (lineCount == 0xFF)
            {
                lineCount = shapeReader.ReadUI16();
            }

            LineStyle[] lineStyles = new LineStyle[lineCount];
            if (format == Tag.DefineShape4)
            {
                for (int i = 0; i < lineCount; i++)
                {
                    lineStyles[i] = this.ReadLineStyle2(shapeReader, format);
                }
            }
            else
            {
                for (int i = 0; i < lineCount; i++)
                {
                    lineStyles[i] = this.ReadLineStyle(shapeReader, format);
                }
            }

            return lineStyles;
        }

        private ShapeDef ReadShapeDef(SWFDataTypeReader shapeReader, Tag format, bool withStyle, IFillStyle[] fillStyles, ILineStyle[] lineStyles)
        {
            ShapeDef shapeDef = new ShapeDef();

            /* Shapes either don't have fill styles (Font glyphs), they come with a bunch of fill styles
             * (Regular shapes) or are preceeded by fill styles which are passed into this method
             * (Morph shapes). Could probably be tidier... */

            if (fillStyles != null)
            {
                shapeDef.FillStyles.AddRange(fillStyles);
            }

            if (lineStyles != null)
            {
                shapeDef.LineStyles.AddRange(lineStyles);
            }

            if (withStyle)
            {
                shapeDef.FillStyles.AddRange(this.ReadFillStyleArray(shapeReader, format));
                shapeReader.Align8();
                shapeDef.LineStyles.AddRange(this.ReadLineStyleArray(shapeReader, format));
                shapeReader.Align8();
            }

            /* Read the shape stuff... */

            int fillBits = (int)shapeReader.ReadUBits(4);
            int lineBits = (int)shapeReader.ReadUBits(4);

            this.ReadShapeRecordsInto(shapeDef, shapeReader, ref fillBits, ref lineBits, format);

            return shapeDef;
        }

        private void ReadShapeRecordsInto(ShapeDef sws, SWFDataTypeReader shapeReader, ref int fillBits, ref int lineBits, Tag format)
        {
            List<IShapeRecord> records = new List<IShapeRecord>();

            int currentX = 0;
            int currentY = 0;

            while (true)
            {
                bool isEdgeRecord = shapeReader.ReadBit();
                if (isEdgeRecord)
                {
                    if (shapeReader.ReadBit())
                    {
                        /* StraightEdgeRecord */
                        int bpv = 2 + (int)shapeReader.ReadUBits(4);

                        bool isGeneralLine = shapeReader.ReadBit();
                        bool isVertical = false;
                        if (!isGeneralLine)
                        {
                            isVertical = shapeReader.ReadBit();
                        }

                        int dx = 0;
                        int dy = 0;

                        if (isGeneralLine || !isVertical)
                        {
                            dx = shapeReader.ReadSBits(bpv);
                        }

                        if (isGeneralLine || isVertical)
                        {
                            dy = shapeReader.ReadSBits(bpv);
                        }

                        currentX += dx;
                        currentY += dx;

                        records.Add(new StraightEdge() { DX = dx, DY = dy });
                    }
                    else
                    {
                        /* CurvedEdgeRecord */
                        int bpv = 2 + (int)shapeReader.ReadUBits(4);

                        int ctrlDX = shapeReader.ReadSBits(bpv);
                        int ctrlDY = shapeReader.ReadSBits(bpv);
                        int anchorDX = shapeReader.ReadSBits(bpv);
                        int anchorDY = shapeReader.ReadSBits(bpv);

                        currentX += ctrlDX + anchorDX;
                        currentY += ctrlDY + anchorDY;

                        records.Add(new CurvedEdge() { AnchorDX = anchorDX, AnchorDY = anchorDY, CtrlDX = ctrlDX, CtrlDY = ctrlDY });
                    }
                }
                else
                {
                    uint flags = shapeReader.ReadUBits(5);

                    if (flags == 0)
                    {
                        /* EndShapeRecord */
                        break;
                    }

                    /* StyleChangeRecord */

                    bool stateMoveTo = (flags & 1) == 1;
                    flags >>= 1;
                    bool stateFillStyle0 = (flags & 1) == 1;
                    flags >>= 1;
                    bool stateFillStyle1 = (flags & 1) == 1;
                    flags >>= 1;
                    bool stateLineStyle = (flags & 1) == 1;
                    flags >>= 1;
                    bool stateNewStyles = (flags & 1) == 1;
                    flags >>= 1;

                    StyleChange sc = new StyleChange();

                    if (stateMoveTo)
                    {
                        int moveBits = (int)shapeReader.ReadUBits(5);

                        sc.DX = shapeReader.ReadSBits(moveBits);
                        sc.DY = shapeReader.ReadSBits(moveBits);

                        currentX = sc.DX.Value;
                        currentY = sc.DY.Value;
                    }

                    if (stateFillStyle0)
                    {
                        sc.FillStyle0 = sws.FillFromIndex((int)shapeReader.ReadUBits(fillBits));
                    }

                    if (stateFillStyle1)
                    {
                        sc.FillStyle1 = sws.FillFromIndex((int)shapeReader.ReadUBits(fillBits));
                    }

                    if (stateLineStyle)
                    {
                        sc.LineStyle = (int)shapeReader.ReadUBits(lineBits);
                    }

                    if (stateNewStyles)
                    {
                        sc.NewFillStyles = this.ReadFillStyleArray(shapeReader, format);
                        sc.NewLineStyles = this.ReadLineStyleArray(shapeReader, format);

                        fillBits = (int)shapeReader.ReadUBits(4);
                        lineBits = (int)shapeReader.ReadUBits(4);

                        /* ISSUE 21: We're storing new styles defined in shape records in two places and
                         * in such a way that we can't figure out where we got them from. This makes
                         * it impossible to reconstruct the SWF data. Kinda need to work out how to
                         * find styles. */

                        sws.FillStyles.AddRange(sc.NewFillStyles);
                        sws.LineStyles.AddRange(sc.NewLineStyles);
                    }

                    records.Add(sc);
                }
            }

            shapeReader.Align8();

            sws.Records = records.ToArray();
        }

        private FillStyle ReadFillStyle(SWFDataTypeReader shapeReader, Tag format)
        {
            FillStyle style = new FillStyle();

            style.Type = (FillType)shapeReader.ReadUI8();

            if (style.Type == FillType.Solid)
            {
                if (format == Tag.DefineShape3 || format == Tag.DefineShape4) /* Assuming shape4 goes here. Spec is ambiguous. */
                {
                    style.Colour = shapeReader.ReadRGBA();
                }
                else if (format == Tag.DefineShape || format == Tag.DefineShape2)
                {
                    style.Colour = shapeReader.ReadRGB();
                }
                else
                {
                    throw new SWFModellerException(SWFModellerError.SWFParsing, "Bad tag format for fill style");
                }
            }

            if (style.Type == FillType.LinearGradient
                    || style.Type == FillType.RadialGradient
                    || style.Type == FillType.FocalGradient)
            {
                style.FillMatrix = shapeReader.ReadMatrix();
                shapeReader.Align8();

                if (style.Type == FillType.LinearGradient
                        || style.Type == FillType.RadialGradient)
                {
                    style.Gradient = this.ReadGradient(shapeReader, format);
                }
                else /* FocalGradient */
                {
                    style.Gradient = this.ReadFocalGradient(shapeReader);
                }
            }

            if (FillTypes.IsBitmap(style.Type))
            {
                int cid = shapeReader.ReadUI16();
                /* Some fills have this magic number in them which seems to deliberately not
                 * reference a bitmap. The spec is silent on the matter. Oh flash, you
                 * stammering ape. */
                if (cid != 0x0000FFFF)
                {
                    style.Bitmap = this.ImageFinder.FindImage(cid);
                }

                style.FillMatrix = shapeReader.ReadMatrix();
                shapeReader.Align8();
            }

            return style;
        }

        private FocalGradient ReadFocalGradient(SWFDataTypeReader shapeReader)
        {
            /* ISSUE 72 */
            throw new SWFModellerException(
                    SWFModellerError.UnimplementedFeature,
                    "Can't parse focal gradients yet.");
        }

        private Gradient ReadGradient(SWFDataTypeReader shapeReader, Tag format)
        {
            GradientSpread spread = (GradientSpread)shapeReader.ReadUBits(2);
            GradientInterpolation interp = (GradientInterpolation)shapeReader.ReadUBits(2);

            int numRecs = (int)shapeReader.ReadUBits(4);

            GradientRecord[] recs = new GradientRecord[numRecs];

            for (int i = 0; i < recs.Length; i++)
            {
                GradientRecord rec = new GradientRecord();
                rec.Ratio = shapeReader.ReadUI8();
                if (format == Tag.DefineShape || format == Tag.DefineShape2)
                {
                    rec.Colour = shapeReader.ReadRGB();
                }
                else if (format == Tag.DefineShape3 || format == Tag.DefineShape4)
                {
                    rec.Colour = shapeReader.ReadRGBA();
                }
                else
                {
                    throw new SWFModellerException(SWFModellerError.Internal, "Can't read gradient in shape format " + format.ToString());
                }

                recs[i] = rec;
            }

            return new Gradient()
            {
                Records = recs,
                Interpolation = interp,
                Spread = spread
            };
        }

        private LineStyle ReadLineStyle(SWFDataTypeReader shapeReader, Tag format)
        {
            LineStyle ls = new LineStyle();

            ls.Width = shapeReader.ReadUI16();

            if (format == Tag.DefineShape || format == Tag.DefineShape2)
            {
                ls.Colour = shapeReader.ReadRGB();
            }
            else if (format == Tag.DefineShape3 || format == Tag.DefineShape4)
            {
                ls.Colour = shapeReader.ReadRGBA();
            }
            else
            {
                throw new SWFModellerException(SWFModellerError.Internal, "Can't line style in shape format " + format.ToString());
            }

            return ls;
        }

        private LineStyle ReadLineStyle2(SWFDataTypeReader shapeReader, Tag format)
        {
            int width = shapeReader.ReadUI16();
            CapStyle startCap = (CapStyle)shapeReader.ReadUBits(2);
            JoinStyle join = (JoinStyle)shapeReader.ReadUBits(2);
            bool hasFill = shapeReader.ReadBit();
            bool noHScaling = shapeReader.ReadBit();
            bool noVScaling = shapeReader.ReadBit();
            bool hasPixelHints = shapeReader.ReadBit();
            shapeReader.ReadUBits(5); /* Reserved: 0 */
            bool noClose = shapeReader.ReadBit();
            CapStyle endCap = (CapStyle)shapeReader.ReadUBits(2);

            int? miterLimit = null;
            if (join == JoinStyle.Miter)
            {
                miterLimit = shapeReader.ReadUI16();
            }

            Color? c = null;
            FillStyle fs = null;

            if (hasFill)
            {
                fs = this.ReadFillStyle(shapeReader, format);
            }
            else
            {
                c = shapeReader.ReadRGBA();
            }

            return new LineStyle()
            {
                Width = width,
                StartCap = startCap,
                EndCap = endCap,
                Join = join,
                HasFill = hasFill,
                NoHScaling = noHScaling,
                NoVScaling = noVScaling,
                HasPixelHints = hasPixelHints,
                Colour = c,
                FillStyle = fs,
                MiterLimit = miterLimit
            };
        }
    }
}
