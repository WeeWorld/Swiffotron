//-----------------------------------------------------------------------
// SWFReader.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using SWFProcessing.SWFModeller.ABC;
    using SWFProcessing.SWFModeller.ABC.Debug;
    using SWFProcessing.SWFModeller.Characters;
    using SWFProcessing.SWFModeller.Characters.Geom;
    using SWFProcessing.SWFModeller.Characters.Images;
    using SWFProcessing.SWFModeller.Characters.Shapes;
    using SWFProcessing.SWFModeller.Characters.Shapes.IO;
    using SWFProcessing.SWFModeller.Characters.Text;
    using SWFProcessing.SWFModeller.IO;
    using SWFProcessing.SWFModeller.Modelling;
    using SWFProcessing.SWFModeller.Text;
    using DBug = System.Diagnostics;
    using SWFProcessing.SWFModeller.Process;

    public class SWFReader : IDisposable, IImageFinder
    {
        private SWFDataTypeReader sdtr;
        private uint fileLen;
        private Rect frameSize;
        private float fps;
        private ushort frameCount;
        private int version;
        private SWF swf;
        private int frameCursor;
        private Timeline currentTimeline;
        private Dictionary<int, ICharacter> characterUnmarshaller;
        private List<FrameNote> sceneNotes;
        private List<FrameNote> frameLabelNotes;
        private int doAbcCount;
        private Dictionary<int, SWFFont> fontDict;
        private JPEGTable jpegTable;

        private const int SIG_UNCOMPRESSED = 0x535746; /* S, W, F */
        private const int SIG_COMPRESSED = 0x535743; /* S, W, C */

        public SWFReaderOptions options;

        private Dictionary<string, Timeline> LateClassResolutions;

#if DEBUG
        /// <summary>
        /// The tag log appears in debug builds as a simple way of seeing which
        /// tags were read before a tag you're interested in whilst inspecting the
        /// code in the debugger.
        /// </summary>
        private List<string> taglog = new List<string>();
        private IABCLoadInterceptor abcInterceptor;
        private int binaryDumpNest = 0;
        private StringBuilder binaryDump;
#endif
        /// <summary>
        /// Internally, characters are stored by string ids, so we create one
        /// by prefixing the SWF numeric IDs with this string. Technically, we'd be
        /// better off just not putting these in the dictionary at all, since they'll
        /// never be retrieved, but since the dictionary is dumped in unit test output
        /// it does have a kinda usefulness.
        /// </summary>
        private const string CID_PREFIX = @"__cid##";

        /// <summary>
        /// Initializes a new instance of a SWF parser.
        /// </summary>
        /// <param name="swfIn">A stream with SWF data to read from.</param>
        /// <param name="binaryDump">Only has an effect in debug builds.</param>
        /// <param name="abcInterceptor">Only has an effect in debug builds.</param>
        /// <param name="dbugConstFilter">Only has an effect in debug builds.</param>
        public SWFReader(
                Stream swfIn,
                SWFReaderOptions options,
                StringBuilder binaryDump,
                IABCLoadInterceptor abcInterceptor)
        {
            this.options = options;
            this.sdtr = new SWFDataTypeReader(swfIn);
            this.characterUnmarshaller = new Dictionary<int, ICharacter>();
            this.LateClassResolutions = new Dictionary<string, Timeline>();
            this.doAbcCount = 0;

#if DEBUG
            this.binaryDump = binaryDump;
            this.binaryDumpNest = 0;
            this.abcInterceptor = abcInterceptor;
#endif
        }

        private bool ReadTag()
        {
            int type;
            uint followingOffset;
            this.sdtr.ReadRECORDHEADER(out type, out followingOffset);
            uint startOffset = this.sdtr.Offset;

#if DEBUG
            Tag _tag = (Tag)type;
            bool isDefine = _tag == Tag.DefineShape ||
                     _tag == Tag.DefineShape3 ||
                     _tag == Tag.DefineShape4 ||
                     _tag == Tag.DefineSprite;
            this.MarkDumpOffset(
                    "Body of " + _tag + " (" + type + ") len=" + (followingOffset - this.sdtr.Offset),
                    isDefine);
            this.binaryDumpNest++;
            this.taglog.Add(_tag.ToString());
#endif

            switch ((Tag)type)
            {
                case Tag.End:
#if DEBUG
                    this.binaryDumpNest--;
#endif
                    return false;

                case Tag.ShowFrame:
#if DEBUG
                    this.MarkDumpOffset("");
#endif
                    this.frameCursor++;
                    break;

                case Tag.Protect:
                    this.sdtr.Align8();
                    if (followingOffset > this.sdtr.Offset)
                    {
                        /*(void)*/this.sdtr.ReadUI16(); /* Reserved. Assumed 0. */
                        this.swf.ProtectHash = this.sdtr.ReadString();
#if DEBUG
                        this.Log("Protect hash = " + this.swf.ProtectHash);
#endif
                    }
                    break;

                case Tag.SetBackgroundColor:
                    this.swf.BackgroundColor = this.sdtr.ReadRGB();
                    break;

                case Tag.PlaceObject:
                    this.currentTimeline.GetFrame(this.frameCursor).AddTag(this.ReadPlaceObject(followingOffset));
                    break;

                case Tag.PlaceObject2:
                    this.currentTimeline.GetFrame(this.frameCursor).AddTag(this.ReadPlaceObject2());
                    break;

                case Tag.RemoveObject2:
                    this.currentTimeline.GetFrame(this.frameCursor).AddTag(this.ReadRemoveObject2());
                    break;

                case Tag.DefineBits:
                case Tag.DefineBitsJPEG2:
                case Tag.DefineBitsLossless:
                case Tag.DefineBitsLossless2:
                    this.ReadImageBlob((Tag)type, followingOffset);
                    break;

                case Tag.JPEGTables:
                    jpegTable = new JPEGTable(this.sdtr.ReadByteBlock((int)(followingOffset - this.sdtr.Offset)));
                    break;

                case Tag.DefineSprite:
                    this.ReadSprite();
                    break;

                case Tag.DefineShape:
                case Tag.DefineShape2:
                case Tag.DefineShape3:
                case Tag.DefineShape4:
                    this.ReadDefineShapeN((Tag)type, followingOffset);
                    break;

                case Tag.DefineMorphShape:
                case Tag.DefineMorphShape2:
                    this.ReadDefineMorphShape((Tag)type, followingOffset);
                    break;

                case Tag.DefineSceneAndFrameLabelData:
                    this.ReadSceneAndFrameLabelData();
                    break;

                case Tag.DoABC:
                    this.ReadDoABC(followingOffset);
                    break;

                case Tag.SymbolClass:
                    this.ReadSymbolClass();
                    break;

                case Tag.Metadata:
                    this.ReadXMLMetadata();
                    break;

                case Tag.EnableDebugger2:
                    this.ReadEnableDebugger2();
                    break;

                case Tag.FrameLabel:
                    this.currentTimeline.GetFrame(this.frameCursor).Label = this.sdtr.ReadString();
#if DEBUG
                    this.Log("Frame label = " + this.currentTimeline.GetFrame(this.frameCursor).Label);
#endif

                    break;

                case Tag.DefineFont3:
                    this.ReadFont((Tag)type);
                    break;

                case Tag.DefineFontAlignZones:
                    this.ReadFontAlignZones();
                    break;

                case Tag.DefineFontName:
                    this.ReadFontName();
                    break;

                case Tag.DefineText:
                case Tag.DefineText2:
                    this.ReadText((Tag)type);
                    break;

                case Tag.DefineEditText:
                    this.ReadEditText();
                    break;

                default:
                    /* ISSUE 73 */
                    throw new SWFModellerException(
                            SWFModellerError.UnimplementedFeature,
                            @"Unsupported tag type: " + type, swf.Context);
            }

            this.FinishTag(followingOffset);
#if DEBUG
            this.binaryDumpNest--;
#endif

            return true;
        }

        private void ReadImageBlob(Tag tag, uint followingOffset)
        {
            int id = this.sdtr.ReadUI16();

#if DEBUG
            this.Log("char id=" + id);
#endif

            byte[] data = this.sdtr.ReadByteBlock((int)(followingOffset - this.sdtr.Offset));

            ImageBlob image = new ImageBlob()
            {
                DataFormat = tag,
                FormattedBytes = data
            };

            if (tag == Tag.DefineBits)
            {
                if (jpegTable == null)
                {
                    throw new SWFModellerException(SWFModellerError.SWFParsing,
                            "DefineBits tag without a JPEGTables tag is illegal.", swf.Context);
                }
                image.JPEGTable = jpegTable;
            }

            this.characterUnmarshaller.Add(id, image);
            this.swf.AddCharacter(CID_PREFIX + id, image);
        }

        private void ReadText(Tag format)
        {
            int cid = this.sdtr.ReadUI16();

            StaticText text = this.swf.NewStaticText(CID_PREFIX + cid); /* ISSUE 38: Should this silly name thing be wrapped into SWF? */
            this.characterUnmarshaller.Add(cid, text);

            text.Bounds = this.sdtr.ReadRect();
            this.sdtr.Align8();
            text.Position = this.sdtr.ReadMatrix();
            this.sdtr.Align8();

            int glyphBits = this.sdtr.ReadUI8();
            int advanceBits = this.sdtr.ReadUI8();

            int flags = 0;
            while((flags = this.sdtr.ReadUI8()) != 0)
            {
                TextRecord tr = new TextRecord();

                if ((flags & 0xF0) != 0x80)
                {
                    /* Top 4 bits must match 1000xxxx */
                    throw new SWFModellerException(SWFModellerError.SWFParsing, "Bad flags in DefineText", swf.Context);
                }

                bool hasFont = ((flags & 0x08) != 0);
                bool hasColour = ((flags & 0x04) != 0);
                bool hasYOffset = ((flags & 0x02) != 0);
                bool hasXOffset = ((flags & 0x01) != 0);

                if (hasFont)
                {
                    tr.Font = fontDict[this.sdtr.ReadUI16()];
                }

                if (hasColour)
                {
                    if (format == Tag.DefineText2)
                    {
                        tr.Colour = this.sdtr.ReadRGBA();
                    }
                    else /* assume DefineText */
                    {
                        tr.Colour = this.sdtr.ReadRGB();
                    }
                }

                if (hasXOffset)
                {
                    tr.XOffset = this.sdtr.ReadSI16();
                }

                if (hasYOffset)
                {
                    tr.YOffset = this.sdtr.ReadSI16();
                }

                if (hasFont)
                {
                    tr.FontHeight = this.sdtr.ReadUI16();
                }

                int glyphCount = this.sdtr.ReadUI8();
                char[] glyphIndex = tr.Font.CodePoints;
                int[] advances = new int[glyphCount];
                StringBuilder sb = new StringBuilder(glyphCount);
                for (int i = 0; i < glyphCount; i++)
                {
                    sb.Append(glyphIndex[(int)this.sdtr.ReadUBits(glyphBits)]);
                    advances[i] = this.sdtr.ReadSBits(advanceBits);
                }
                tr.Text = sb.ToString();
                tr.Advances = advances;

                text.Records.Add(tr);

                this.sdtr.Align8();
            }
        }

        private void ReadEditText()
        {
            int cid = this.sdtr.ReadUI16();

            EditText editText = this.swf.NewEditText(CID_PREFIX + cid);
            this.characterUnmarshaller.Add(cid, editText);

            editText.Bounds = this.sdtr.ReadRect();

#if DEBUG
            this.Log("id=" + cid + ", bounds=" + editText.Bounds);
#endif

            /* ISSUE 39: This might be faster with a ReadUI16 and some masks, if we care
             * about such micro-optimisations */

            this.sdtr.Align8();

            bool hasText = this.sdtr.ReadBit();
            editText.WordWrapEnabled = this.sdtr.ReadBit();
            editText.IsMultiline = this.sdtr.ReadBit();
            editText.IsPassword = this.sdtr.ReadBit();

            editText.IsReadOnly = this.sdtr.ReadBit();
            bool hasTextColour = this.sdtr.ReadBit();
            bool hasMaxLength = this.sdtr.ReadBit();
            bool hasFont = this.sdtr.ReadBit();

            bool hasFontClass = this.sdtr.ReadBit();
            editText.IsAutoSized = this.sdtr.ReadBit();
            bool hasLayout = this.sdtr.ReadBit();
            editText.IsNonSelectable = this.sdtr.ReadBit();

            editText.HasBorder = this.sdtr.ReadBit();
            editText.IsStatic = this.sdtr.ReadBit();
            editText.IsHTML = this.sdtr.ReadBit();
            editText.UseOutlines = this.sdtr.ReadBit();

            if (hasFont)
            {
                editText.Font = fontDict[this.sdtr.ReadUI16()];
            }

            if (!hasFont && hasFontClass)
            {
                /* ISSUE 14 */
                throw new SWFModellerException(SWFModellerError.UnimplementedFeature,
                        "Font classes are not yet supported.", swf.Context);
            }

            if (hasFont)
            {
                editText.FontHeight = this.sdtr.ReadUI16();
            }

            if (hasTextColour)
            {
                editText.Color = this.sdtr.ReadRGBA();
            }

            if (hasMaxLength)
            {
                editText.MaxLength = this.sdtr.ReadUI16();
            }

            if (hasLayout)
            {
                editText.LayoutInfo = new EditText.Layout()
                {
                    Align = (EditText.Alignment)this.sdtr.ReadUI8(),
                    LeftMargin = this.sdtr.ReadUI16(),
                    RightMargin = this.sdtr.ReadUI16(),
                    Indent = this.sdtr.ReadUI16(),
                    Leading = this.sdtr.ReadSI16()
                };
            }

            editText.VarName = this.sdtr.ReadString();

            if (hasText)
            {
                editText.Text = this.sdtr.ReadString();
            }
        }

        private void ReadFontName()
        {
            SWFFont font = fontDict[this.sdtr.ReadUI16()];
            if (font == null)
            {
                throw new SWFModellerException(SWFModellerError.SWFParsing, "Bad font ID in font name data", swf.Context);
            }

            font.FullName = this.sdtr.ReadString();
            font.Copyright = this.sdtr.ReadString();
        }

        private void ReadFontAlignZones()
        {
            SWFFont font = fontDict[this.sdtr.ReadUI16()];
            if (font == null)
            {
                throw new SWFModellerException(SWFModellerError.SWFParsing, "Bad font ID in pixel alignment data", swf.Context);
            }

            font.ThicknessHint = (SWFFont.Thickness)this.sdtr.ReadUBits(2);
            /*(void)*/this.sdtr.ReadUBits(6); /* Reserved */

            char[] codes = font.CodePoints;
            for (int i = 0; i < codes.Length; i++)
            {
                PixelAlignment alignment = new PixelAlignment();

                alignment.ZoneInfo = new PixelAlignment.ZoneData[this.sdtr.ReadUI8()];
                for (int j = 0; j < alignment.ZoneInfo.Length; j++)
                {
                    alignment.ZoneInfo[j] = new PixelAlignment.ZoneData()
                    {
                        AlignmentCoord = this.sdtr.ReadUI16(),
                        Range = this.sdtr.ReadUI16()
                    };
                }

                /*(void)*/this.sdtr.ReadUBits(6); /* Reserved */

                alignment.HasY = this.sdtr.ReadBit();
                alignment.HasX = this.sdtr.ReadBit();

                font.AddPixelAlignment(codes[i], alignment);
            }
        }

        private void ReadFont(Tag fontType)
        {
            int fontID = this.sdtr.ReadUI16();

            /* Bunch of flags */
            bool hasLayout = this.sdtr.ReadBit();
            bool isShiftJIS = this.sdtr.ReadBit();
            bool isSmallText = this.sdtr.ReadBit();
            bool isANSI = this.sdtr.ReadBit();
            bool isWideOffsets = this.sdtr.ReadBit();
            bool isWideCodes = this.sdtr.ReadBit();
            bool isItalic = this.sdtr.ReadBit();
            bool isBold = this.sdtr.ReadBit();

            if (!isWideCodes)
            {
                throw new SWFModellerException(SWFModellerError.SWFParsing,
                        "Non-wide codes in font encodings are not valid.", swf.Context);
            }

            if (isShiftJIS)
            {
                /* ISSUE 50 */
                throw new SWFModellerException(SWFModellerError.UnimplementedFeature,
                        "ShiftJIS character encoding is not supported.", swf.Context);
            }

            int language = this.sdtr.ReadUI8();
            string name = this.sdtr.ReadLengthedUTF8(this.sdtr.ReadUI8());
            int numGlyphs = this.sdtr.ReadUI16();
#if DEBUG
            this.Log("id=" + fontID + ", name=" + name);
#endif

            SWFFont font = new SWFFont(
                    (SWFFont.Language)language,
                    name,
                    isBold,
                    isItalic,
                    isSmallText,
                    numGlyphs);

            int startOffset = (int)this.sdtr.Offset; /* The offset table measures from this point. */

            int[] shapeOffsets = new int[numGlyphs];
            for (int i = 0; i < numGlyphs; i++)
            {
                shapeOffsets[i] = isWideOffsets ? (int)this.sdtr.ReadUI32() : this.sdtr.ReadUI16();
            }

            int codeTableOffset = isWideOffsets ? (int)this.sdtr.ReadUI32() : this.sdtr.ReadUI16();

            IShape[] shapes = new IShape[numGlyphs];

            for (int i = 0; i < numGlyphs; i++)
            {
                int shapeOffset = (int)sdtr.Offset - startOffset;
                if (shapeOffsets[i] != shapeOffset)
                {
                    throw new SWFModellerException(SWFModellerError.SWFParsing, "Bad font data.", swf.Context);
                }

                int end = codeTableOffset;
                if (i < numGlyphs - 1)
                {
                    end = shapeOffsets[i + 1];
                }

                int len = end - shapeOffset;

                byte[] shapeData = this.sdtr.ReadByteBlock(len);

                shapes[i] = new ShapeParser().Parse(fontType, shapeData, this);
            }

            char[] codes = new char[numGlyphs];

            for (int i = 0; i < numGlyphs; i++)
            {
                codes[i] = (char)this.sdtr.ReadUI16();
                font.AddGlyph(codes[i], shapes[i]);
            }


            if (hasLayout)
            {
                font.Ascent = this.sdtr.ReadSI16();
                font.Descent = this.sdtr.ReadSI16();
                font.Leading = this.sdtr.ReadSI16();

                GlyphLayout[] layouts = new GlyphLayout[numGlyphs];

                for (int i = 0; i < numGlyphs; i++)
                {
                    layouts[i] = new GlyphLayout()
                    {
                        Advance = this.sdtr.ReadSI16()
                    };
                }

                for (int i = 0; i < numGlyphs; i++)
                {
                    layouts[i].Bounds = this.sdtr.ReadRect();
                    font.AddLayout(codes[i], layouts[i]);
                }

                int kerningCount = this.sdtr.ReadUI16();
                KerningPair[] kernTable = new KerningPair[kerningCount];

                for (int i = 0; i < kerningCount; i++)
                {
                    kernTable[i] = new KerningPair()
                    {
                        LeftChar = (char)(isWideCodes ? this.sdtr.ReadUI16() : this.sdtr.ReadUI8()),
                        RightChar = (char)(isWideCodes ? this.sdtr.ReadUI16() : this.sdtr.ReadUI8()),
                        Adjustment = this.sdtr.ReadSI16()
                    };
                }

                font.KerningTable = kernTable;
            }

            fontDict.Add(fontID, font);
            swf.AddFont(font);
        }

        #region IDisposable Members

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.sdtr != null)
            {
                this.sdtr.Dispose();
                this.sdtr = null;
            }
        }

        #endregion

        /// <summary>
        /// Reads a SWF from the stream.
        /// </summary>
        /// <returns>A parsed SWF object</returns>
        public SWF ReadSWF(SWFContext ctx)
        {
#if DEBUG
            this.MarkDumpOffset("Start of file");
#endif

            this.jpegTable = null;

            this.sceneNotes = new List<FrameNote>();
            this.frameLabelNotes = new List<FrameNote>();

            this.ReadHeader(ctx);

            this.fontDict = new Dictionary<int, SWFFont>();

            this.frameCursor = 1;

            bool as3 = false;
            if (this.version >= 8)
            {
#if DEBUG
                this.MarkDumpOffset("Start of file attributes tag");
#endif
                this.ReadFileAttributesTag(out as3);
            }

            if (!as3)
            {
                throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        @"AS2 and under is not supported.", ctx);
            }

            this.swf = new SWF(ctx, false);
            this.swf.FrameWidth = this.frameSize.Width;
            this.swf.FrameHeight = this.frameSize.Height;
            this.swf.Fps = this.fps;
            this.swf.FrameCount = this.frameCount;

            this.currentTimeline = this.swf;

            bool hasMore = true;
            do
            {
                hasMore = this.ReadTag();
            }
            while (hasMore);

            foreach (FrameNote note in this.sceneNotes)
            {
                this.swf.GetFrame((int)note.frame).SceneName = note.note;
            }

            foreach (FrameNote note in this.frameLabelNotes)
            {
                this.swf.GetFrame((int)note.frame).Label = note.note;
            }

            foreach (string className in this.LateClassResolutions.Keys)
            {
                swf.MapClassnameToClip(className, this.LateClassResolutions[className]);
            }

            return this.swf;
        }

        private void FinishTag(uint followingOffset)
        {
            if (this.sdtr.Offset < followingOffset)
            {
                if (options.StrictTagLength)
                {
                    throw new SWFModellerException(
                            SWFModellerError.SWFParsing,
                            "Incorrect tag length at offset @" + this.sdtr.Offset, swf.Context);
                }
                else
                {
                    DBug.Debug.Print(@"WARNING: Skipping bytes at offset " + this.sdtr.Offset);
                    this.sdtr.SkipForwardTo(followingOffset);
                }
            }
            else if (this.sdtr.Offset > followingOffset)
            {
                throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        @"Tag over-ran its length by " + (followingOffset - this.sdtr.Offset) + @" bytes", swf.Context);
            }
        }

        private RemoveObject ReadRemoveObject2()
        {
            int depth = this.sdtr.ReadUI16();

#if DEBUG
            this.Log("depth=" + depth+", frameCursor="+this.frameCursor);
#endif
            RemoveObject ro = new RemoveObject(this.currentTimeline.GetLayer(depth));
            return ro;
        }

        private PlaceObject ReadPlaceObject(uint followingOffset)
        {
            int id = this.sdtr.ReadUI16();
            int depth = this.sdtr.ReadUI16();
            Matrix matrix = this.sdtr.ReadMatrix();

#if DEBUG
            this.Log("id=" + id + ", depth=" + depth + " matrix=" + matrix);
#endif

            ColorTransform cxform = null;
            if (followingOffset > this.sdtr.Offset)
            {
                cxform = this.sdtr.ReadColorTransform();
#if DEBUG
                this.Log("cxform=" + cxform);
#endif
            }

            ICharacter ch = this.characterUnmarshaller[id];
            Layer layer = this.currentTimeline.GetLayer(depth);

            return new PlaceObject(ch, layer, null, matrix, null, false, cxform, null, null);
        }

        private PlaceObject ReadPlaceObject2()
        {
            bool hasClipActions = this.sdtr.ReadBit();
            bool hasClipDepth = this.sdtr.ReadBit();
            bool hasName = this.sdtr.ReadBit();
            bool hasRatio = this.sdtr.ReadBit();
            bool hasColorTransform = this.sdtr.ReadBit();
            bool hasMatrix = this.sdtr.ReadBit();
            bool hasCharacter = this.sdtr.ReadBit();
            bool isMove = this.sdtr.ReadBit();

            int depth = this.sdtr.ReadUI16();

            int? id = null;
            if (hasCharacter)
            {
                id = this.sdtr.ReadUI16();
            }

            Matrix matrix = null;
            if (hasMatrix)
            {
                matrix = this.sdtr.ReadMatrix();
            }

#if DEBUG
            this.Log("id=" + id + ", depth=" + depth + " matrix=" + matrix);
#endif

            ColorTransform cxform = null;
            if (hasColorTransform)
            {
                cxform = this.sdtr.ReadColorTransformWithAlpha();
#if DEBUG
                this.Log("cxform=" + cxform);
#endif
            }

            int? ratio = null;
            if (hasRatio)
            {
                ratio = this.sdtr.ReadUI16();
#if DEBUG
                this.Log("ratio=" + ratio);
#endif
            }

            string name = null;
            if (hasName)
            {
                name = this.sdtr.ReadString();
#if DEBUG
                this.Log("name=" + name);
#endif
            }

            int? clipDepth = null;
            if (hasClipDepth)
            {
                clipDepth = this.sdtr.ReadUI16();
#if DEBUG
                this.Log("clipDepth=" + clipDepth);
#endif
            }

            if (hasClipActions)
            {
                throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        @"Clip actions are not supported by the target flash player version.", swf.Context);
            }

            /* ISSUE 40: A null id means something in the spec. Look up how to find out what ID this should be.
             * After that, fix the writer so that it knows when it can omit IDs of its own. This is one of
             * those areas where we're kinda exposing part of the SWF spec into the model, which feels dirty
             * and wrong. */
            ICharacter ch = id == null ? null : this.characterUnmarshaller[(int)id];

            return new PlaceObject(ch, this.currentTimeline.GetLayer(depth), clipDepth, matrix, name, isMove, cxform, null, ratio);
        }


        private void ReadDefineMorphShape(Tag format, uint followingOffset)
        {
            int id = this.sdtr.ReadUI16();

            byte[] unparsedShape = this.sdtr.ReadByteBlock((int)(followingOffset - this.sdtr.Offset));

            IShape shape = new ShapeParser().Parse(format, unparsedShape, this);

            this.characterUnmarshaller.Add(id, shape);
            this.swf.AddCharacter(CID_PREFIX + id, shape);
        }

        private void ReadDefineShapeN(Tag tagType, uint followingOffset)
        {
            int id = this.sdtr.ReadUI16();

#if DEBUG
            this.Log("char id=" + id);
#endif
            byte[] unparsedShape = this.sdtr.ReadByteBlock((int)(followingOffset - this.sdtr.Offset));

            IShape shape = new ShapeParser().Parse(tagType, unparsedShape, this);

            this.characterUnmarshaller.Add(id, shape);
            this.swf.AddCharacter(CID_PREFIX + id, shape);
        }

        private void ReadFileAttributesTag(out bool as3)
        {
            int type;
            uint followingOffset;

            this.sdtr.ReadRECORDHEADER(out type, out followingOffset);

            bool err = false;

            err = err || this.sdtr.ReadBit(); /* Reserved */

            /*(void)*/this.sdtr.ReadUBits(3); /* direct blit / use GPU ; both ignored, Also */
                                              /* has metadata is ignored, we just read the metadata if we find it. */

            as3 = this.sdtr.ReadBit();

            err = err || (as3 && this.version < 9); /* AS3 only permitted in v9+ */

            err = err || (this.sdtr.ReadUBits(2) != 0); /* Reserved */

            /*(void)*/
            this.sdtr.ReadUBits(1); /* Use network ; ignored */

            err = err || (this.sdtr.ReadUBits(24) != 0); /* Reserved */

            if (err)
            {
                throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        @"Invalid file attributes", swf.Context);
            }

            this.FinishTag(followingOffset);
        }

        private void ReadHeader(SWFContext ctx)
        {
            int sig = this.sdtr.ReadUI24();
            if (sig != SIG_COMPRESSED && sig != SIG_UNCOMPRESSED)
            {
                throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        @"Not a SWF file", ctx);
            }

            bool compressed = sig == SIG_COMPRESSED;

            this.version = this.sdtr.ReadUI8();
            if (this.version < 9)
            {
                throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        @"Only SWF 9+ is supported (Found " + this.version +")", ctx);
            }
