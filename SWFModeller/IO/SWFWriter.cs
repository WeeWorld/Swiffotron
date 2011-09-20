//-----------------------------------------------------------------------
// SWFWriter.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
    using SWFProcessing.SWFModeller.ABC;
    using SWFProcessing.SWFModeller.ABC.IO;
    using SWFProcessing.SWFModeller.Characters;
    using SWFProcessing.SWFModeller.Characters.Geom;
    using SWFProcessing.SWFModeller.Characters.Images;
    using SWFProcessing.SWFModeller.Characters.Shapes;
    using SWFProcessing.SWFModeller.Characters.Shapes.IO;
    using SWFProcessing.SWFModeller.Characters.Text;
    using SWFProcessing.SWFModeller.DisplayList;
    using SWFProcessing.SWFModeller.IO;
    using SWFProcessing.SWFModeller.Modelling;
    using SWFProcessing.SWFModeller.Text;
    using SWFProcessing.SWFModeller.Util;
    using SWFProcessing.SWFModeller.ABC.Code;

    /// <summary>
    /// An object which can take a SWF model and produce binary SWF data.
    /// </summary>
    public class SWFWriter
    {
        private const uint SIG_UNCOMPRESSED = 0x535746; /* S, W, F */
        private const uint SIG_COMPRESSED = 0x535743; /* S, W, C */

        private const int SWF_VERSION = 10;

        /// <summary>
        /// The SWF to be written.
        /// </summary>
        private SWF swf;

        private SWFWriterOptions options;

        /// <summary>This is the root writer that sits at the bottom of the stack and holds
        /// the whole file.</summary>
        private WriteBuffer swfOut;

        private Stack<WriteBuffer> writers;
        private IDMarshaller<ICharacter> characterMarshal;
        private StringBuilder writeLog;
        private StringBuilder abcWriteLog;

        private JPEGTable writtenJPEGTable;

        /// <summary>
        /// Initializes a new instance of a SWF writer with the given options.
        /// </summary>
        /// <param name="swf">The SWF to write.</param>
        /// <param name="options">The options that control the output, or
        /// null for the defaults.</param>
        public SWFWriter(SWF swf, SWFWriterOptions options, StringBuilder writeLog, StringBuilder abcWriteLog)
        {
            if (options == null)
            {
                /* Create a default object */
                this.options = new SWFWriterOptions()
                {
                    Compressed = true,
                    EnableDebugger = false
                };
            }
            else
            {
                this.options = options;
            }

            this.swf = swf;
            this.writeLog = writeLog;
            this.abcWriteLog = abcWriteLog;
            this.characterMarshal = new IDMarshaller<ICharacter>(1);
        }

        /// <summary>
        /// Initializes a new instance of a SWF writer with the default options.
        /// </summary>
        /// <param name="swf">The SWF to write.</param>
        public SWFWriter(SWF swf, StringBuilder writeLog, StringBuilder abcWriteLog)
            : this(swf, null, writeLog, abcWriteLog)
        {
            /* Convenience constructor. */
        }

        /// <summary>
        /// Takes a SWF object and turns it into a .swf binary file. It might seem nicer to
        /// write it to a stream, but the stream has a file length at the start which we only
        /// know once we've written it all out to a byte array anyway. Returning it as a
        /// chunk of data is simply more honest; an output stream would promise streaminess behind
        /// the scenes that we cannot deliver.
        /// </summary>
        /// <returns>The .swf data is returned as a byte array.</returns>
        public byte[] ToByteArray()
        {
            /* We start writing things mid-way through the header, at the point
             * where compression comes into effect. Once we've created and perhaps
             * compressed the data, we can write the first part of the header and
             * concatenate the rest. */

            writtenJPEGTable = null;

            /* The file contains tags, which can contain tags, each of which is prefixed with a length.
             * To track all this, we use a stack of writers, each with its own buffer. The first on the
             * stack is the buffer that will contain the file itself. */
            this.writers = new Stack<WriteBuffer>();
            this.swfOut = new WriteBuffer(Tag.None, "swf");
            this.writers.Push(this.swfOut);

            this.swfOut.WriteRect(new Rect(0, this.swf.FrameWidth, 0, this.swf.FrameHeight));

            this.swfOut.WriteFIXED8(this.swf.Fps);
            this.swfOut.WriteUI16(this.swf.FrameCount);

            this.WriteTags();

            /* Ok, we basically have a SWF now. All we need to do is compress it and
             * stick a header on at the front... */

            byte[] body = this.swfOut.Data;
            uint fileLen = (uint)(body.Length + 8); /* Add the 8 bytes of header we haven't done yet. */

            if (this.options.Compressed)
            {
                MemoryStream zbytes = new MemoryStream();
                DeflaterOutputStream zos = new DeflaterOutputStream(zbytes);
                zos.Write(body, 0, body.Length);
                zos.Close();
                body = zbytes.ToArray();
            }

            MemoryStream final = new MemoryStream();
            SWFDataTypeWriter finalWriter = new SWFDataTypeWriter(final);

            finalWriter.WriteUI24(this.options.Compressed ? SIG_COMPRESSED : SIG_UNCOMPRESSED);

            /* ISSUE 27: Hard-coded SWF version 10. Technically this should be an option but
             * for now we don't want the headache of version-specific code. */
            finalWriter.WriteUI8(SWF_VERSION);

            finalWriter.WriteUI32(fileLen);

            finalWriter.Write(body, 0, body.Length);

            return final.ToArray();
        }

        [Conditional("DEBUG")]
        private void LogTag(Tag t, string logdata)
        {
            this.LogMessage("Write " + t.ToString() + "(" + logdata + ")");
        }

        [Conditional("DEBUG")]
        private void LogMessage(string s)
        {
            if (this.writeLog != null)
            {
                /* Add 8 to the offset because when it finally makes it to disk, there
                 * will be an 8-byte header at the front. */
                this.writeLog.AppendLine((new string('\t', this.writers.Count - 1)) + s + " @" + (this.swfOut.Offset + 8));
            }
        }

        /// <summary>
        /// Does the grunt-work of writing all the objects in the SWF file, tagging
        /// each of them with a record header.
        /// </summary>
        private void WriteTags()
        {
            /* Start with a file attributes tag */
            this.WriteFileAttributesTag();
            /* Despite background color being specified in the header, flash always puts this in too. */
            this.WriteBGColorTag();

            if (swf.ProtectHash != null)
            {
                /* ISSUE 45: This should be an option of some kind. */
                WriteBuffer protectTag = this.OpenTag(Tag.Protect);
                protectTag.WriteUI16(0); /* Reserved, always 0 */
                protectTag.WriteString(swf.ProtectHash);
                this.CloseTag();
            }

            if (this.options.EnableDebugger)
            {
                WriteBuffer dbugTag = this.OpenTag(Tag.EnableDebugger2);
                dbugTag.WriteUI16(0); /* Reserved, always 0 */
                dbugTag.WriteString("$1$ZH$B14iwyCzzcXcqLaJz0Mif0"); /* MD5-encoded password "abc"; http://devadraco.blogspot.com/2009/06/guide-to-cracking-enabledebugger2.html */
                this.CloseTag();
            }

            /* ISSUE 46: Write DefineSceneAndFrameLabelData tag */
            foreach (DoABC abc in this.swf.Scripts)
            {
                WriteBuffer abcOut = this.OpenTag(Tag.DoABC);

                abcOut.WriteUI32((uint)(abc.IsLazilyInitialized ? ABCValues.AbcFlagLazyInitialize : 0));
                abcOut.WriteString(abc.Name);

                AbcWriter abcWriter = new AbcWriter();
                abcWriter.AssembleIfNecessary(
                        abc,
                        this.options.EnableDebugger,
                        this.swf.Class == null ? null : this.swf.Class.QualifiedName,
                        this.abcWriteLog);

                abcOut.WriteBytes(abc.Bytecode);

                this.CloseTag();
            }

            ListSet<Timeline> writtenSymbolClasses = new ListSet<Timeline>();
            ListSet<Timeline> unboundClasses = new ListSet<Timeline>();

            foreach (Sprite exported in this.swf.ExportOnFirstFrame)
            {
                this.WriteSprite(exported, unboundClasses);
            }

            this.BindClasses(unboundClasses);

            if (this.swf.FrameCount > 0)
            {
                int writtenFrames = 0;

                if (this.swf.HasClass)
                {
                    WriteBuffer scbuf = this.OpenTag(Tag.SymbolClass);
                    scbuf.WriteUI16(1); /* Count */
                    scbuf.WriteUI16(0); /* Character ref */
                    scbuf.WriteString(this.swf.Class.QualifiedName); /* Name */
                    this.CloseTag();
                }

                foreach (Frame f in this.swf.Frames)
                {
                    if (f.HasLabel)
                    {
#if DEBUG
                        this.LogMessage("frame label=" + f.Label);
#endif
                        WriteBuffer labelWriter = this.OpenTag(Tag.FrameLabel);
                        labelWriter.WriteString(f.Label);
                        this.CloseTag();
                    }

                    foreach (IDisplayListItem dli in f.DisplayList)
                    {
                        switch (dli.Type)
                        {
                            case DisplayListItemType.PlaceObjectX:
                                this.WriteCharacter(((ICharacterReference)dli).Character, unboundClasses);
                                this.WritePlaceObjectTag((PlaceObject)dli);
                                break;

                            case DisplayListItemType.RemoveObjectX:
                            default:
                                this.WriteRemoveObjectTag((RemoveObject)dli);
                                break;
                        }
                    }

                    this.BindClasses(unboundClasses);

                    this.OpenTag(Tag.ShowFrame);
                    this.CloseTag();

                    writtenFrames++;

                    List<SymbolClass> symbolClasses = new List<SymbolClass>();
                }
            }
            else
            {
                /* No SWF should be frameless. Awwww. */
                this.OpenTag(Tag.ShowFrame);
                this.CloseTag();
            }

            /* Finish with an end tag */
            this.OpenTag(Tag.End);
            this.CloseTag();
        }

        private void BindClasses(ListSet<Timeline> unboundClasses)
        {
            if (unboundClasses.Count > 0)
            {
                WriteBuffer symbolBuf = this.OpenTag(Tag.SymbolClass);

                symbolBuf.WriteUI16((uint)unboundClasses.Count);

                foreach (Timeline t in unboundClasses)
                {
                    symbolBuf.WriteUI16((uint)this.characterMarshal.GetIDFor((ICharacter)t));
                    symbolBuf.WriteString(t.Class.QualifiedName);
                }
                this.CloseTag();

                unboundClasses.Clear();
            }
        }

        private Tag TagForPlaceObject(PlaceObject po)
        {
            if (po.HasClassName)
            {
                /* ISSUE 47:
                 * Also indicated by presence of
                 * - Surface filter list
                 * - Blend mode
                 * - Bitmap cache
                 */
                return Tag.PlaceObject3;
            }

            if (po.HasName || po.IsMove || po.HasClipDepth || po.HasRatio || po.HasClipActions || po.HasColorTransformWithAlpha)
            {
                return Tag.PlaceObject2;
            }

            return Tag.PlaceObject;
        }

        private void WriteRemoveObjectTag(RemoveObject ro)
        {
            WriteBuffer removeBuf = this.OpenTag(ro.HasCharacter ? Tag.RemoveObject : Tag.RemoveObject2);

            if (ro.HasCharacter)
            {
                int cid = this.characterMarshal.GetIDFor(ro.Character);
                removeBuf.WriteUI16((uint)cid);
            }

            removeBuf.WriteUI16((uint)ro.LayerIndex);
            this.CloseTag();
        }

        private void WritePlaceObjectTag(PlaceObject po)
        {
            Tag placeTag = this.TagForPlaceObject(po);
            WriteBuffer tagWriter = this.OpenTag(placeTag);
            int cid;
            switch (placeTag)
            {
                case Tag.PlaceObject:
                    if (!po.HasCharacter)
                    {
                        throw new SWFModellerException(
                                SWFModellerError.Internal,
                                "A PlaceObject display list item must have a character unless it is a move instruction.");
                    }
#if DEBUG
                    if (!this.characterMarshal.HasMarshalled(po.Character))
                    {
                        throw new SWFModellerException(
                                SWFModellerError.Internal,
                                "Can't place object that hasn't been written to stream yet.");
                    }
#endif
                    cid = this.characterMarshal.GetIDFor(po.Character);
                    tagWriter.WriteUI16((uint)cid);
#if DEBUG
                    this.LogMessage("po cid =" + cid);
#endif
                    tagWriter.WriteUI16((uint)po.LayerIndex);
                    if (!po.HasMatrix)
                    {
                        throw new SWFModellerException(
                                SWFModellerError.Internal,
                                "A PlaceObject display list item must have a Matrix, unless it's a PlaceObject2 tag. See spec for info, I can't work it out.");
                    }
                    tagWriter.WriteMatrix(po.Matrix);
                    if (po.HasColorTransform)
                    {
                        tagWriter.WriteColorTransform(po.CXForm, false);
                    }
                    break;


                case Tag.PlaceObject2:
                    tagWriter.WriteBit(po.HasClipActions);
                    tagWriter.WriteBit(po.HasClipDepth);
                    tagWriter.WriteBit(po.HasName);
                    tagWriter.WriteBit(po.HasRatio);
                    tagWriter.WriteBit(po.HasColorTransform);
                    tagWriter.WriteBit(po.HasMatrix);
                    tagWriter.WriteBit(po.HasCharacter);
                    tagWriter.WriteBit(po.IsMove);
                    tagWriter.WriteUI16((uint)po.LayerIndex);

                    if (po.HasCharacter)
                    {
#if DEBUG
                        if (!this.characterMarshal.HasMarshalled(po.Character))
                        {
                            throw new SWFModellerException(
                                    SWFModellerError.Internal,
                                    "Can't place object that hasn't been written to stream yet.");
                        }
#endif
                        cid = this.characterMarshal.GetIDFor(po.Character);
                        tagWriter.WriteUI16((uint)cid);
#if DEBUG
                        this.LogMessage("po cid =" + cid);
#endif
                    }

                    if (po.HasMatrix)
                    {
                        tagWriter.WriteMatrix(po.Matrix);
                    }

                    if (po.HasColorTransform)
                    {
                        tagWriter.WriteColorTransform(po.CXForm, true);
                    }

                    if (po.HasRatio)
                    {
                        tagWriter.WriteUI16((uint)po.Ratio);
                    }

                    if (po.HasName)
                    {
#if DEBUG
                        this.LogMessage("name=" + po.Name);
#endif
                        tagWriter.WriteString(po.Name);
                    }

                    if (po.HasClipDepth)
                    {
                        tagWriter.WriteUI16((uint)po.ClipDepth);
                    }

                    if (po.HasClipActions)
                    {
                        throw new SWFModellerException(
                                SWFModellerError.Internal,
                                "Clips cannot have actions in the target SWF version.");
                    }
                    break;

                default:
                    /* TODO */
                    throw new SWFModellerException(
                            SWFModellerError.UnimplementedFeature,
                            "Unsupported PlaceObject tag: " + placeTag.ToString());
            }

#if DEBUG
            this.LogMessage("po char =" + po.Character);
#endif

            this.CloseTag();
        }

        private void WriteSprite(Sprite s, ListSet<Timeline> unboundClasses)
        {
            foreach (ICharacterReference cr in s.CharacterRefs)
            {
                this.WriteCharacter(cr.Character, unboundClasses);
            }

            if (s.HasClass && !(s.Class is AdobeClass) && !unboundClasses.Contains(s))
            {
                unboundClasses.Add(s);
            }

            int id = this.characterMarshal.GetIDFor(s);
            WriteBuffer tagWriter = this.OpenTag(Tag.DefineSprite, s.ToString() + ";id=" + id.ToString());
            tagWriter.WriteUI16((uint)id);
            tagWriter.WriteUI16(s.FrameCount);
#if DEBUG
            this.LogMessage("char id=" + id);
#endif

            foreach (Frame f in s.Frames)
            {
                if (f.HasLabel)
                {
#if DEBUG
                    this.LogMessage("frame label=" + f.Label);
#endif
                    WriteBuffer labelWriter = this.OpenTag(Tag.FrameLabel);
                    labelWriter.WriteString(f.Label);
                    this.CloseTag();
                }

                foreach (IDisplayListItem dli in f.DisplayList)
                {
                    switch (dli.Type)
                    {
                        case DisplayListItemType.PlaceObjectX:
                            this.WritePlaceObjectTag((PlaceObject)dli);
                            break;

                        case DisplayListItemType.RemoveObjectX:
                            this.WriteRemoveObjectTag((RemoveObject)dli);
                            break;

                        default:
                            /* TODO */
                            throw new SWFModellerException(
                                    SWFModellerError.UnimplementedFeature,
                                    "Unsupported tag in SWF sprite writer: " + dli.GetType().ToString());
                    }
                }
                this.OpenTag(Tag.ShowFrame);
                this.CloseTag();
            }


            this.OpenTag(Tag.End, id.ToString()); /* ISSUE 48: Optimization: For bodyless tags, we can probably have a special case that doesn't go through the hoops of adding new writers to stacks etc */
            this.CloseTag();

            this.CloseTag(); /* DefineSprite */
        }

        private void WriteImage(IImage image)
        {
            if (characterMarshal.HasMarshalled(image))
            {
                /* Been there, done that. */
                return;
            }

            ImageBlob blob = image as ImageBlob;

            if (blob == null)
            {
                throw new SWFModellerException(SWFModellerError.Internal, "Can't write " + image.ToString());
            }

            switch (blob.DataFormat)
            {
                case Tag.DefineBitsJPEG2:
                case Tag.DefineBitsLossless:
                case Tag.DefineBitsLossless2:
                    WriteBuffer blobBuffer = OpenTag(blob.DataFormat);
                    blobBuffer.WriteUI16((uint)characterMarshal.GetIDFor(image));
                    blobBuffer.WriteBytes(blob.FormattedBytes);
                    CloseTag();
                    break;

                case Tag.DefineBits:
                    if (blob.JPEGTable != null)
                    {
                        if (writtenJPEGTable != null && writtenJPEGTable != blob.JPEGTable)
                        {
                            /* ISSUE 16 */
                            throw new SWFModellerException(SWFModellerError.UnimplementedFeature,
                                    "Can't process multiple JPEG encoding tables yet.");
                        }

                        WriteBuffer tables = OpenTag(Tag.JPEGTables);
                        tables.WriteBytes(blob.JPEGTable.TableData);
                        CloseTag();

                        writtenJPEGTable = blob.JPEGTable;
                    }

                    WriteBuffer bits = OpenTag(Tag.DefineBits);
                    bits.WriteUI16((uint)characterMarshal.GetIDFor(image));
                    bits.WriteBytes(blob.FormattedBytes);
                    CloseTag();
                    break;

                default:
                    throw new SWFModellerException(SWFModellerError.Internal, "Can't write image format " + blob.DataFormat.ToString());;
            }
        }

        private void WriteCharacter(ICharacter ch, ListSet<Timeline> unboundClasses)
        {
            int cid;

            if (ch == null)
            {
                return;
            }

            if (this.characterMarshal.HasMarshalled(ch))
            {
                return;
            }

            int fontID = -1;

            if (ch is IFontUserProcessor)
            {
                IFontUserProcessor fup = (IFontUserProcessor)ch;
                fup.FontUserProc(delegate(IFontUser fu)
                {
                    if (fu.HasFont && !characterMarshal.HasMarshalled(fu.Font))
                    {
                        fontID = characterMarshal.GetIDFor(fu.Font);
                        this.WriteFont(fu.Font, fontID);
                    }
                    else
                    {
                        fontID = characterMarshal.GetExistingIDFor(fu.Font);
                    }
                });
            }

            if (ch is IShape)
            {
                IImage[] images = ((IShape)ch).GetImages();

                if (images != null)
                {
                    foreach (IImage image in images)
                    {
                        this.WriteImage(image);
                    }
                }

                Tag format;
                byte[] shapeBytes = ShapeWriter.ShapeToBytes((IShape)ch, out format);

                WriteBuffer shapeTag = this.OpenTag(format);
                cid = this.characterMarshal.GetIDFor(ch);
                shapeTag.WriteUI16((uint)cid);
                shapeTag.WriteBytes(shapeBytes);
#if DEBUG
                this.LogMessage("char id=" + cid);
#endif
                this.CloseTag();
            }
            else if (ch is Sprite)
            {
                this.WriteSprite((Sprite)ch, unboundClasses);
            }
            else if (ch is EditText)
            {
                this.WriteEditText((EditText)ch, fontID);
            }
            else if (ch is StaticText)
            {
                this.WriteStaticText((StaticText)ch);
            }
            else
            {
                /* TODO */
                throw new SWFModellerException(
                            SWFModellerError.UnimplementedFeature,
                            "Character of type " + ch.GetType().ToString() + " not currently supported in writer");
            }

            if (ch is Timeline)
            {
                Timeline tl = (Timeline)ch;
                if (tl.HasClass && !(tl.Class is AdobeClass) && !unboundClasses.Contains(tl))
                {
                    unboundClasses.Add(tl);
                }
            }
        }

        private void WriteStaticText(StaticText text)
        {
            int cid = characterMarshal.GetIDFor(text);

            bool hasAlpha = text.HasAlpha;

            WriteBuffer textTag = this.OpenTag(hasAlpha ? Tag.DefineText2 : Tag.DefineText, "; id=" + cid);

            /* Tag.DefineText(2) */
            {
                textTag.WriteUI16((uint)cid);

                textTag.WriteRect(text.Bounds);
                textTag.Align8();

                textTag.WriteMatrix(text.Position);
                textTag.Align8();

                /* ISSUE 49: We're lazy here. We max out the bits for text and advances coz we can't
                 * yet calculate them. Fix this attrocity. */

                int glyphBits = 16;
                int advanceBits = 16;

                textTag.WriteUI8((uint)glyphBits);
                textTag.WriteUI8((uint)advanceBits);

                foreach (TextRecord tr in text.Records)
                {
                    Dictionary<char, int> glyphIDX = null;

                    uint flags = 0x80;

                    if (tr.HasFont)
                    {
                        flags |= 0x08;
                    }

                    if (tr.HasColour)
                    {
                        flags |= 0x04;
                    }

                    if (tr.HasYOffset)
                    {
                        flags |= 0x02;
                    }

                    if (tr.HasXOffset)
                    {
                        flags |= 0x01;
                    }

                    textTag.WriteUI8(flags);

                    if (tr.HasFont)
                    {
                        textTag.WriteUI16((uint)this.characterMarshal.GetExistingIDFor(tr.Font));
                    }

                    if (tr.HasColour)
                    {
                        if (hasAlpha)
                        {
                            textTag.WriteRGBA(tr.Colour.ToArgb());
                        }
                        else
                        {
                            textTag.WriteRGB(tr.Colour.ToArgb());
                        }
                    }

                    if (tr.HasXOffset)
                    {
                        textTag.WriteSI16(tr.XOffset);
                    }

                    if (tr.HasYOffset)
                    {
                        textTag.WriteSI16(tr.YOffset);
                    }

                    if (tr.HasFont)
                    {
                        textTag.WriteUI16((uint)tr.FontHeight);

                        glyphIDX = tr.Font.IndexMap;
                    }

                    char[] chars = tr.Text.ToCharArray();
                    if (chars.Length > 255)
                    {
                        throw new SWFModellerException(SWFModellerError.Internal, "String too long. This should be split across text records.");
                    }

                    textTag.WriteUI8((uint)chars.Length);
                    for (int i = 0; i < tr.Advances.Length; i++)
                    {
                        textTag.WriteUBits((uint)glyphIDX[chars[i]], glyphBits);
                        textTag.WriteSBits(tr.Advances[i], advanceBits);
                    }

                    textTag.Align8();
                }

                textTag.WriteUI8(0); /* End record */

            } /* End of tag code. */

            this.CloseTag();
        }

        /// <param name="fontID">Pass -1 if this has no font.</param>
        private void WriteEditText(EditText text, int fontID)
        {
            int cid = characterMarshal.GetIDFor(text);

            WriteBuffer textTag = this.OpenTag(Tag.DefineEditText, "; id=" + cid);

            /* Tag.DefineEditText */
            {
                textTag.WriteUI16((uint)cid);

                textTag.WriteRect(text.Bounds);
                textTag.Align8();

                textTag.WriteBit(text.HasText);
                textTag.WriteBit(text.WordWrapEnabled);
                textTag.WriteBit(text.IsMultiline);
                textTag.WriteBit(text.IsPassword);
                textTag.WriteBit(text.IsReadOnly);
                textTag.WriteBit(text.HasTextColor);
                textTag.WriteBit(text.HasMaxLength);
                textTag.WriteBit(text.HasFont);
                textTag.WriteBit(text.HasFontClass);
                textTag.WriteBit(text.IsAutoSized);
                textTag.WriteBit(text.HasLayout);
                textTag.WriteBit(text.IsNonSelectable);
                textTag.WriteBit(text.HasBorder);
                textTag.WriteBit(text.IsStatic);
                textTag.WriteBit(text.IsHTML);
                textTag.WriteBit(text.UseOutlines);

                if (text.HasFont)
                {
                    textTag.WriteUI16((uint)fontID);
                }

                if (text.HasFontClass)
                {
                    /* ISSUE 14 */
                    throw new SWFModellerException(SWFModellerError.UnimplementedFeature, "Font classes can't be written.");
                }

                if (text.HasFont)
                {
                    textTag.WriteUI16((uint)text.FontHeight);
                }

                if (text.HasTextColor)
                {
                    textTag.WriteRGBA(text.Color.ToArgb());
                }

                if (text.HasMaxLength)
                {
                    textTag.WriteUI16((uint)text.MaxLength.Value);
                }

                if (text.HasLayout)
                {
                    EditText.Layout layout = text.LayoutInfo;

                    textTag.WriteUI8((uint)layout.Align);
                    textTag.WriteUI16((uint)layout.LeftMargin);
                    textTag.WriteUI16((uint)layout.RightMargin);
                    textTag.WriteUI16((uint)layout.Indent);
                    textTag.WriteSI16(layout.Leading);
                }

                textTag.WriteString(text.VarName);

                if (text.HasText)
                {
                    textTag.WriteString(text.Text);
                }
            }

            CloseTag();
        }

        private void WriteFont(SWFFont font, int fid)
        {
            WriteBuffer fontTag = this.OpenTag(Tag.DefineFont3, font.Name + "; id=" + fid);

            char[] codes = font.CodePoints;

            /* Tag.DefineFont3 */
            {
                fontTag.WriteUI16((uint)fid);

                fontTag.WriteBit(font.HasLayout);

                fontTag.WriteBit(false); /* ISSUE 50: ShiftJIS support */
                fontTag.WriteBit(font.IsSmall);
                fontTag.WriteBit(false); /* ISSUE 51: ANSI support, though I think this might never be false. */

                fontTag.WriteBit(true); /* ISSUE 52: We always write wide offsets. This is because we're too lazy to measure our table. */
                fontTag.WriteBit(true); /* Spec says must be true. */

                fontTag.WriteBit(font.IsItalic);
                fontTag.WriteBit(font.IsBold);

                fontTag.WriteUI8((uint)font.LanguageCode);

                fontTag.WriteString(font.Name, true);

                fontTag.WriteUI16((uint)font.GlyphCount);

                byte[][] shapeData = new byte[font.GlyphCount][];
                int totalShapeBytes = 0;
                for (int i = 0; i < font.GlyphCount; i++)
                {
                    Tag format;
                    shapeData[i] = ShapeWriter.ShapeToBytes(font.GetGlyphShape(codes[i]), out format);

                    if (format != Tag.DefineFont3)
                    {
                        throw new SWFModellerException(SWFModellerError.Internal, "Can't write non-font shapes as glyphs");
                    }

                    totalShapeBytes += shapeData[i].Length;
                }

                int startOffset = font.GlyphCount * 4 + 4; /* 4 bytes per offset (wide offsets) + 4 for the code table offset */
                int nextOffset = startOffset;
                foreach (byte[] shapeBytes in shapeData)
                {
                    fontTag.WriteUI32((uint)nextOffset);
                    nextOffset += shapeBytes.Length;
                }

                fontTag.WriteUI32((uint)(startOffset + totalShapeBytes));

                foreach (byte[] shapeBytes in shapeData)
                {
                    fontTag.WriteBytes(shapeBytes);
                }

                foreach (char code in codes)
                {
                    fontTag.WriteUI16((uint)code);
                }

                if (font.HasLayout)
                {
                    fontTag.WriteSI16(font.Ascent.Value);
                    fontTag.WriteSI16(font.Descent.Value);
                    fontTag.WriteSI16(font.Leading.Value);

                    Rect[] bounds = new Rect[font.GlyphCount];
                    int boundsPos = 0;
                    foreach (char c in codes)
                    {
                        GlyphLayout gl = font.GetLayout(c);
                        fontTag.WriteSI16(gl.Advance);
                        bounds[boundsPos++] = gl.Bounds;
                    }

                    foreach (Rect bound in bounds)
                    {
                        fontTag.WriteRect(bound);
                        fontTag.Align8();
                    }

                    fontTag.WriteUI16((uint)font.KerningTable.Length);
                    foreach (KerningPair kern in font.KerningTable)
                    {
                        fontTag.WriteUI16(kern.LeftChar);
                        fontTag.WriteUI16(kern.RightChar);
                        fontTag.WriteSI16(kern.Adjustment);
                    }
                }
            }

            this.CloseTag();

            if (font.HasPixelAlignment)
            {
                WriteBuffer zonesTag = this.OpenTag(Tag.DefineFontAlignZones, font.Name + "; id=" + fid);

                zonesTag.WriteUI16((uint)fid);

                if (font.ThicknessHint == null)
                {
                    throw new SWFModellerException(SWFModellerError.Internal, "Can't have pixel aligmnent without a font thickness hint.");
                }

                zonesTag.WriteUBits((uint)font.ThicknessHint, 2);
                zonesTag.WriteUBits(0, 6); /* Reserved */

                foreach (char c in codes)
                {
                    PixelAlignment pa = font.GetPixelAligment(c);

                    if (pa.ZoneInfo.Length != 2)
                    {
                        throw new SWFModellerException(SWFModellerError.Internal, "Pixel aligment should always have 2 zones.");
                    }

                    zonesTag.WriteUI8((uint)pa.ZoneInfo.Length);

                    foreach (PixelAlignment.ZoneData zi in pa.ZoneInfo)
                    {
                        /* These int values are just unparsed 16-bit floats. */
                        zonesTag.WriteUI16((uint)zi.AlignmentCoord);
                        zonesTag.WriteUI16((uint)zi.Range);
                    }

                    zonesTag.WriteUBits(0, 6); /* Reserved */
                    zonesTag.WriteBit(pa.HasY);
                    zonesTag.WriteBit(pa.HasX);
                }

                this.CloseTag();
            }

            if (font.HasExtraNameInfo)
            {
                WriteBuffer nameTag = this.OpenTag(Tag.DefineFontName, font.FullName + "; id=" + fid);

                nameTag.WriteUI16((uint)fid);
                nameTag.WriteString(font.FullName);
                nameTag.WriteString(font.Copyright);

                this.CloseTag();
            }
        }

        private void WriteBGColorTag()
        {
            this.OpenTag(Tag.SetBackgroundColor).WriteRGB(this.swf.BackgroundColor.ToArgb());
            this.CloseTag();
        }

        private void WriteFileAttributesTag()
        {
            WriteBuffer tagWriter = this.OpenTag(Tag.FileAttributes);

            /* ISSUE 27: Some of these flags are for SWF v10 - check the flash 9 spec for defaults instead. */

            /* No data written yet, so data will be aligned. */
            tagWriter.WriteBit(false);  /* Reserved, must be 0 */
            tagWriter.WriteBit(true);  /* UseDirectBlit, TODO: We set this to '1' but I dunno what the IDE does. */
            tagWriter.WriteBit(true);  /* UseGPU, TODO: We set this to '1' but I dunno what the IDE does. */
            tagWriter.WriteBit(false);  /* HasMetadata. TODO: Set this to 0. We won't need it unless we know how to access it from the SWF. I don't think you can. */
            tagWriter.WriteBit(true);  /* AS3, because we don't like AS2. Boo. */
            tagWriter.WriteUBits(0, 2);  /* Reserved, must be 0 */
            tagWriter.WriteBit(false);  /* UseNetwork. TODO: Check what the IDE sets for this. */
            tagWriter.WriteUBits(0, 24); /* Reserved, must be 0 */
            this.CloseTag();
        }

        private WriteBuffer OpenTag(Tag tag, string log = null)
        {
            WriteBuffer buf = new WriteBuffer(tag, log);
            this.writers.Push(buf);

#if DEBUG
            this.LogTag(tag, log);
#endif

            return buf;
        }

        private WriteBuffer CloseTag()
        {
            if (this.writers.Count <= 1)
            {
                throw new SWFModellerException(
                        SWFModellerError.Internal,
                        "Call to CloseTag when no tag was open.");
            }

            WriteBuffer tagWriter = this.writers.Peek();

            tagWriter.Align8();
            byte[] tagData = tagWriter.Data;
            int tagCode = (int)tagWriter.Tag;

#if DEBUG
            this.LogMessage("Body length of " + tagWriter.Tag.ToString()+" " + tagData.Length + " bytes on " + tagWriter.LogData);
#endif
            this.writers.Pop();

            tagWriter = this.writers.Peek();
            tagWriter.Align8();

            if (tagData.Length >= 63)
            {
                /* Long record header */
                int hdr = (tagCode << 6) | 0x3f;
                tagWriter.WriteUI16((uint)hdr);
                tagWriter.WriteSI32(tagData.Length);
            }
            else
            {
                /* Short record header */
                int hdr = (tagCode << 6) | tagData.Length;
                tagWriter.WriteUI16((uint)hdr);
            }

            tagWriter.Write(tagData, 0, tagData.Length);

            return tagWriter;
        }

        struct SymbolClass
        {
            public string ClassName;
            public int CharacterID;

            public SymbolClass(string className, int characterID)
            {
                this.CharacterID = characterID;
                this.ClassName = className;
            }
        }

        /// <summary>
        /// Since tags include their length at the start, we use this class to
        /// buffer a tag's data on it's way to an outer stream.
        /// </summary>
        private class WriteBuffer : SWFDataTypeWriter
        {
            private MemoryStream bytes;

            public WriteBuffer(Tag tag, string logdata)
                : this(new MemoryStream())
            {
                this.Tag = tag;
                this.LogData = logdata;
            }

            private WriteBuffer(MemoryStream bytes)
                : base(bytes)
            {
                this.bytes = bytes;
            }

            public Tag Tag { get; set; }
            public byte[] Data { get { return this.bytes.ToArray(); } }

            /// <summary>
            /// This is used in the write log to help debugging.
            /// </summary>
            public string LogData { get; set; }
        }
    }
}
