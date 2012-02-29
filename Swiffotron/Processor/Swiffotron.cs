//-----------------------------------------------------------------------
// Swiffotron.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.XPath;
    using SWFProcessing.SWFModeller;
    using SWFProcessing.SWFModeller.ABC.Code;
    using SWFProcessing.SWFModeller.ABC.Debug;
    using SWFProcessing.SWFModeller.Characters;
    using SWFProcessing.SWFModeller.Characters.Geom;
    using SWFProcessing.SWFModeller.Characters.Text;
    using SWFProcessing.SWFModeller.IO;
    using SWFProcessing.SWFModeller.Modelling;
    using SWFProcessing.SWFModeller.Process;
    using SWFProcessing.Swiffotron.IO.Debug;
    using SWFProcessing.Swiffotron.Processor;
    using SWF2Raster = SWFProcessing.SWF2Raster.SWF2Raster;
    using SWF2SVG = SWFProcessing.SWF2SVG.SWF2SVG;

    /// <summary>
    /// <para>
    /// Main entry point to the swiffotron's functionality. This is the main
    /// object you need to create to start building SWF files.</para>
    /// </summary>
    public class Swiffotron
    {
        private Configuration conf;

        private Caches caches;

        private Stores stores;

#if(DEBUG)
        /* We could use MSTest's private access, but we want the express versions
         * to be in on the fun too. Horrible, but there you go. */
        public Configuration conf_accessor
        {
            get
            {
                return this.conf;
            }
        }
#endif

        /* ISSUE 55: Would be good to also store a 'processedSWFsRenderedAsMovieClips' so that
         * we don't keep converting the same clip over and over again when it's re-used.
         * Mental note: pretty sure SWF objects are being subtley modified in annoying ways,
         * when cache copies should be pristine.
         * Also.. unit tests for this stuff. 
         * Further note. This appears to replicate the functionality of the SWF.dictionary
         * object. These two things need reviewed, consolidated, or just to have their heads
         * banged together. */

        /// <summary>A map of SWF models that have been processed already.</summary>
        private Dictionary<string, SWF> processedSWFs;

        /// <summary>
        /// This is a list of navigator objects pointing at SWF tags. Each of them
        /// represents a single job to do and the list is in the correct order. E.g.
        /// if a swf depends on another swf, the dependency will be first in the list.
        /// In this way we can process SWFs and not worry about child swf elements.
        /// </summary>
        private List<XPathNavigator> processingList;

#if DEBUG
        public List<XPathNavigator> processingList_accessor
        {
            get
            {
                return this.processingList;
            }
        }

        /// <summary>
        /// Used only in debug builds. This intercepts all byte arrays of ABC data
        /// that the swiffotron comes accross.
        /// </summary>
        private IABCLoadInterceptor abcInterceptor;

        /// <summary>
        /// Used only in debug builds. Whenever Swiffotron reads a SWF file, it will call
        /// back with a log of read activity.
        /// </summary>
        private ISwiffotronReadLogHandler readLogHandler;
#endif

        /// <summary>
        /// Whenever Swiffotron commits a new SWF file, it calls this. This happens even
        /// if store writes are disabled.
        /// </summary>
        private Dictionary<string, byte[]> commitStore;
        
        /// <summary>
        /// A list of element dependency maps.
        /// </summary>
        private List<DependencyList> dependencyMap;

        /// <summary>
        /// Local copies of cached objects we might need, just in case they get ejected
        /// before we use them.
        /// </summary>
        private Dictionary<string, object> localCache;

        private SWFProcessor swfProc;

        private XMLHelper Xml;

        /// <summary>
        /// Initializes a new instance of the Swiffotron class. It will be created with
        /// default configuration options.
        /// </summary>
        public Swiffotron()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Swiffotron class.
        /// </summary>
        /// <param name="configStream">An open stream to the config XML data, or null to
        /// use default configuration.</param>
        public Swiffotron(Stream configStream)
        {
            if (configStream == null)
            {
                configStream = Assembly
                        .GetExecutingAssembly()
                        .GetManifestResourceStream(@"SWFProcessing.Swiffotron.res.default-config.xml");
            }

            this.conf = new Configuration(configStream);

            this.Xml = new XMLHelper();

            this.stores = this.conf.Stores;
            this.caches = this.conf.Caches;
        }

        /// <summary>
        /// Context for enriching debug log output
        /// </summary>
        public SwiffotronContext Context { get; set; }

        /// <summary>
        /// Process a job XML file.
        /// </summary>
        /// <param name="xml">An open stream to the XML data.</param>
        /// <param name="commitStore">If not null, this will hold all store commits made
        /// by this job.</param>
        /// <param name="writeLog">Ignored in release builds. This will accumulate a
        /// log of write operations into the output SWF file(s).</param>
        /// <param name="abcWriteLog">A log of write events to ABC data within the
        /// output SWF files.</param>
        /// <param name="abcInterceptor">Ignored in release builds. This will be called
        /// when an ABC file is loaded as an opportunity to dump the data to file
        /// for inspection.</param>
        /// <param name="readLogHandler">Ignored in release builds. Whenever
        /// the Swiffotron reads a SWF file, this is called so that it can dump read
        /// operations to a log.</param>
        public void Process(
                Stream xml,
                Dictionary<string, byte[]> commitStore = null,
                StringBuilder writeLog = null,
                StringBuilder abcWriteLog = null,
                IABCLoadInterceptor abcInterceptor = null,
                ISwiffotronReadLogHandler readLogHandler = null)
        {
#if DEBUG
            this.abcInterceptor = abcInterceptor;
            this.readLogHandler = readLogHandler;
#endif
            this.commitStore = commitStore;
            Xml.LoadSwiffotronXML(xml);
            string jobID = Xml.SelectString("swf:swiffotron/@id");
            this.Context = new SwiffotronContext(jobID);
            this.Xml.SetContext(this.Context);
            this.swfProc = new SWFProcessor(this.Context);

            this.processingList = new List<XPathNavigator>();
            this.dependencyMap = new List<DependencyList>();

            this.processedSWFs = new Dictionary<string, SWF>();

            this.localCache = new Dictionary<string, object>();

            /* Take local copies of all referenced cache objects to guard against
             * them being ejected before we access them, since we work out what we
             * need to do based on what's in the cache before we do it. */
            foreach (XPathNavigator keyNode in Xml.Select(@"//@cachekey"))
            {
                string key = keyNode.ToString();
                object data = this.caches.Get(this.Context, key);
                if (data != null)
                {
                    this.localCache.Add(key, data);
                }
            }

            /* Select all the swf tags that have some sort of output: */
            foreach (XPathNavigator outputTag in Xml.Select(@"//swf:swfout|//swf:pngout|//swf:vidout|//swf:svgout"))
            {
                XmlAttributeCollection attribs = ((XmlElement)outputTag.UnderlyingObject).Attributes;

                /* ISSUE 28: Check the runifnotchanged thing. */

                string dest = outputTag.GetAttribute(XMLHelper.AttrStore, string.Empty);
                outputTag.MoveToParent(); /* Select SWF tag */
                if (!this.IsInDependenciesMap(outputTag))
                {
                    /* Add them with no dependencies. We'll work out dependencies in the
                     * next step. */
                    this.dependencyMap.Add(new DependencyList(outputTag, null));
                }
            }

            int pos = 0;
            while (pos < this.dependencyMap.Count)
            {
                this.AddDependencies(this.dependencyMap[pos]);
                pos++;
            }

            /* Now that we have a list of things and their dependencies, we
             * need to copy those nodes into the processingList in the correct
             * order. */

            while (this.dependencyMap.Count > 0)
            {
                List<DependencyList> newMap = new List<DependencyList>();
                foreach (DependencyList dep in this.dependencyMap)
                {
                    if (dep.Count == 0)
                    {
                        this.processingList.Add(dep.Node);
                        foreach (DependencyList filterDep in this.dependencyMap)
                        {
                            filterDep.RemoveDependency(dep.Node);
                        }
                    }
                    else
                    {
                        /* Things still to be done. */
                        newMap.Add(dep);
                    }
                }

                if (newMap.Count == this.dependencyMap.Count)
                {
                    /* No progress was made, so: */
                    throw new SwiffotronException(
                            SwiffotronError.BadInputXML,
                            this.Context,
                            @"A circular dependency was detected.");
                }

                this.dependencyMap = newMap;
            }

            /* And so, after all that, we can begin: */
            this.GenerateSWFs(writeLog, abcWriteLog);
        }

        /// <summary>
        /// Once the processing list has been established, we can run over the list and
        /// process each one in turn.
        /// </summary>
        /// <param name="writeLog">An optional string builder that will accumulate a log
        /// of write actions.</param>
        /// <param name="abcWriteLog">An optional string builder that will accumulate a
        /// log of write actions to ABC data within the SWF files.</param>
        private void GenerateSWFs(StringBuilder writeLog, StringBuilder abcWriteLog)
        {
            foreach (XPathNavigator swfTag in this.processingList)
            {
                this.ProcessSWF(
                        swfTag,
                        swfTag.GetAttribute(XMLHelper.AttrBase, string.Empty),
                        Xml.IntegerAttribute(swfTag, XMLHelper.AttrWidth),
                        Xml.IntegerAttribute(swfTag, XMLHelper.AttrHeight),
                        Xml.ColorAttribute(swfTag, XMLHelper.AttrBGColor),
                        swfTag.GetAttribute(XMLHelper.AttrCachekey, string.Empty),
                        writeLog,
                        abcWriteLog);
            }
        }

        /// <summary>
        /// Load a SWF from a store path.
        /// </summary>
        /// <param name="path">A store path from the job XML</param>
        /// <returns>A parsed SWF object.</returns>
        private SWF SwfFromStore(string path)
        {
            using (Stream s = stores.Open(this.Context, path))
            {
                string name = null;

#if DEBUG
                name = new Uri(path).AbsolutePath.Substring(1);

                if (this.readLogHandler != null)
                {
                    StringBuilder sb = new StringBuilder();

                    SWF swf = new SWFReader(
                            s,
                            new SWFReaderOptions() { StrictTagLength = true },
                            sb,
                            this.abcInterceptor)
                        .ReadSWF(new SWFContext(name));

                    this.readLogHandler.OnSwiffotronReadSWF(name, swf, sb.ToString());
                    return swf;
                }

                return new SWFReader(
                        s,
                        new SWFReaderOptions() { StrictTagLength = true },
                        null,
                        this.abcInterceptor)
                    .ReadSWF(new SWFContext(name));
#else
                return new SWFReader(s, new SWFReaderOptions() { StrictTagLength = true }, null, null).ReadSWF(new SWFContext(name));
#endif
            }
        }

        /// <summary>
        /// Process a SWF tag in the job XML
        /// </summary>
        /// <param name="swfTag">A pointer to the SWF node</param>
        /// <param name="baseSwf">A path to the SWF file upon which to base this new SWF</param>
        /// <param name="width">The stage width</param>
        /// <param name="height">The stage height</param>
        /// <param name="bgcolor">The stage colour</param>
        /// <param name="cachekey">A key to cache the results in (Or to get it from, which would
        /// be ever so nice)</param>
        /// <param name="writeLog">Only used in debug builds. This will accumulate a log of
        /// writes to an output from this SWF.</param>
        /// <param name="abcWriteLog">Only used in debug builds. This will accumulate a log of
        /// writes to ABC bytecode data within the output SWF.</param>
        private void ProcessSWF(
                XPathNavigator swfTag,
                string baseSwf,
                int? width,
                int? height,
                Color? bgcolor,
                string cachekey,
                StringBuilder writeLog,
                StringBuilder abcWriteLog)
        {
            SWF swf;
            if (baseSwf == string.Empty)
            {
                /* It's a new SWF */
                swf = new SWF(new SWFContext(swfTag.GetAttribute(XMLHelper.AttrID, string.Empty)), true);

                /* ISSUE 56: If this SWF has no output, then perhaps we can create it as a movieclip
                 * since it'll certainly be converted into one later on. */
            }
            else
            {
                /* It's a SWF loaded from a store */
                swf = this.SwfFromStore(baseSwf);
            }

            if (bgcolor != null)
            {
                swf.BackgroundColor = (Color)bgcolor;
            }

            if (width != null)
            {
                swf.FrameWidth = (int)width;
            }

            if (height != null)
            {
                swf.FrameHeight = (int)height;
            }

            if (swfTag.HasChildren)
            {
                /* Move down to the swf tag child nodes which we will process in order. */
                XPathNavigator nav = swfTag.Clone();
                Xml.MoveToFirstChildElement(nav);

                do
                {
                    switch (nav.LocalName)
                    {
                        case XMLHelper.TagModify:
                            this.ModifyInstance(nav, swf);
                            break;

                        case XMLHelper.TagTextReplace:
                            this.TextReplace(nav, swf);
                            break;

                        case XMLHelper.TagInstance:
                            this.CreateInstance(nav, swfTag, swf);
                            break;

                        case XMLHelper.TagRemove:
                            this.RemoveInstance(nav, swf);
                            break;

                        case XMLHelper.TagMovieClip:
                            this.CreateMovieClip(nav, swfTag, swf);
                            break;

                        case XMLHelper.TagSwfOut:
                        case XMLHelper.TagPngOut:
                        case XMLHelper.TagVidOut:
                        case XMLHelper.TagSvgOut:
                            /* These are not processing steps. Skip 'em. */
                            break;

                        default:
                            /* ISSUE 73 */
                            throw new SwiffotronException(
                                    SwiffotronError.UnimplementedFeature,
                                    this.Context,
                                    @"Unsupported tag: " + nav.LocalName);
                    }
                }
                while (nav.MoveToNext(XPathNodeType.Element));
            }

            this.processedSWFs.Add(swfTag.GetAttribute(XMLHelper.AttrID, string.Empty), swf);

            this.ProcessSWFOutput(swfTag, swf, writeLog, abcWriteLog);
        }

        /// <summary>
        /// Processes a text replacement tag
        /// </summary>
        /// <param name="nav">The text replacement node.</param>
        /// <param name="swf">The SWF being processed.</param>
        private void TextReplace(XPathNavigator nav, SWF swf)
        {
            XPathNavigator findNode = Xml.SelectNode(nav, @"swf:find/text()");
            if (findNode == null)
            {
                throw new SwiffotronException(SwiffotronError.BadInputXML, this.Context, "The find element in textreplace operations cannot be empty.");
            }

            string find = findNode.Value;

            XPathNavigator replaceNode = Xml.SelectNode(nav, @"swf:replace/text()");
            string replace = replaceNode == null ? string.Empty : replaceNode.Value;

            foreach (XPathNavigator loc in Xml.SelectChildren(nav, @"location"))
            {
                string type = loc.GetAttribute(XMLHelper.AttrType, string.Empty);
                switch (type)
                {
                    case XMLHelper.ValActionscript:
                        swf.TextReplaceInCode(find, replace);
                        break;

                    case XMLHelper.ValMovieClip:
                        string path = loc.GetAttribute(XMLHelper.AttrPath, string.Empty);
                        Timeline[] clips = new Timeline[] { swf };
                        if (path != "*")
                        {
                            clips = swfProc.SpritesFromQname(path, swf, true);
                        }

                        foreach (Timeline clip in clips)
                        {
                            clip.CharacterProc(delegate(ICharacter ch)
                            {
                                if (ch is IText)
                                {
                                    IText t = (IText)ch;
                                    string text = t.Text;
                                    if (text.Contains(find))
                                    {
                                        /* The Contains test may seem redundant here, but the cost of the
                                         * assignment, even if there was nothing replaced is potentially
                                         * quite high. */
                                        t.Text = text.Replace(find, replace);
                                    }
                                }
                            });
                        }

                        break;

                    default:
                        throw new SwiffotronException(SwiffotronError.BadInputXML, this.Context, "Bad text replace location type: " + type);
                }
            }
        }

        /// <summary>
        /// Processes a remove instance tag
        /// </summary>
        /// <param name="nav">The remove node</param>
        /// <param name="swf">The SWF to remove from</param>
        private void RemoveInstance(XPathNavigator nav, SWF swf)
        {
            string qname = nav.GetAttribute(XMLHelper.AttrQName, string.Empty);

            string uname;
            Timeline parent = this.QNameToTimeline(qname, swf, out uname);

            if (!parent.RemoveInstance(uname))
            {
                /* ISSUE 57: Unit test for this please. Also for non-existant parent names in the qname. */
                throw new SwiffotronException(SwiffotronError.BadPathOrID, this.Context, "Cannot remove '" + qname + "'; it does not exist.");
            }
        }

        /// <summary>
        /// Process an instantiation tag in the job XML
        /// </summary>
        /// <param name="insTag">A pointer to the instantiation node</param>
        /// <param name="currentSwfTag">A pointer to the current SWF node that contains
        /// the instance node.</param>
        /// <param name="swf">The SWF within which to instantiate something.</param>
        private void CreateInstance(XPathNavigator insTag, XPathNavigator currentSwfTag, SWF swf)
        {
            string type = insTag.GetAttribute(XMLHelper.AttrType, string.Empty);
            string src = insTag.GetAttribute(XMLHelper.AttrSrc, string.Empty);
            string className = insTag.GetAttribute(XMLHelper.AttrClass, string.Empty);

            /* ISSUE 58: If the instance name (id) is the same as the class name, it can
             * cause problems in files generated and decompiled again in sothink. We should
             * probably detect this and warn against it. */

            switch (type)
            {
                case XMLHelper.ValSwf:
                    if (className == string.Empty)
                    {
                        throw new SwiffotronException(
                                SwiffotronError.BadInputXML,
                                this.Context,
                                "An instance created from a SWF needs a class name to be defined.");
                    }

                    if (src.StartsWith("store://"))
                    {
                        throw new SwiffotronException(SwiffotronError.BadPathOrID, this.Context, "Unexpected store path. Did you mean for the type to be extern, perhaps?");
                    }

                    if (!this.processedSWFs.ContainsKey(src))
                    {
                        throw new SwiffotronException(
                                SwiffotronError.Internal,
                                this.Context,
                                "Internal error. SWF tags were processed out of order (" + currentSwfTag.GetAttribute(XMLHelper.AttrID, string.Empty) + " requres " + src + ").");
                    }

                    SWF processedSWF = this.processedSWFs[src];

                    this.CreateInstanceFromSWF(insTag, swf, className, processedSWF);
                    break;

                case XMLHelper.ValMovieClip:
                    Sprite clip = swf.GetCharacter(src) as Sprite;
                    if (clip == null)
                    {
                        throw new SwiffotronException(SwiffotronError.BadInputXML, this.Context, "MovieClip not defined: " + src);
                    }

                    this.CreateInstanceIn(
                            insTag.GetAttribute(XMLHelper.AttrID, string.Empty),
                            swf,
                            insTag,
                            clip,
                            className);

                    clip.SpriteProc(delegate(Sprite s)
                    {
                        if (s.Class != null && !(s.Class is AdobeClass))
                        {
                            /* ISSUE 29: Only do this if the class hasn't already been bound.
                             * Note that this will be a problem if one movieclip is used to create
                             * several instances. At the time of writing, there is no unit test for
                             * this case. */
                            swf.FirstScript.Code.GenerateClipClassBindingScript(s);
                        }
                    });

                    break;

                case XMLHelper.ValInstance:
                    if (className != string.Empty)
                    {
                        throw new SwiffotronException(
                                SwiffotronError.BadInputXML,
                                this.Context.Sentinel("ClassNameInClonedInstance"),
                                "An instance cannot be given a new classname if it is a clone of an existing instance (" + className + ")");
                    }

                    Sprite srcSprite = swfProc.SpritesFromQname(src, swf, false)[0];
                    if (!srcSprite.HasClass)
                    {
                        srcSprite.Class = AdobeClass.CreateFlashDisplayMovieClip(srcSprite.Root.FirstScript.Code);
                        className = srcSprite.Class.QualifiedName;
                    }

                    this.CreateInstanceIn(
                            insTag.GetAttribute(XMLHelper.AttrID, string.Empty),
                            swf,
                            insTag,
                            srcSprite,
                            className);
                    break;

                case XMLHelper.ValExtern:
                    SWF importSwf = this.SwfFromStore(src);

                    this.CreateInstanceFromSWF(insTag, swf, className, importSwf);

                    break;

                default:
                    /* ISSUE 73 */
                    throw new SwiffotronException(
                            SwiffotronError.UnimplementedFeature,
                            this.Context,
                            "Bad instance type: " + type);
            }
        }

        /// <summary>
        /// Creates an instance from a referenced SWF.
        /// </summary>
        /// <param name="insTag">The instance tag.</param>
        /// <param name="swf">The SWF to place the instance into.</param>
        /// <param name="className">Name of the class for the new clip.</param>
        /// <param name="importSwf">The SWF to import as a clip.</param>
        private void CreateInstanceFromSWF(XPathNavigator insTag, SWF swf, string className, SWF importSwf)
        {
            if (!swf.HasClass)
            {
                /* Can't create instances if the parent timeline has no code now can we? */
                swf.GenerateTimelineScripts();
            }

            bool isAdobeClassname = className.StartsWith("flash.")
                || className.StartsWith("fl.")
                || className.StartsWith("adobe.")
                || className.StartsWith("air.")
                || className.StartsWith("flashx.");

            if (isAdobeClassname && importSwf.HasClass)
            {
                /* Can't rename a class to an Adobe class name. That's bonkers. */
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML,
                        this.Context.Sentinel("InstanceClassNameInappropriate"),
                        "You can't rename a timeline class to a reserved adobe classname (" + className + "), SWF: " + importSwf.Context);
            }

            if (className == string.Empty)
            {
                if (importSwf.Class == null)
                {
                    /* No class name is fine if the imported SWF has no class to rename. We need
                     * a class to bind it to though, so let's make it MovieClip, just like
                     * real flash. */
                    className = "flash.display.MovieClip";
                }
                else
                {
                    throw new SwiffotronException(
                            SwiffotronError.BadInputXML,
                            this.Context.Sentinel("MainTimelineInstanceNotRenamed"),
                            "An external instance with timeline code must be explicitely renamed with the instance tag's class attribute.");
                }
            }

            if (className == "flash.display.MovieClip")
            {
                Sprite spr = new Sprite(importSwf, swf);

                this.CreateInstanceIn(
                        insTag.GetAttribute(XMLHelper.AttrID, string.Empty),
                        swf,
                        insTag,
                        spr,
                        className);
            }
            else
            {
                Sprite spr = new Sprite(importSwf, swf, className);

                this.CreateInstanceIn(
                        insTag.GetAttribute(XMLHelper.AttrID, string.Empty),
                        swf,
                        insTag,
                        spr,
                        className);

                spr.SpriteProc(delegate(Sprite s)
                {
                    if (s.Class != null && !(s.Class is AdobeClass))
                    {
                        /* ISSUE 29: Only do this if the class hasn't already been bound. */
                        swf.FirstScript.Code.GenerateClipClassBindingScript(s);
                    }
                });

                if (spr.Class != null)
                {
                    swf.FirstScript.Code.GenerateClipClassBindingScript(spr);
                }
            }
        }

        /// <summary>
        /// Create a MovieClip that can later be referenced by SWFs and instantiated.
        /// </summary>
        /// <param name="movieClipTag">A pointer to the movie clip node in the job XML</param>
        /// <param name="currentSwfTag">A pointer to the currently processing SWF node</param>
        /// <param name="swf">The SWF to add the movie clip to</param>
        private void CreateMovieClip(XPathNavigator movieClipTag, XPathNavigator currentSwfTag, SWF swf)
        {
            string type = movieClipTag.GetAttribute(XMLHelper.AttrType, string.Empty);
            string src = movieClipTag.GetAttribute(XMLHelper.AttrSrc, string.Empty);
            string className = movieClipTag.GetAttribute(XMLHelper.AttrClass, string.Empty);
            string swfTag = movieClipTag.GetAttribute(XMLHelper.AttrSwf, string.Empty);

            /*
             * ISSUE 59: The examples in unit tests all put instances in the same package, e.g.
             *  <instance blahblah class="com.swiffotron.Class1" />
             *  <instance blahblah class="com.swiffotron.Class2" />
             * both put them into com.swiffotron. These are moved from the IDE generated packages
             * myswf1_fla and myswf2_fla so if you think about it, moving them both to com.swiffotron
             * makes sense because they would both have been put into the same generated package
             * if you'd done it via the IDE. The problem comes if we move them into different packages.
             * I don't know what will happen, I just haven't tried it. I have a sneaking suspicion that
             * we'll run into problems in namespace sets and the bugs will be very hard
             * to track down. Some solutions:
             * - Insist on using the same package in classes
             * - make class just be a class name, and dictate the package automatically
             * - Resolve the namespace sets to include all relevant, foreign packages (hard)
             */

            bool fromOtherSwf = swfTag != null && swfTag != string.Empty;
            if (fromOtherSwf)
            {
                /* ISSUE 60: If swf attribute is present, find the swf. It should be loaded/generated. It should not be the current swf. */
                throw new SwiffotronException(
                        SwiffotronError.UnimplementedFeature,
                        this.Context,
                        "swf attribute is not supported in movieclip tags yet.");
            }

            switch (type)
            {
                case XMLHelper.ValSwf:
                    if (fromOtherSwf)
                    {
                        throw new SwiffotronException(
                                SwiffotronError.BadInputXML,
                                this.Context,
                                "The movieclip 'swf' attribute does not make sense when the type is set to 'swf'.");
                    }

                    if (!this.processedSWFs.ContainsKey(src))
                    {
                        throw new SwiffotronException(
                                SwiffotronError.Internal,
                                this.Context,
                                "Internal error. SWF tags were processed out of order (" + currentSwfTag.GetAttribute(XMLHelper.AttrID, string.Empty) + " requres " + src + ").");
                    }

                    if (className == string.Empty)
                    {
                        throw new SwiffotronException(
                                SwiffotronError.BadInputXML,
                                this.Context,
                                "An clip created from a SWF needs a class name to be defined.");
                    }

                    SWF importSwf = this.processedSWFs[src];

                    /* ISSUE 55: There's an issue comment up there next to the declaration of processedSWFs which is intended
                     * to cache the new Sprite() bit below. */
                    swf.AddCharacter(movieClipTag.GetAttribute(XMLHelper.AttrID, string.Empty), new Sprite(importSwf, swf, className));

                    /* ISSUE 61: The above new Sprite will merge the sprite's code into the SWF regardless of whether
                     * it's instantiated. This should be done based on the exportforscript flag, or if the sprite
                     * is instantiated. Mind you, there is an argument that says if you declare a movieclip and
                     * never use it, and don't mark if for export, then you should probably remove it. Perhaps
                     * a more elegant solution would be to verify this in the XML and give a warning that you're
                     * doing redundant things. */

                    break;

                case XMLHelper.ValExtern:
                    if (fromOtherSwf)
                    {
                        throw new SwiffotronException(
                                SwiffotronError.BadInputXML,
                                this.Context,
                                "The movieclip 'swf' attribute does not make sense when the type is set to 'extern'. Try declaring the external SWF in its own tag instead, and reference it by ID.");
                    }

                    if (className == string.Empty)
                    {
                        throw new SwiffotronException(
                                SwiffotronError.BadInputXML,
                                this.Context,
                                "An clip created from a SWF needs a class name to be defined.");
                    }

                    SWF forImport = this.SwfFromStore(src);
                    try
                    {
                        swf.AddCharacter(movieClipTag.GetAttribute(XMLHelper.AttrID, string.Empty), new Sprite(forImport, swf, className));
                    }
                    catch (SWFModellerException sme)
                    {
                        if (sme.Error == SWFModellerError.CodeMerge)
                        {
                            throw new SwiffotronException(SwiffotronError.BadInputXML, this.Context.Sentinel("ClassNameCollision"), "Possible class name collision.", sme);
                        }
                        else
                        {
                            throw;
                        }
                    }

                    break;

                default:
                    /* ISSUE 73 */
                    throw new SwiffotronException(
                            SwiffotronError.UnimplementedFeature,
                            this.Context,
                            "Bad instance type: " + type);
            }
        }

        /// <summary>
        /// Creates an instance within another instance's clip, e.g. creating "mc1.mc2.mc3"
        /// will create mc3 within mc2, where mc1.mc2 is mc2's qname.
        /// </summary>
        /// <param name="qname">The qualified name of the instance to create.</param>
        /// <param name="swf">The SWF to create it in.</param>
        /// <param name="transform">This is an XML node which extends transformRelativeToType in the XSD, and
        /// can be queried for transform information.</param>
        /// <param name="charToInstantiate">The character to create an instance of.</param>
        private void CreateInstanceIn(string qname, SWF swf, XPathNavigator transform, Sprite charToInstantiate, string qClassName)
        {
            string newInsName;
            Timeline parent = this.QNameToTimeline(qname, swf, out newInsName);

            string relativeToQname = transform.GetAttribute(XMLHelper.AttrRelativeTo, string.Empty);
            Matrix m = null;
            if (relativeToQname == null || relativeToQname == string.Empty)
            {
                m = Xml.TransformTagToMatrix(transform);
                if (m.IsSimpleTranslate)
                {
                    m.TransX = m.TransX;
                    m.TransY = m.TransY;
                }
            }
            else
            {
                m = swfProc.PositionFromQname(relativeToQname, swf);
                Matrix rel = Xml.TransformTagToMatrix(transform);
                if (rel.IsSimpleTranslate)
                {
                    m.Translate(rel.TransX, rel.TransY);
                }
                else
                {
                    m.Apply(rel);
                }
            }

            try
            {
                parent.Instantiate(1, charToInstantiate, Layer.Position.Front, m, newInsName, qClassName);
            }
            catch (SWFModellerException sme)
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML,
                        this.Context.Sentinel("CreateInstanceIn"),
                        "Failed to instantiate an instance in a timeline. instance name:" + qname + ", instance class:" + qClassName,
                        sme);
            }
        }

        /// <summary>
        /// Finds a timeline from a qualified instance name.
        /// </summary>
        /// <param name="qname">The dotted qname path to the instance.</param>
        /// <param name="swf">The SWF to search.</param>
        /// <param name="uname">The unqualified name of the found instance is returned here.</param>
        /// <returns>The found timeline</returns>
        private Timeline QNameToTimeline(string qname, SWF swf, out string uname)
        {
            int lpos = qname == null ? -1 : qname.LastIndexOf('.');
            uname = qname;
            if (lpos == -1)
            {
                /* If this is just a simple instance name, return the stage. */
                return swf;
            }
            else
            {
                uname = qname.Substring(lpos + 1);
                string parentQname = qname.Substring(0, lpos);
                PlaceObject parentIns = swf.LookupInstance(parentQname);
                if (parentIns == null)
                {
                    throw new SwiffotronException(
                            SwiffotronError.BadPathOrID,
                            this.Context,
                            "Ancestor of '" + uname + "', i.e. '" + parentQname + "' does not exist.");
                }

                ICharacter parentChar = parentIns.Character;

                if (!(parentChar is Timeline))
                {
                    throw new SwiffotronException(
                            SwiffotronError.BadPathOrID,
                            this.Context,
                            "QName '" + parentQname + "' does not refer to a timeline.");
                }

                return (Timeline)parentChar;
            }
        }

        /// <summary>
        /// Executes a modify tag on a SWF
        /// </summary>
        /// <param name="modify">A navigator pointing to the modify element in the XML</param>
        /// <param name="swf">The SWF to modify</param>
        private void ModifyInstance(XPathNavigator modify, SWF swf)
        {
            string qname = modify.GetAttribute(XMLHelper.AttrQName, string.Empty);

            PlaceObject po = swf.LookupInstance(qname);

            /* ISSUE 63: There is a question of whether to error if the instance is not found. Some are
             * found with a pattern rather than a path, and you may not expect it to always find something. 
             * At the moment, we shall throw an exception, because it suits our development, unit testing
             * fail-fast strictness. */
            if (po == null)
            {
                throw new SwiffotronException(
                        SwiffotronError.BadPathOrID,
                        this.Context.Sentinel("ModifyInstance"),
                        @"Instance not found: " + qname);
            }

            Xml.MoveToFirstChildElement(modify);

            do
            {
                if (modify.NodeType == XPathNodeType.Element)
                {
                    switch (modify.LocalName)
                    {
                        case XMLHelper.TagMoveRel:
                            Matrix rel = Xml.TransformTagToMatrix(modify);
                            po.Matrix.Apply(rel);
                            break;

                        case XMLHelper.TagMoveAbs:
                            Matrix abs = Xml.TransformTagToMatrix(modify);
                            po.Matrix = abs;
                            break;

                        default:
                            /* ISSUE 73 */
                            throw new SwiffotronException(
                                    SwiffotronError.UnimplementedFeature,
                                    this.Context,
                                    @"Unsupported modification tag: " + modify.LocalName);
                    }
                }
            }
            while (modify.MoveToNext(XPathNodeType.Element));

            modify.MoveToParent();
        }

        private void SaveToStore(string key, byte[] data)
        {
            string relativePath = this.stores.Save(this.Context, key, data);

            /* ISSUE 79 - An interesting upshot here is that if you know you're never
             * going to save files back to a store, then you can get away with
             * using any string you like in swfout store keys. */

            if (this.commitStore != null && relativePath != null)
            {
                /* For debug purposes, we can intercept and inspect every file the
                 * swiffotron ejects from it's glossy, futuristic core. */
                this.commitStore.Add(relativePath, data);
            }
        }

        /// <summary>
        /// Serialize a SWF to a store path
        /// </summary>
        /// <param name="swfNav">The node of the SWF being stored</param>
        /// <param name="swf">The processed SWF object</param>
        /// <param name="writeLog">Only used in debug builds. Accumulates a log of write operations
        /// to the SWF file.</param>
        /// <param name="abcWriteLog">Only used in debug builds. Accumulates a log of write operations
        /// to the ABC data within the SWF file.</param>
        private void ProcessSWFOutput(XPathNavigator swfNav, SWF swf, StringBuilder writeLog, StringBuilder abcWriteLog)
        {
            byte[] swfData = null;
            byte[] pngData = null;
            byte[] svgData = null;

            int swfOuts = Xml.SelectChildren(swfNav, @"swfout").Count;
            int pngOuts = Xml.SelectChildren(swfNav, @"pngout").Count;
            int vidOuts = Xml.SelectChildren(swfNav, @"vidout").Count;
            int svgOuts = Xml.SelectChildren(swfNav, @"svgout").Count;

            if (swfOuts > 0)
            {
                swfData = new SWFWriter(swf, conf.swfWriterOptions, writeLog, abcWriteLog).ToByteArray();
            }

            foreach (XPathNavigator swfout in Xml.SelectChildren(swfNav, @"swfout"))
            {
                string swfoutStore = swfout.GetAttribute(@"store", string.Empty);

                if (swfoutStore == string.Empty)
                {
                    throw new SwiffotronException(
                            SwiffotronError.BadInputXML,
                            this.Context,
                            @"The swfout tag needs either a cachekey or a storeput attribute");
                }

                this.SaveToStore(swfoutStore, swfData);
            }

            if (pngOuts > 0)
            {
                SWF2Raster pngConv = new SWF2Raster(swf);
                pngData = pngConv.GetPNGAsBytes();
            }

            foreach (XPathNavigator pngout in Xml.SelectChildren(swfNav, @"pngout"))
            {
                string pngoutStore = pngout.GetAttribute(@"store", string.Empty);

                if (pngoutStore == string.Empty)
                {
                    throw new SwiffotronException(
                            SwiffotronError.BadInputXML,
                            this.Context,
                            @"The pngout tag needs either a cachekey or a storeput attribute");
                }

                this.SaveToStore(pngoutStore, pngData);
            }

            if (svgOuts > 0)
            {
                SWF2SVG svgConv = new SWF2SVG(swf);
                svgData = svgConv.GetSVGAsBytes();
            }

            foreach (XPathNavigator svgout in Xml.SelectChildren(swfNav, @"svgout"))
            {
                string svgoutStore = svgout.GetAttribute(@"store", string.Empty);

                if (svgoutStore == string.Empty)
                {
                    throw new SwiffotronException(
                            SwiffotronError.BadInputXML,
                            this.Context,
                            @"The svgout tag needs either a cachekey or a storeput attribute");
                }

                this.SaveToStore(svgoutStore, svgData);
            }

            if (vidOuts > 0)
            {
                /* ISSUE 66 */
                throw new SwiffotronException(
                        SwiffotronError.UnimplementedFeature,
                        this.Context,
                        "We can't do video output yet.");
            }
        }

        /// <summary>
        /// Takes a node, works out its dependencies and adds them to the
        /// toBeProcessed list.
        /// </summary>
        /// <param name="deps">The dependency map entry for a node.</param>
        private void AddDependencies(DependencyList deps)
        {
            /* This is the node we will work out the dependencies for. */
            XPathNavigator node = deps.Node;

            foreach (XPathNavigator depID in Xml.Select(node, @"swf:movieclip|swf:instance"))
            {
                /* ISSUE 64: Work out if this is a movieclip tag. If it's cached, and in the local cache, then
                 * we don't need to add it's dependent SWF. */

                XPathNavigator swfNode = Xml.SwfTagFromRef(depID);

                if (swfNode != null)
                {
                    deps.AddDependency(swfNode); /* This only adds if it's not already in the list. */
                    if (!this.IsInDependenciesMap(swfNode))
                    {
                        this.dependencyMap.Add(new DependencyList(swfNode, null));
                    }
                }
            }
        }

        /// <summary>
        /// Checks an XPathNavigator object to see if it's already in the dependency
        /// map. Contains won't work with XPathNavigator objects that point
        /// to the same node, you see.
        /// </summary>
        /// <param name="node">The node to test.</param>
        /// <returns>true if it's on the list.</returns>
        private bool IsInDependenciesMap(XPathNavigator node)
        {
            foreach (DependencyList next in this.dependencyMap)
            {
                if (node.ComparePosition(next.Node) == XmlNodeOrder.Same)
                {
                    return true;
                }
            }

            return false;
        }

#if(DEBUG)
        public void LoadSwiffotronXML_accesor(Stream swiffotronXml)
        {
            Xml.LoadSwiffotronXML(swiffotronXml);
        }
#endif

        /// <summary>
        /// Get some useful information for debug purposes letting us find out how things
        /// are set up. I should list them all here, really.
        /// </summary>
        /// <returns>A big map of arbitrary string->string data that you can pick apart and
        /// use as you so desire.</returns>
        public Dictionary<string, string> Interrogate()
        {
            Dictionary<string, string> info = new Dictionary<string, string>();

            conf.Interrogate(info);

            return info;
        }
    }
}
