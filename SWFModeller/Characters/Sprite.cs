//-----------------------------------------------------------------------
// Sprite.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Characters
{
    using System.Collections.Generic;
    using System.Text;
    using SWFProcessing.ModellingUtils.Util;
    using SWFProcessing.SWFModeller.ABC;
    using SWFProcessing.SWFModeller.ABC.Code;
    using SWFProcessing.SWFModeller.Characters.Text;
    using SWFProcessing.SWFModeller.DisplayList;
    using SWFProcessing.SWFModeller.Modelling;
    using SWFProcessing.SWFModeller.Text;

    /// <summary>
    /// A sprite object with its own timeline
    /// </summary>
    public class Sprite : Timeline, ICharacter
    {
        private SWF _root;

        /// <summary>
        /// Initializes a new instance of a sprite and empty timeline.
        /// </summary>
        /// <param name="frameCount">The initial number of empty frames on
        /// the timeline.</param>
        public Sprite(uint frameCount, SWF root)
        {
            this._root = root;
            if (frameCount < 1)
            {
                throw new SWFModellerException(
                        SWFModellerError.Internal,
                        @"A sprite must have at least 1 frame");
            }
            FrameCount = frameCount;
        }

        /// <summary>
        /// Initializes a new instance of a sprite by cloning another timeline.
        /// </summary>
        /// <param name="srcTimeline">The timeline to clone.</param>
        /// <param name="className">If the cloned timeline is a SWF, then
        /// you should pass in a class name here. The MainTimeline class
        /// will be renamed in here to this new name.</param>
        public Sprite(Timeline srcTimeline, SWF root, string className = null)
        {
            this._root = root;

            /* Layers are just objects that exist purely to be arranged in some
             * kind of order and be pointed at by more meaningful, other things.
             * To clone layers, we need to simply copy the list and map old
             * layer refs to our new ones. */
            Dictionary<Layer, Layer> newLayers = new Dictionary<Layer, Layer>(srcTimeline.LayerCount);
            foreach (Layer l in srcTimeline.Layers)
            {
                Layer newLayer = new Layer(this);
                LayerList.Add(newLayer);
                newLayers.Add(l, newLayer);
            }

            FrameList = new List<Frame>((int)srcTimeline.FrameCount);
            foreach (Frame f in srcTimeline.Frames)
            {
                Frame newFrame = new Frame();
                foreach (IDisplayListItem dli in f.DisplayList)
                {
                    newFrame.AddTag(dli.Clone(newLayers[dli.Layer], false));
                }
                FrameList.Add(newFrame);
            }

            if (srcTimeline is SWF)
            {
                SWF srcSWF = (SWF)srcTimeline;

                if (className != null)
                {
                    if (srcSWF.Class != null)
                    {
                        srcSWF.RenameMainTimelineClass(className);
                    }
                    /* Else the class will be generated later */
                }

                RemapFonts(srcSWF, root);

                if (className != null)
                {
                    foreach (DoABC abc in srcSWF.Scripts)
                    {
                        root.MergeScript(abc);
                    }
                }

                if (className == null)
                {
                    /* It's tempting to use ClassByName("flash.display.MovieClip") but
                     * remember that that class exists in the player, not the SWF. What
                     * we need in this case is just the name of the class, not a reference
                     * to the class itself. Because that's complicated, we assign a
                     * dummy class and watch for it when we write the class out. */
                    this.Class = AdobeClass.CreateFlashDisplayMovieClip(root.FirstScript.Code);
                }
                else
                {
                    this.Class = srcSWF.Class;
                }
            }
        }

        /// <summary>
        /// This will alter the incoming SWF's font references. Any new fonts will be
        /// left untouched. Any fonts that exist in root already will have their glyphs
        /// imported into the root SWF's fonts and references will re-point to the
        /// now possibly larger font. Any fonts that exist only in incoming will be left
        /// alone since importing the SWF as a movieclip will automatically make those
        /// fonts part of root.
        /// </summary>
        /// <param name="incoming">The SWF being absorbed into the root.</param>
        /// <param name="root">The root SWF that will absord new fonts or glyphs.</param>
        private void RemapFonts(SWF incoming, SWF root)
        {
            List<SWFFont> rootFonts = new List<SWFFont>();
            root.FontProc(delegate(SWFFont font)
            {
                rootFonts.Add(font);
            });

            Dictionary<SWFFont, SWFFont> remaps = new Dictionary<SWFFont, SWFFont>();

            incoming.FontProc(delegate(SWFFont font)
            {
                foreach (SWFFont rootFont in rootFonts)
                {
                    if (font.CanMergeWith(rootFont))
                    {
                        remaps.Add(font, rootFont);

                        char[] codes = font.CodePoints;
                        foreach (char c in codes)
                        {
                            bool hasPixelAlignment = font.HasPixelAlignment;
                            bool hasLayout = font.HasLayout;

                            if (!rootFont.HasGlyph(c))
                            {
                                rootFont.AddGlyph(c, font.GetGlyphShape(c));

                                if (hasPixelAlignment)
                                {
                                    rootFont.AddPixelAlignment(c, font.GetPixelAligment(c));
                                }

                                if (hasLayout)
                                {
                                    rootFont.AddLayout(c, font.GetLayout(c));
                                }
                            }
                        }
                    }
                }
            });

            /* Now we've merged glyphs into our target fonts, we need to go over all
             * uses of the old fonts and remap them... */

            incoming.CharacterProc(delegate(ICharacter ch)
            {
                if (ch is IFontUserProcessor)
                {
                    IFontUserProcessor fup = (IFontUserProcessor)ch;

                    fup.FontUserProc(delegate(IFontUser fu)
                    {
                        if (fu.HasFont && remaps.ContainsKey(fu.Font))
                        {
                            fu.Font = remaps[fu.Font];
                        }
                    });
                }
            });
        }

        public delegate void SpriteProcessor(Sprite s);

        public override SWF Root { get { return this._root; } }

#if(DEBUG)
        public string ID { get { return this._root == null ? null : this._root.IDFor(this); } }
#endif

        public IEnumerable<ICharacterReference> CharacterRefs
        {
            get
            {
                ListSet<ICharacterReference> refs = new ListSet<ICharacterReference>();
                IEnumerable<ICharacterReference> enumerable = this.PopulateCharacterRefList(refs).AsEnumerable();
                return enumerable;
            }
        }

        public PlaceObject FindInstance(string name)
        {
            foreach (Frame f in FrameList)
            {
                PlaceObject po = f.FindInstance(name);
                if (po != null)
                {
                    return po;
                }
            }
            return null;
        }

#if DEBUG
        /// <summary>
        /// Unit test method present in the debug build. Renders the object as text
        /// that can be written to a diffable output file.
        /// </summary>
        /// <param name="nest">The current nest level in our output</param>
        /// <param name="sb">The buffer that is accumulating output.</param>
        public void ToStringModelView(int nest, StringBuilder sb, out bool oneLiner)
        {
            oneLiner = false;

            string indent = new string(' ', nest * 4);

            if (this.Class != null)
            {
                sb.Append(indent + "Class name:" + this.Class + "\n");
            }

            int frameIdx = 1;
            if (FrameList != null)
            {
                foreach (Frame f in FrameList)
                {
                    StringBuilder frameSB = new StringBuilder();
                    f.ToStringModelView(nest + 1, frameSB);

                    if (frameSB.Length > 0)
                    {
                        sb.Append(indent + "Frame# " + frameIdx + "\n");
                        sb.Append(indent + "{\n");
                        sb.Append(frameSB.ToString());
                        sb.Append(indent + "}\n");
                    }
                    else
                    {
                        sb.Append(indent + "Frame# " + frameIdx + " ;\n");
                    }

                    frameIdx++;
                }
            }
        }
#endif


#if(DEBUG)
        /// <summary>
        /// Renders the sprite as a string. Used only in test/debug console output.
        /// </summary>
        /// <returns>A string rendition of this sprite.</returns>
        public override string ToString()
        {
            string id = this._root.IDFor(this);

            if (id == null)
            {
                if (this.Class != null)
                {
                    return "[sprite " + this.Class + "]";
                }
                else
                {
                    return "[sprite]";
                }
            }
            else
            {
                if (this.Class != null)
                {
                    return "[sprite '" + id + "', " + this.Class + "]";
                }
                else
                {
                    return "[sprite '" + id + "']";
                }

            }
        }
#endif

        /// <summary>
        /// Calls a delegate method on each descendant sprite in this sprite.
        /// </summary>
        /// <param name="sp"></param>
        public void SpriteProc(SpriteProcessor sp)
        {
            foreach (Frame f in FrameList)
            {
                foreach (IDisplayListItem dli in f.DisplayList)
                {
                    ICharacterReference cr = dli as ICharacterReference;
                    if (cr != null)
                    {
                        Sprite child = cr.Character as Sprite;
                        if (child != null)
                        {
                            sp(child);
                            child.SpriteProc(sp);
                        }
                    }
                }
            }
        }

        private ListSet<ICharacterReference> PopulateCharacterRefList(ListSet<ICharacterReference> refs)
        {
            foreach (Frame f in FrameList)
            {
                foreach (IDisplayListItem dli in f.DisplayList)
                {
                    if (dli is ICharacterReference)
                    {
                        ICharacterReference cr = (ICharacterReference)dli;

                        refs.Add(cr);

                        if (cr.Character is Sprite && !refs.Contains(cr))
                        {
                            ((Sprite)cr.Character).PopulateCharacterRefList(refs);
                        }
                    }
                }
            }
            return refs;
        }
    }
}
