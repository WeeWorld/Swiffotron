//-----------------------------------------------------------------------
// SWF.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using SWFProcessing.SWFModeller.ABC;
    using SWFProcessing.SWFModeller.ABC.Code;
    using SWFProcessing.SWFModeller.Characters;
#if(DEBUG)
    using SWFProcessing.SWFModeller.Characters.Debug;
#endif
    using SWFProcessing.SWFModeller.Characters.Text;
    using SWFProcessing.SWFModeller.Modelling;
    using SWFProcessing.SWFModeller.Text;
    using SWFProcessing.SWFModeller.Process;

    /// <summary>
    /// Top-level object for a SWF movie.
    /// </summary>
    public class SWF : Timeline
    {
        private const float DefaultFrameWidth = 550;
        private const float DefaultFrameHeight = 400;
        private const float DefaultFPS = 24;

        /// <summary>
        /// The default stage colour.
        /// </summary>
        private static Color defaultBGColour = Color.White;

        /* ISSUE 24: The dictionary is only of use when creating SWF files, much like the library
         * is only of use in a fla file. Since this is a SWF class, not a FLA class, we could
         * probably move the dictionary out to the Swiffotron, since it is our equivalient
         * to the IDE. */
        private Dictionary<string, ICharacter> dictionary;

        /// <summary>
        /// The list of sprites exported on frame 1.
        /// </summary>
        private List<Sprite> exportOnFirstFrame;

        /// <summary>
        /// ISSUE 25: We assume that a SWF can have multiple bytecode tags in it. I suspect however
        /// that this is not the case. What we will probably need to do is take two *parsed*
        /// chunks of ABC code and merge them, namespaces, scripts and all into some sort of
        /// uberscript. We'll have an array here in the meantime though because I'm still hoping
        /// I can stick my head in the sand about all that.
        /// </summary>
        private List<DoABC> scripts;

        /// <summary>
        /// All the fonts loaded from the base SWF.
        /// </summary>
        private List<SWFFont> fonts;

        /// <summary>
        /// For code merging, it's useful to know what clips are bound to what classes. This takes
        /// a class name, and maps it to a clip.
        /// </summary>
        private Dictionary<AS3ClassDef, Timeline> clipClassMap;

        public bool HasMainTimelineClass
        {
            get
            {
                return true;
            }
        }


        /// <summary>
        /// Initializes a new instance of a SWF with the same defaults as the
        /// Flash IDE.
        /// </summary>
        /// <param name="name">An optional name intended solely to give error messages
        /// some context.</param>
        public SWF(SWFContext ctx, bool generateScripts)
        {
            this.Context = ctx;

            this.FrameWidth = DefaultFrameWidth;
            this.FrameHeight = DefaultFrameHeight;
            this.Fps = DefaultFPS;

            this.dictionary = new Dictionary<string, ICharacter>();

            this.scripts = new List<DoABC>();

            this.fonts = new List<SWFFont>();

            this.BackgroundColor = defaultBGColour;

            /* ISSUE 30: This list is read when the swf is written, but we never
             * add to it. We should add to it on instruction from swiffotron xml
             * attributes, and also when we infer that a read sprite should be
             * exported based on its position in the source SWF relative to its first
             * use. */
            this.exportOnFirstFrame = new List<Sprite>();

            this.clipClassMap = new Dictionary<AS3ClassDef, Timeline>();

            if (generateScripts)
            {
                this.GenerateTimelineScripts();
            }
        }

        /// <summary>
        /// A delegate declaration for a script processor. See ScriptProc
        /// </summary>
        /// <param name="clazz">Each script will be passed into the delegate.</param>
        public delegate void ScriptProcessor(DoABC abc);

        /// <summary>
        /// A delegate declaration for a class processor. See ClassProc
        /// </summary>
        /// <param name="clazz">Each class will be passed into the delegate.</param>
        public delegate void ClassProcessor(AS3ClassDef clazz);

        /// <summary>
        /// Gets the root timeline.
        /// </summary>
        public override SWF Root
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// Gets or sets a name for this SWF.
        /// This name is purely so that we can identify what SWF we're looking at
        /// during development in the debugger, and in error messages. It is never written
        /// to the SWF binary.
        /// </summary>
        public SWFContext Context { get; set; }

        /// <summary>
        /// Gets or sets the width of the stage
        /// </summary>
        public float FrameWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of the stage
        /// </summary>
        public float FrameHeight { get; set; }

        /// <summary>
        /// Gets or sets the frame rate of the movie.
        /// </summary>
        public float Fps { get; set; }

        /// <summary>
        /// Gets or sets the  the MD5 crypto hash for the SWF's protection. If this is
        /// null, then it's unprotected.
        /// </summary>
        public string ProtectHash { get; set; }

        /// <summary>
        /// Gets the number of items in the dictionary.
        /// </summary>
        public int DictionaryCount
        {
            get
            {
                return this.dictionary.Count;
            }
        }

        /// <summary>
        /// Gets or sets the background color of the SWF stage.
        /// </summary>
        public Color BackgroundColor { get; set; }

        /// <summary>
        /// Gets an iterable list of scripts.
        /// </summary>
        public IEnumerable<DoABC> Scripts
        {
            get
            {
                return this.scripts.AsEnumerable();
            }
        }

        /// <summary>
        /// Enumeration of all the sprites that are exported on the first frame.
        /// </summary>
        public IEnumerable<Sprite> ExportOnFirstFrame
        {
            get
            {
                return from o in this.exportOnFirstFrame where o.HasClass select o;
            }
        }

        /// <summary>
        /// Gets the first script in the SWF, or null if there are no scripts.
        /// </summary>
        public DoABC FirstScript
        {
            get
            {
                /* ISSUE 26: Classes have scripts. We also call high level ABC objects 'scripts'. This is
                 * unnecessary and confusing. We need to sort out our terminology. "ScriptTag" would be good.*/

                /* ISSUE 25: This seems like a hack. Really we should probably only ever have one script tag anyway.
                 * Just doesn't feel right until I've seen enough SWF files to accept that they only ever have
                 * one. I guess a fix would be to say we don't support multiple DoABC tags. */

                if (this.scripts.Count == 0)
                {
                    return null;
                }

                return this.scripts[0];
            }
        }

        /// <summary>
        /// Adds a character, e.g. a shape or an image to the SWF dictionary.
        /// </summary>
        /// <param name="id">A name to refer to this character in the dictionary.</param>
        /// <param name="c">The character to add.</param>
        public void AddCharacter(string id, ICharacter c)
        {
            this.dictionary[id] = c;
        }

        /// <summary>
        /// Create a new sprite and add it to the SWF.
        /// </summary>
        /// <param name="id">A unique ID as supplied by valid SWF data</param>
        /// <param name="frameCount">The number of frames on the sprite's
        /// timeline.</param>
        /// <param name="exportForScript">If a sprite is exported for script, it will
        /// appear at the start of the SWF file.</param>
        /// <returns>The newly created sprite.</returns>
        public Sprite NewSprite(string id, uint frameCount, bool exportForScript)
        {
            if (this.dictionary.ContainsKey(id))
            {
                throw new SWFModellerException(SWFModellerError.Internal, @"Duplicate character ID (Sprite)");
            }

            Sprite newSprite = new Sprite(frameCount, this);
            this.dictionary[id] = newSprite;

            if (exportForScript)
            {
                this.exportOnFirstFrame.Add(newSprite);
            }

            return newSprite;
        }

        /// <summary>
        /// Creates a new StaticText field in the SWF.
        /// </summary>
        /// <param name="id">The id (library name).</param>
        /// <returns>The new static text object</returns>
        public StaticText NewStaticText(string id)
        {
            if (this.dictionary.ContainsKey(id))
            {
                throw new SWFModellerException(SWFModellerError.Internal, @"Duplicate character ID (StaticText)");
            }

            StaticText newText = new StaticText(this);
            this.dictionary[id] = newText;

            return newText;
        }

        /// <summary>
        /// Creates a new EditText field in the SWF.
        /// </summary>
        /// <param name="id">The id (library name).</param>
        /// <returns>The new edit text object</returns>
        public EditText NewEditText(string id)
        {
            if (this.dictionary.ContainsKey(id))
            {
                throw new SWFModellerException(SWFModellerError.Internal, @"Duplicate character ID (EditText)");
            }

            EditText newText = new EditText(this);
            this.dictionary[id] = newText;

            return newText;
        }

        /// <summary>
        /// Adds a script.
        /// </summary>
        /// <param name="abc">The abc code.</param>
        public void AddScript(DoABC abc)
        {
            this.scripts.Add(abc);
        }

        /// <summary>
        /// This takes an existing script and merges it into our script by merging
        /// constants and classes together in a terrifying clash of opcodes. Where we have
        /// multiple scripts, we will merge into the first one, because we need to pick one
        /// and have no way to decide what would be best.
        /// </summary>
        /// <param name="abc">The script to merge into our one.</param>
        public void MergeScript(DoABC abc)
        {
            if (this.scripts.Count == 0)
            {
                this.AddScript(abc);
                /* Well, that was easy */
                return;
            }

            this.scripts[0].Merge(abc, Context);
        }

#if(DEBUG)
        /// <summary>
        /// This is brutal and slow, but it's only in debug builds for unit test
        /// output, so we just don't care.
        /// </summary>
        public string IDFor(ICharacter c)
        {
            foreach (string id in this.dictionary.Keys)
            {
                if (c == this.dictionary[id])
                {
                    return id;
                }
            }

            return null;
        }
#endif

        /// <summary>
        /// Renders the SWF and everything in it to a string buffer
        /// that can be compared with expected output in unit tests. This
        /// method is only available in debug builds.
        /// </summary>
        /// <param name="nest">The nest level for nicely formatted text.</param>
        /// <param name="sb">A string builder reference into which all text
        /// will be written.</param>
        [Conditional("DEBUG")]
        public void ToStringModelView(int nest, StringBuilder sb)
        {
#if DEBUG
            string indent = new string(' ', nest * 4);

            sb.Append(indent + "SWF stage=" + this.FrameWidth + "," + this.FrameHeight + ", fps=" + this.Fps + ", bg=" + ColorTranslator.ToHtml(this.BackgroundColor) + "\n");

            if (Class != null)
            {
                sb.Append(indent + "Document class:" + this.Class + "\n");
            }

            sb.Append(indent + "{\n");

            nest++;
            indent = new string(' ', nest * 4);

            List<string> indices = new List<string>();

            foreach (string id in this.dictionary.Keys)
            {
                indices.Add(id);
            }

            indices.Sort();

            foreach (string id in indices)
            {
                bool oneLiner;
                StringBuilder characterContents = new StringBuilder();
                CharacterDump.ToStringModelView(this.dictionary[id], nest + 1, characterContents, out oneLiner);

                if (oneLiner)
                {
                    sb.Append(indent + "Character '" + id + "' = "+characterContents.ToString().Trim()+"\n");
                }
                else
                {
                    sb.Append(indent + "Character '" + id + "'\n");
                    sb.Append(indent + "{\n");
                    sb.Append(characterContents.ToString());
                    sb.Append(indent + "}\n");
                }
            }

            int frameIdx = 1;

            if (this.frames != null)
            {
                foreach (Frame f in this.frames)
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

            foreach (DoABC abc in this.scripts)
            {
                abc.ToStringModelView(nest, sb);
            }

            if (fonts.Count > 0)
            {
                sb.Append(indent + "Fonts\n");
                sb.Append(indent + "{\n");
                foreach (SWFFont font in fonts)
                {
                    font.ToStringModelView(nest + 1, sb);
                }
                sb.Append(indent + "}\n");
            }

            nest--;
            indent = new string(' ', nest * 4);

            sb.Append(indent + "}\n");
#endif
        }

        /// <summary>
        /// Finds a placed character on the stage by its instance name.
        /// </summary>
        /// <param name="qname">The qualified name, which is a dotted path through
        /// nested instances to the instance you want, starting from the root
        /// timeline.</param>
        /// <returns>The placeobject displaylist item, or null if not found.</returns>
        public PlaceObject LookupInstance(string qname)
        {
            /* ISSUE 31: In Swiffotron, we should be making a quick
             * check on qname to make sure it's not a store path since
             * if it is, and we've specified a wrong instance type, it
             * just crashes instead of supplying an informative error
             * message.
             */

            string[] path = qname.Split('.');

            if (path.Length == 0)
            {
                return null;
            }

            PlaceObject po = null;
            ICharacter c = null;

            foreach (Frame f in this.frames)
            {
                po = f.FindInstance(path[0]);

                if (po != null)
                {
                    c = po.Character;

                    if (c == null)
                    {
                        throw new SWFModellerException(
                                SWFModellerError.CharacterNotFound,
                                "Instance has missing character: " + path[0]);
                    }

                    break;
                }
            }

            for (int i = 1; i < path.Length; i++)
            {
                if (!(c is Sprite))
                {
                    continue;
                }

                po = ((Sprite)c).FindInstance(path[i]);

                if (po == null)
                {
                    return null;
                }

                c = po.Character;

                if (c == null)
                {
                    throw new SWFModellerException(
                            SWFModellerError.CharacterNotFound,
                            "Instance has missing character: " + path[i]);
                }
            }

            return po;
        }

        /// <summary>
        /// Marks all code as tampered which will force dissassembly and re-assembly. Useful
        /// in testing.
        /// </summary>
        public void MarkCodeAsTampered()
        {
            foreach (DoABC script in this.scripts)
            {
                script.MarkCodeAsTampered();
            }
        }

        /// <summary>
        /// Finds the movieclip that has been bound to a class.
        /// </summary>
        /// <param name="clazz">The class to look up</param>
        /// <returns>The clip, or null if the classname is not bound to a clip.</returns>
        public Timeline ClipFromClass(AS3ClassDef clazz)
        {
            if (this.clipClassMap.ContainsKey(clazz))
            {
                return this.clipClassMap[clazz];
            }

            return null;
        }

        /// <summary>
        /// Find a class by name.
        /// </summary>
        /// <param name="name">The class name.</param>
        /// <returns>A class, or null if none match</returns>
        public AS3ClassDef ClassByName(string name)
        {
            if (name == null)
            {
                return null;
            }

            foreach (DoABC script in this.scripts)
            {
                AS3ClassDef c = script.Code.GetClassByName(name);

                if (c != null)
                {
                    /* ISSUE 25: The chance for different classes with the same name to appear in
                     * different DoABC tags is enough to convince us that we should only really 
                     * have one tag, not this silly list of "scripts" */
                    return c;
                }
            }

            return null;
        }

        /// <summary>
        /// Runs a delegate function on each script in the SWF.
        /// </summary>
        /// <param name="sd">The delegate to run.</param>
        public void ScriptProc(ScriptProcessor sd)
        {
            foreach (DoABC abc in this.scripts)
            {
                sd(abc);
            }
        }

        /// <summary>
        /// Generates the timeline scripts.
        /// </summary>
        public void GenerateTimelineScripts()
        {
            if (this.scripts.Count != 0)
            {
                throw new SWFModellerException(
                        SWFModellerError.Internal,
                        "Internal error. Can't make auto-generated scripts if scripts already exist.");
            }

            string flaName = this.Context.Name.Replace('.', '_') + "_swiffotron";
            string qClassName = flaName + ".MainTimeline";

            this.scripts.Add(DoABC.GenerateDefaultScript(qClassName, this));
        }

        /// <summary>
        /// Maps the classname to a clip.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="tl">The timeline to bind the class to.</param>
        internal void MapClassnameToClip(string className, Timeline tl)
        {
            AS3ClassDef clazz = this.ClassByName(className);
            tl.Class = clazz;
            this.clipClassMap[clazz] = tl;
        }

        /// <summary>
        /// Renames the main timeline class.
        /// </summary>
        /// <param name="classQName">New name of the class.</param>
        internal void RenameMainTimelineClass(string classQName)
        {
            if (this.Class == null)
            {
                throw new SWFModellerException(SWFModellerError.Internal, "Can't rename non-existant timeline class.");
            }

            AS3ClassDef classDef = this.Class as AS3ClassDef;
            if (classDef == null)
            {
                throw new SWFModellerException(SWFModellerError.Internal, "MainTimeline class is an in-built class, which is impossible.");
            }


            int splitPos = classQName.LastIndexOf('.');

            string className = classQName;
            string newPackageName = string.Empty;
            if (splitPos != -1)
            {
                newPackageName = classQName.Substring(0, splitPos);
                className = classQName.Substring(splitPos + 1);
            }

            /* Class name will be a QName, so won't have a NS set */
            Multiname oldName = classDef.Name;
            Namespace oldNameNS = oldName.NS;
            Multiname newName = this.scripts[0].Code.CreateMultiname(
                    oldName.Kind,
                    className,
                    this.scripts[0].Code.CreateNamespace(oldNameNS.Kind, newPackageName),
                    null);

            Namespace oldProtectedNS = classDef.ProtectedNS;
            Namespace newProtectedNS = null;
            if (oldProtectedNS != null)
            {
                newProtectedNS = this.scripts[0].Code.CreateNamespace(oldProtectedNS.Kind, newPackageName + ":" + className);
            }

            this.ClassProc(delegate(AS3ClassDef c)
            {
                bool inRenamedClass = false;

                if (c.Name == oldName)
                {
                    inRenamedClass = true;
                    c.Name = newName;
                }

                if (c.Supername == oldName)
                {
                    c.Supername = newName;
                }

                if (c.ProtectedNS == oldProtectedNS)
                {
                    c.ProtectedNS = newProtectedNS;
                }

                c.TraitProc(delegate(ref Trait t, AbcCode abc)
                {
                    Namespace traitNS = t.Name.NS;
                    if (traitNS == null)
                    {
                        return;
                    }

                    if (!t.Name.IsEmptySet)
                    {
                        /* ISSUE 32: Delete this once confidence is felt. */
                        throw new SWFModellerException(
                                    SWFModellerError.UnimplementedFeature,
                                    "Trait name has a set. I didn't think that was possible! Following code needs fixed!!!");
                    }

                    switch (traitNS.Kind)
                    {
                        case Namespace.NamespaceKind.PackageInternal:
                        case Namespace.NamespaceKind.Package:
                            if (inRenamedClass && traitNS.Name == oldNameNS.Name)
                            {
                                t.Name = this.scripts[0].Code.CreateMultiname(
                                        t.Name.Kind,
                                        t.Name.Name,
                                        this.scripts[0].Code.CreateNamespace(traitNS.Kind, newPackageName),
                                        null);
                            }

                            break;

                        default:
                            throw new SWFModellerException(
                                    SWFModellerError.UnimplementedFeature,
                                    "As yet unsupported namespace (" + traitNS.Kind + ") when remapping a class trait (" + c.Name.Name + "::" + t.Name + ")");
                    }
                });
            });

            this.MethodProc(delegate(Method m, AS3ClassDef c)
            {
                bool inRenamedClass = c.Name == newName;

                m.OpcodeFilter(delegate(ref Opcode op, AbcCode abc)
                {
                    if (op.Args == null)
                    {
                        return;
                    }

                    for (int i = 0; i < op.Args.Length; i++)
                    {
                        object arg = op.Args[i];
                        if (arg is Multiname)
                        {
                            Multiname mn = (Multiname)arg;
                            if (mn == oldName)
                            {
                                op.Args[i] = newName;
                            }
                            else if (inRenamedClass)
                            {
                                bool isMnModified = false;
                                Namespace multinameNS = mn.NS;
                                NamespaceSet multinameSet = mn.Set;

                                if (multinameNS != null)
                                {
                                    switch (multinameNS.Kind)
                                    {
                                        case Namespace.NamespaceKind.Package:
                                        case Namespace.NamespaceKind.PackageInternal:
                                            if (inRenamedClass && multinameNS.Name == oldNameNS.Name)
                                            {
                                                multinameNS = this.scripts[0].Code.CreateNamespace(
                                                        multinameNS.Kind,
                                                        newPackageName);
                                                isMnModified = true;
                                            }

                                            break;

                                        default:
                                            throw new SWFModellerException(
                                                    SWFModellerError.UnimplementedFeature,
                                                    "As yet unsupported multiname's NS kind (" + multinameNS.Kind.ToString() + ") in op's Multiname arg during MethodProc");
                                    }
                                }

                                if (multinameSet != null)
                                {
                                    List<Namespace> newSet = new List<Namespace>(multinameSet.Count);
                                    bool isSetModified = false;
                                    foreach (Namespace ns in multinameSet)
                                    {
                                        switch (ns.Kind)
                                        {
                                            case Namespace.NamespaceKind.Protected:
                                            case Namespace.NamespaceKind.StaticProtected:
                                                if (ns.Name == oldProtectedNS.Name)
                                                {
                                                    newSet.Add(this.scripts[0].Code.CreateNamespace(
                                                            ns.Kind,
                                                            newProtectedNS.Name));
                                                    isSetModified = true;
                                                    continue;
                                                }

                                                break;

                                            case Namespace.NamespaceKind.Package:
                                            case Namespace.NamespaceKind.PackageInternal:
                                            case Namespace.NamespaceKind.Private:
                                                if (inRenamedClass && ns.Name == oldNameNS.Name)
                                                {
                                                    newSet.Add(this.scripts[0].Code.CreateNamespace(
                                                            ns.Kind,
                                                            newPackageName));
                                                    isSetModified = true;
                                                    continue;
                                                }

                                                break;

                                            case Namespace.NamespaceKind.Ns:
                                                if (ns.Name == oldNameNS.Name)
                                                {
                                                    newSet.Add(this.scripts[0].Code.CreateNamespace(
                                                            ns.Kind,
                                                            newPackageName));
                                                    isSetModified = true;
                                                    continue;
                                                }

                                                break;

                                            default:
                                                throw new SWFModellerException(
                                                        SWFModellerError.UnimplementedFeature,
                                                        "As yet unsupported multiname NS set entry kind (" + ns.Kind.ToString() + ") in op's Multiname arg during MethodProc");
                                        }

                                        newSet.Add(ns);
                                    }

                                    if (isSetModified)
                                    {
                                        isMnModified = true;
                                        multinameSet = new NamespaceSet(newSet.ToArray());
                                    }
                                }

                                if (isMnModified)
                                {
                                    op.Args[i] = this.scripts[0].Code.CreateMultiname(
                                            mn.Kind,
                                            mn.Name,
                                            multinameNS,
                                            multinameSet);
                                }
                            }
                        }
                        else if (arg is Namespace)
                        {
                            throw new SWFModellerException(
                                    SWFModellerError.UnimplementedFeature,
                                    "As yet unsupported op arg in MethodProc: Namespace");
                        }
                        else if (arg is AS3ClassDef)
                        {
                            throw new SWFModellerException(
                                    SWFModellerError.UnimplementedFeature,
                                    "As yet unsupported op arg in MethodProc: AS3Class");
                        }
                    }
                });
            });
        }

        /// <summary>
        /// Runs a delegate function over every method in the SWF.
        /// </summary>
        /// <param name="mp">The delegate to run.</param>
        private void MethodProc(AbcCode.MethodProcessor mp)
        {
            foreach (DoABC script in this.scripts)
            {
                script.MethodProc(mp);
            }
        }

        /// <summary>
        /// Does a search and replace on strings used in the actionscript code.
        /// </summary>
        /// <param name="find">The text to find.</param>
        /// <param name="replace">The new text to replace it with.</param>
        public void TextReplaceInCode(string find, string replace)
        {
            MarkCodeAsTampered();
            MethodProc(delegate(Method m, AS3ClassDef c)
            {
                m.OpcodeFilter(delegate(ref Opcode op, AbcCode abc)
                {
                    for (int i = 0; i < op.Args.Length; i++)
                    {
                        if (op.Args[i] is string)
                        {
                            op.Args[i] = op.Args[i].ToString().Replace(find, replace);
                        }
                    }
                });
            });
        }

        /// <summary>
        /// Runs a delegate function on every class in the SWF.
        /// </summary>
        /// <param name="cp">The delegate to call for each class.</param>
        private void ClassProc(ClassProcessor cp)
        {
            bool mainClassProcessed = false;

            foreach (DoABC script in this.scripts)
            {
                AbcCode code = script.Code;

                foreach (AS3ClassDef c in code.Classes)
                {
                    if (c == this.Class)
                    {
                        mainClassProcessed = true;
                    }
                    cp(c);
                }
            }

            if (!mainClassProcessed)
            {
                cp((AS3ClassDef)this.Class);
            }
        }

        /// <summary>
        /// Adds a font to the SWF.
        /// </summary>
        /// <param name="font">The font.</param>
        internal void AddFont(SWFFont font)
        {
            fonts.Add(font);
        }

        /// <summary>
        /// Gets a character from the internal dictionary.
        /// </summary>
        /// <param name="id">The id of the character to fetch.</param>
        /// <returns>A character, which must exist.</returns>
        public ICharacter GetCharacter(string id)
        {
            return this.dictionary[id];
        }
    }
}