#if DEBUG
            this.Log("SWF version = " + this.version);
#endif
            this.fileLen = this.sdtr.ReadUI32();

            if (compressed)
            {
                this.sdtr.SwitchToDeflateMode(this.fileLen);
            }

            this.frameSize = this.sdtr.ReadRect();

            this.fps = this.sdtr.ReadFIXED8();

            this.frameCount = this.sdtr.ReadUI16();
        }

        private void ReadDoABC(uint followingOffset)
        {
            this.doAbcCount++;
            bool lazyInit = (this.sdtr.ReadUI32() & ABCValues.AbcFlagLazyInitialize) != 0;
            string name = this.sdtr.ReadString();
            byte[] bytecode = this.sdtr.ReadByteBlock((int)(followingOffset - this.sdtr.Offset));
#if DEBUG
            this.Log("lazyInit=" + lazyInit + ", name=" + name + ", bytelen=" + bytecode.Length);
            if (this.abcInterceptor != null)
            {
                this.abcInterceptor.OnLoadAbc(lazyInit, this.swf.Context, name, doAbcCount, bytecode);
            }
#endif
            this.swf.AddScript(new DoABC(lazyInit, name, bytecode, null));
        }

        private void ReadXMLMetadata()
        {
            string rdf = this.sdtr.ReadString();
#if DEBUG
            this.Log("xml metadata:");
            this.Log(rdf);
#endif
        }

        private void ReadEnableDebugger2()
        {
            int zero = this.sdtr.ReadUI16();
            if (zero != 0)
            {
                throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        "Unexpected value in debug tag", swf.Context);
            }

            string md5 = this.sdtr.ReadString();

            /* We ignore this and put in a new password when we save it out. */
