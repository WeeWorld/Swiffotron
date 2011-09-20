//-----------------------------------------------------------------------
// Timeline.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Modelling
{
    using System.Collections.Generic;
    using System.Linq;
    using SWFProcessing.SWFModeller.ABC;
    using SWFProcessing.SWFModeller.ABC.Code;
    using SWFProcessing.SWFModeller.Characters;
    using SWFProcessing.SWFModeller.Characters.Geom;
    using SWFProcessing.SWFModeller.Text;
    using SWFProcessing.SWFModeller.DisplayList;
    using SWFProcessing.SWFModeller.Characters.Text;

    /// <summary>
    /// Timeline objects are simply objects that have frames in them.
    /// </summary>
    public abstract class Timeline
    {
        protected List<Frame> frames = new List<Frame>();

        protected List<Layer> layers = new List<Layer>();

        public AS3Class Class { get; set; }

        public bool HasClass
        {
            get
            {
                return this.Class != null;
            }
        }

        public uint FrameCount
        {
            get
            {
                if (this.frames == null)
                {
                    return 0;
                }

                return (uint)this.frames.Count;
            }

            set
            {
                this.frames = new List<Frame>((int)value);
                for (int i = 0; i < value; i++)
                {
                    this.frames.Add(new Frame());
                }
            }
        }

        public int LayerCount
        {
            get
            {
                return this.layers.Count;
            }
        }

        public abstract SWF Root { get; }

        public delegate void FontProcessor(SWFFont font);

        public delegate void CharacterProcessor(ICharacter character);

        /// <summary>
        /// Gets an iterable list of frames.
        /// </summary>
        public IEnumerable<Frame> Frames
        {
            get
            {
                return this.frames.AsEnumerable();
            }
        }

        /// <summary>
        /// Iterable list of layers.
        /// </summary>
        public IEnumerable<Layer> Layers
        {
            get
            {
                return this.layers.AsEnumerable();
            }
        }

        /// <summary>
        /// Gets a frame. If the frame doesn't exist, the timeline will be extended to
        /// make it exist.
        /// </summary>
        /// <param name="idx">1-based frame index.</param>
        /// <returns>A frame at the desired index.</returns>
        public Frame GetFrame(int idx)
        {
            while (this.frames.Count < idx)
            {
                this.frames.Add(new Frame());
            }

            return this.frames[idx - 1];
        }

        public Layer GetFreeLayer(Layer.Position position)
        {
            switch (position)
            {
                case Layer.Position.Front:
                    Layer l = new Layer(this);
                    this.layers.Add(l);
                    return l;

                case Layer.Position.Back:
                default:
                    throw new SWFModellerException(
                            SWFModellerError.Internal,
                            "SWF::GetFreeLayer; " + position.ToString());
            }
        }

        public int GetLayerIndex(Layer layer)
        {
            int idx = -1;

            if (this.layers != null)
            {
                idx = this.layers.FindIndex(l => l == layer);
            }

            if (idx == -1)
            {
                throw new SWFModellerException(
                        SWFModellerError.Timeline,
                        "Layer does not belong on this timeline");
            }

            return idx + 1;
        }

        public Layer GetLayer(int depth)
        {
            depth--;

            if (depth < 0)
            {
                throw new SWFModellerException(
                        SWFModellerError.Timeline,
                        "Negative depth is as yet unsupported.");
            }

            if (depth < this.layers.Count)
            {
                return this.layers[depth];
            }

            do
            {
                this.layers.Add(new Layer(this));
            }
            while (this.layers.Count <= depth);

            return this.layers[depth];
        }

        public void FontProc(FontProcessor fd)
        {
            /* If you like delegate methods, you'll love this. */

            foreach (Frame f in frames)
            {
                foreach (IDisplayListItem dl in f.DisplayList)
                {
                    if (dl is ICharacterReference)
                    {
                        ICharacter ch = ((ICharacterReference)dl).Character;
                        if (ch is IFontUserProcessor)
                        {
                            IFontUserProcessor fup = (IFontUserProcessor)ch;
                            fup.FontUserProc(delegate(IFontUser fu)
                            {
                                if (fu.HasFont)
                                {
                                    fd(fu.Font);
                                }
                            });
                        }

                        if (ch is Timeline)
                        {
                            /* ISSUE 69: Is there any risk of infinite recursion here? Possible pass in a set
                             * of visited objects, timelines and fonts. */
                            ((Timeline)ch).FontProc(fd);
                        }
                    }
                }
            }
        }

        public void CharacterProc(CharacterProcessor cp)
        {
            foreach (Frame f in frames)
            {
                foreach (IDisplayListItem dl in f.DisplayList)
                {
                    if (dl is ICharacterReference)
                    {
                        ICharacter ch = ((ICharacterReference)dl).Character;
                        cp(ch);

                        if (ch is Timeline)
                        {
                            ((Timeline)ch).CharacterProc(cp);
                        }
                    }
                }
            }
        }

        public void Instantiate(int frameNum, Sprite sprite, Layer.Position layering, Matrix position, string instanceName, string qClassName)
        {
            AS3Class instanceClass = sprite.Class;

            if (this.Class == null)
            {
                throw new SWFModellerException(
                        SWFModellerError.Internal,
                        "Can't instantiate " + instanceName + " on timeline with no code");
            }

            DoABC scriptTag = this.Root.FirstScript;
            if (scriptTag == null)
            {
                /* ISSUE 70: Y'know, we can generate scripts. We should probably do that. */
                throw new SWFModellerException(
                        SWFModellerError.Internal,
                        "Can't instantiate clips in a SWF with no code.");
            }

            if (instanceClass == null)
            {
                DoABC.GenerateDefaultScript(qClassName, sprite);
                instanceClass = sprite.Class;
            }

            /* Create instance variable of type referenced by instanceClass */

            Namespace propNS = scriptTag.Code.CreateNamespace(Namespace.NamespaceKind.Package, string.Empty);
            Multiname propName = scriptTag.Code.CreateMultiname(Multiname.MultinameKind.QName, instanceName, propNS, NamespaceSet.EmptySet);

            if (this.Class is AS3ClassDef)
            {
                ((AS3ClassDef)this.Class).AddInstanceTrait(new SlotTrait()
                {
                    Kind = TraitKind.Slot,
                    TypeName = instanceClass.Name,
                    Name = propName,
                    ValKind = ConstantKind.ConUndefined
                });
            }

            this.GetFrame(frameNum).AddTag(new PlaceObject(sprite, this.GetFreeLayer(layering), null, position, instanceName, false, null, null, null));
        }

        public bool RemoveInstance(string name)
        {
            List<KeyValuePair<Frame, IDisplayListItem>> hitList = new List<KeyValuePair<Frame, IDisplayListItem>>();
            List<PlaceObject> openTimelines = new List<PlaceObject>();

            /* ISSUE 71: The unit test for this has no RemoveObject dli items in it, so we don't really
             * know if the crazy timeline searching found here actually works. Make a harsher test. */

            foreach (Frame f in frames)
            {
                foreach (IDisplayListItem dli in f.DisplayList)
                {
                    switch (dli.Type)
                    {
                        case DisplayListItemType.PlaceObjectX:
                            PlaceObject po = (PlaceObject)dli;
                            if (po.Name == name)
                            {
                                hitList.Add(new KeyValuePair<Frame, IDisplayListItem>(f, po));
                                openTimelines.Add(po);
                            }
                            break;

                        case DisplayListItemType.RemoveObjectX:
                            RemoveObject ro = (RemoveObject)dli;
                            foreach (PlaceObject openPo in openTimelines)
                            {
                                if ((!ro.HasCharacter || openPo.Character == ro.Character) && ro.Layer == openPo.Layer)
                                {
                                    hitList.Add(new KeyValuePair<Frame, IDisplayListItem>(f, ro));
                                    openTimelines.Remove(openPo);
                                    break;
                                }
                            }
                            break;

                        default:
                            /* TODO */
                            throw new SWFModellerException(SWFModellerError.UnimplementedFeature, "Crazy, hitherto not seen display list item: " + dli.Type.ToString());
                    }
                }
            }

            foreach (KeyValuePair<Frame, IDisplayListItem> hit in hitList)
            {
                hit.Key.RemoveDisplayListItem(hit.Value);
            }

            return hitList.Count > 0;
        }
    }
}