#if DEBUG
            this.Log("debug enabled with md5:"+md5);
#endif
        }

        private void ReadSymbolClass()
        {
            int numSymbols = this.sdtr.ReadUI16();
            for (int i = 0; i < numSymbols; i++)
            {
                int cid = this.sdtr.ReadUI16();
                string className = this.sdtr.ReadString();
#if DEBUG
                this.Log("bind class '" + className + "' to character " + cid);
#endif
                if (className.StartsWith("."))
                {
                    /*
                     * ISSUE 41: I don't know why some files do this and some don't. An example of
                     * a file that does this is bottoms.swf in the flat avatar unit test. I
                     * really wish I could find where this came from so that I can see what's
                     * different in the .fla
                     */
                    className = className.Substring(1);
                }

                if (cid == 0)
                {
                    /* ID 0 is the main timeline */
                    this.LateClassResolutions.Add(className, this.swf);
                    continue;
                }

                if (!this.characterUnmarshaller.ContainsKey(cid))
                {
                    throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        "Can't bind class " + className + " to missing character ID " + cid, swf.Context);
                }

                ICharacter c = this.characterUnmarshaller[cid];

                if (!(c is Timeline))
                {
                    throw new SWFModellerException(
                        SWFModellerError.SWFParsing,
                        "Can't bind class " + className + " to non-timeline character " + c, swf.Context);
                }

                this.LateClassResolutions.Add(className, (Timeline)c);
            }
        }

        private void ReadSceneAndFrameLabelData()
        {
            uint nscenes = this.sdtr.ReadEncodedU32();
            for (int i = 0; i < nscenes; i++)
            {
                uint sceneOffset = this.sdtr.ReadEncodedU32() + 1;
                string sceneName = this.sdtr.ReadString();
#if DEBUG
                this.Log("scene " + i + ", frame=" + sceneOffset + ", name=" + sceneName);
#endif
                this.sceneNotes.Add(new FrameNote { frame = sceneOffset, note = sceneName });
            }

            uint numFrameLabels = this.sdtr.ReadEncodedU32();
            for (int i = 0; i < numFrameLabels; i++)
            {
                uint frameNum = this.sdtr.ReadEncodedU32() + 1;
                string frameLabel = this.sdtr.ReadString();
#if DEBUG
                this.Log("frame label " + i + ", frame=" + frameNum + ", label=" + frameLabel);
#endif
                this.frameLabelNotes.Add(new FrameNote { frame = frameNum, note = frameLabel });
            }

            /* ISSUE 42: The spec says the offsets are 'global' which appears to imply that they relate
             * to the frame's offset in the file, rather than the frame number on the main timeline.
             * This means we need to keep a 'frame offset' lookup in the reader, rather than
             * treat these as frame numbers, which we do at the moment. Damn. */
        }

        private void ReadSprite()
        {
            int characterID = this.sdtr.ReadUI16();
            uint frameCount = this.sdtr.ReadUI16();

#if DEBUG
            this.Log("char id=" + characterID + ", frames=" + frameCount);
#endif

            Sprite sprite = this.swf.NewSprite(CID_PREFIX + characterID, frameCount, (this.frameCursor == 1));

            this.currentTimeline = sprite;
            this.characterUnmarshaller.Add(characterID, sprite);

            int currentFrame = 1;
            for (; ; )
            {
                int type;
                uint followingOffset;
                this.sdtr.ReadRECORDHEADER(out type, out followingOffset);

#if DEBUG
                Tag _tag = (Tag)type;
                bool isDefine = _tag == Tag.DefineShape ||
                     _tag == Tag.DefineShape3 ||
                     _tag == Tag.DefineShape4 ||
                     _tag == Tag.DefineSprite;
                this.MarkDumpOffset(
                    "Body of " + _tag + " (" + type + ") len=" + (followingOffset - this.sdtr.Offset),
                    isDefine);
                this.binaryDumpNest++;
#endif

                switch ((Tag)type)
                {
                    case Tag.ShowFrame:
#if DEBUG
                        this.MarkDumpOffset("");
#endif
                        currentFrame++;
                        break;

                    case Tag.End:
                        if ((currentFrame - 1) != frameCount)
                        {
                            throw new SWFModellerException(
                                    SWFModellerError.SWFParsing,
                                    @"Frame count mismatch in sprite " + characterID, swf.Context);
                        }
                        this.currentTimeline = this.swf;
#if DEBUG
                        this.binaryDumpNest--;
#endif
                        return;

                    case Tag.PlaceObject2:
                        sprite.GetFrame(currentFrame).AddTag(this.ReadPlaceObject2());
                        break;

                    case Tag.PlaceObject:
                        sprite.GetFrame(currentFrame).AddTag(this.ReadPlaceObject(followingOffset));
                        break;

                    case Tag.FrameLabel:
                        sprite.GetFrame(currentFrame).Label = this.sdtr.ReadString();
#if DEBUG
                        this.Log("Frame label = " + sprite.GetFrame(currentFrame).Label);
#endif
                        break;

                    case Tag.RemoveObject2:
                        sprite.GetFrame(currentFrame).AddTag(this.ReadRemoveObject2());
                        break;

                    case Tag.RemoveObject:
                    case Tag.StartSound:
                    case Tag.SoundStreamHead:
                    case Tag.SoundStreamHead2:
                    case Tag.SoundStreamBlock:
                    case Tag.DoAction:
                        /* ISSUE 73 */
                        throw new SWFModellerException(
                                SWFModellerError.UnimplementedFeature,
                                @"Unsupported tag within a sprite definition: " + ((Tag)type).ToString(), swf.Context);

                    default:
                        throw new SWFModellerException(
                                SWFModellerError.SWFParsing,
                                @"Bad SWF; A " + ((Tag)type).ToString() + @" tag is not permitted within a sprite definition", swf.Context);
                }
#if DEBUG
                this.binaryDumpNest--;
#endif
            }
        }

        private void ReadScriptLimits()
        {
            int maxRecurseDepth = this.sdtr.ReadUI16();
            int scriptTimeout = this.sdtr.ReadUI16();
#if DEBUG
            this.Log("maxRecurse=" + maxRecurseDepth + ", timeout=" + scriptTimeout + "s");
#endif
            /* ISSUE 43: Read this and store it in the sprite. */
        }

        /// <summary>
        /// Writes an entry into the log file, marking the byte offset where the file pointer
        /// is currently pointing.
        /// </summary>
        /// <param name="comment">The text to display in the log next to the offset.</param>
        /// <param name="makeGap">The line will be preceeded by a gap to mark a section in the file.</param>
        [Conditional("DEBUG")]
        private void MarkDumpOffset(string comment, bool makeGap = false)
        {
#if DEBUG
            if (this.binaryDump != null)
            {
                if (makeGap)
                {
                    this.binaryDump.AppendLine();
                }
                this.binaryDump.AppendLine((new string('\t', Math.Max(0, this.binaryDumpNest))) + comment);
            }
#endif
        }

        [Conditional("DEBUG")]
        private void Log(string comment)
        {
#if DEBUG
            if (this.binaryDump != null)
            {
                this.binaryDump.AppendLine((new string('\t', Math.Max(0, this.binaryDumpNest))) + "; " + comment);
            }
#endif
        }

        /// <summary>
        /// A note which can be made against a frame number, e.g. a scene or a frame
        /// label. These are resolved later into frame object references.
        /// </summary>
        private struct FrameNote
        {
            public uint frame;
            public string note;
        }

        public IImage FindImage(int cid)
        {
            if (characterUnmarshaller.ContainsKey(cid))
            {
                ICharacter ch = characterUnmarshaller[cid];

                if (ch is IImage)
                {
                    return (IImage)ch;
                }

                throw new SWFModellerException(SWFModellerError.SWFParsing, "Image reference does not point to image", swf.Context);
            }

            throw new SWFModellerException(SWFModellerError.SWFParsing, "Missing image cid " + cid, swf.Context);
        }
    }
}
