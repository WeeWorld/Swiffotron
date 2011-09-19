//-----------------------------------------------------------------------
// Swiffotron.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Remoting;
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
    using SWFProcessing.Swiffotron.IO;
    using SWFProcessing.Swiffotron.IO.Debug;
    using SWFProcessing.Swiffotron.Processor;

    /// <summary>
    /// <para>
    /// Main entry point to the swiffotron's functionality. This is the main
    /// object you need to create to start building SWF files.</para>
    /// <para>
    /// TODO: This class needs refactored into smaller lumps. This might mean
    /// lumps such as xml wrapping services, dependency management and
    /// SWF generation.</para>
    /// </summary>
    public class Swiffotron
    {
        /// <summary>The configuration XML file namespace</summary>
        private const string ConfigNS = @"urn:swiffotron-schemas:swiffotron-config/24/05/2011";

        /// <summary>The swiffotron XML file namespace</summary>
        private const string SwiffotronNS = @"urn:swiffotron-schemas:swiffotron-job/24/05/2011";

        /* Tag attributes: */

        /// <summary>XML attribute; store reference</summary>
        private const string AttrStore = @"store";

        /// <summary>XML attribute; Some cache key value</summary>
        private const string AttrCachekey = @"cachekey";

        /// <summary>XML attribute; Width pixel value</summary>
        private const string AttrWidth = @"width";

        /// <summary>XML attribute; Height pixel value</summary>
        private const string AttrHeight = @"height";

        /// <summary>XML attribute; X pixel position</summary>
        private const string AttrX = @"x";

        /// <summary>XML attribute; Y pixel position</summary>
        private const string AttrY = @"y";

        /// <summary>XML attribute; Background colour</summary>
        private const string AttrBGColor = @"bgcolor";

        /// <summary>XML attribute; Base SWF ref</summary>
        private const string AttrBase = @"base";

        /// <summary>XML attribute; Qualified name</summary>
        private const string AttrQName = @"qname";

        /// <summary>XML attribute; Copy from reference</summary>
        private const string AttrFrom = @"from";

        /// <summary>XML attribute; Copy to reference</summary>
        private const string AttrTo = @"to";

        /// <summary>XML attribute; Rotate value</summary>
        private const string AttrRotate = @"rotate";

        /// <summary>XML attribute; X-axis scale factor</summary>
        private const string AttrScaleX = @"scalex";

        /// <summary>XML attribute; Y-axis scale factor</summary>
        private const string AttrScaleY = @"scaley";

        /// <summary>XML attribute; Type of reference</summary>
        private const string AttrType = @"type";

        /// <summary>XML attribute; Path to something. A qname or a class. May be * for all.</summary>
        private const string AttrPath = @"path";

        /// <summary>XML attribute; Source reference</summary>
        private const string AttrSrc = @"src";

        /// <summary>XML attribute; Class name</summary>
        private const string AttrClass = @"class";

        /// <summary>XML attribute; SWF reference</summary>
        private const string AttrSwf = @"swf";

        /// <summary>XML attribute; Relative to reference</summary>
        private const string AttrRelativeTo = @"relativeTo";

        /// <summary>XML attribute; Element ID reference</summary>
        private const string AttrID = @"id";

        /* Tag names: */

        /// <summary>XML tag name; A SWF declaration</summary>
        private const string TagSwf = @"swf";

        /// <summary>XML tag name; A SWF output declaration</summary>
        private const string TagSwfOut = @"swfout";

        /// <summary>XML tag name; A PNG image output declaration</summary>
        private const string TagPngOut = @"pngout";

        /// <summary>XML tag name; A video output declaration</summary>
        private const string TagVidOut = @"vidout";

        /// <summary>XML tag name; An instruction to modify a clip instance</summary>
        private const string TagModify = @"modify";

        /// <summary>XML tag name; A text search and replace operation</summary>
        private const string TagTextReplace = @"textreplace";

        /// <summary>XML tag name; An instance declaration</summary>
        private const string TagInstance = @"instance";

        /// <summary>XML tag name; A movie clip declaration</summary>
        private const string TagMovieClip = @"movieclip";

        /// <summary>XML tag name; Relative move instruction</summary>
        private const string TagMoveRel = @"moveRel";

        /// <summary>XML tag name; Absolute move instruction</summary>
        private const string TagMoveAbs = @"moveAbs";

        /// <summary>XML tag name; Absolute position</summary>
        private const string TagAbsolute = @"absolute";

        /// <summary>XML tag name; Relative position</summary>
        private const string TagRelative = @"relative";

        /// <summary>XML tag name; Remove an instance from a SWF</summary>
        private const string TagRemove = @"remove";

        /* Tag attribute values: */

        /// <summary>XML value; SWF type</summary>
        private const string ValSwf = @"swf";

        /// <summary>XML value; Movie clip type</summary>
        private const string ValMovieClip = @"movieclip";

        /// <summary>XML value; Actionscript type</summary>
        private const string ValActionscript = @"actionscript";

        /// <summary>XML value; Instance type</summary>
        private const string ValInstance = @"instance";

        /// <summary>XML value; External SWF type</summary>
        private const string ValExtern = @"extern";

        /// <summary>XML parsing settings for reading the config file.</summary>
        private XmlReaderSettings configReaderSettings;

        /// <summary>XML parsing settings for reading swiffotron job files.</summary>
        private XmlReaderSettings swiffotronReaderSettings;

        /// <summary>A map of cache names to cache implementation instances.</summary>
        private Dictionary<string, ISwiffotronCache> caches;

        /// <summary>A map of store names to store implementation instances.</summary>
        private Dictionary<string, ISwiffotronStore> stores;

#if(DEBUG)
        /* We could use MSTest's private access, but we want the express versions
         * to be in on the fun too. Horrible, but there you go. */
        public Dictionary<string, ISwiffotronStore> stores_accessor { get { return stores; } }

        public Dictionary<string, ISwiffotronCache> caches_accessor { get { return caches; } }
#endif

        /* TODO: Would be good to also store a 'processedSWFsRenderedAsMovieClips' so that
         * we don't keep converting the same clip over and over again when it's re-used.
         * Mental note: pretty sure SWF objects are being subtley modified in annoying ways,
         * when cache copies should be pristine.
         * Also.. unit tests for this stuff. 
         * Further note. This appears to replicate the functionality of the SWF.dictionary
         * object. These two things need reviewed, consolidated, or just to have their heads
         * banged together. */

        /// <summary>A map of SWF models that have been processed already.</summary>
        private Dictionary<string, SWF> processedSWFs;

        /// <summary>The namespace manager for the current job XML</summary>
        private XmlNamespaceManager namespaceMgr;

        /// <summary>An XPath navigator that points to the root of the current XML file</summary>
        private XPathNavigator root;

        /// <summary>Settings to pass to the SWF parser</summary>
        private SWFReaderOptions swfReaderOptions;

        /// <summary>Settings to pass to the SWF writer</summary>
        private SWFWriterOptions swfWriterOptions;

        /// <summary>
        /// Prevents Swiffotron saving files to the store. Why would you do this? Well you
        /// might prefer to get the data from a commit listener (Which will still be enabled)
        /// and squirt it out over a network or something instead.
        /// </summary>
        public bool EnableStoreWrites { get; set; }

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

        /// <summary>
        /// Context for enriching debug log output
        /// </summary>
        public SwiffotronContext Context { get; set; }

        /// <summary>
        /// Initializes a new instance of the Swiffotron class. It will be created with
        /// default configuration options.
        /// </summary>
        public Swiffotron() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Swiffotron class.
        /// </summary>
        /// <param name="configStream">An open stream to the config XML data, or null to
        /// use default configuration.</param>
        public Swiffotron(Stream configStream)
        {
            configReaderSettings = CreateValidationSettings(@"swiffotron-config.xsd");
            swiffotronReaderSettings = CreateValidationSettings(@"swiffotron.xsd");

            caches = new Dictionary<string, ISwiffotronCache>();
            stores = new Dictionary<string, ISwiffotronStore>();

            if (configStream == null)
            {
                configStream = Assembly
                        .GetExecutingAssembly()
                        .GetManifestResourceStream(@"SWFProcessing.Swiffotron.res.default-config.xml");
            }

            this.LoadConfigXML(configStream);
        }

        /// <summary>
        /// Process a job XML file.
        /// </summary>
        /// <param name="xml">An open stream to the XML data.</param>
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
        /// <param name="commitStore">If not null, this will hold all store commits made
        /// by this job.</param>
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
            this.root = this.LoadSwiffotronXML(xml);
            string jobID = this.root.SelectSingleNode("swf:swiffotron/@id", this.namespaceMgr).ToString();
            this.Context = new SwiffotronContext(jobID);

            this.processingList = new List<XPathNavigator>();
            this.dependencyMap = new List<DependencyList>();

            this.processedSWFs = new Dictionary<string, SWF>();

            this.localCache = new Dictionary<string, object>();

            /* TODO: If the root node has an ID, then replace this.Context with one that has a more
             * meaningful name. */

            /* Take local copies of all referenced cache objects to guard against
             * them being ejected before we access them, since we work out what we
             * need to do based on what's in the cache before we do it. */
            foreach (XPathNavigator keyNode in this.root.Select(@"//@cachekey", this.namespaceMgr))
            {
                string key = keyNode.ToString();
                object data = this.GetCacheItem(key);
                if (data != null)
                {
                    this.localCache.Add(key, data);
                }
            }

            /* Select all the swf tags that have some sort of output: */
            foreach (XPathNavigator outputTag in this.root.Select(@"//swf:swfout|//swf:pngout|//swf:vidout", this.namespaceMgr))
            {
                XmlAttributeCollection attribs = ((XmlElement)outputTag.UnderlyingObject).Attributes;

                /* TODO: Check the runifnotchanged thing. */

                string dest = outputTag.GetAttribute(AttrStore, string.Empty);
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
                            SwiffotronError.BadInputXML, this.Context,
                            @"A circular dependency was detected.");
                }

                this.dependencyMap = newMap;
            }

            /* And so, after all that, we can begin: */
            this.GenerateSWFs(writeLog, abcWriteLog);
        }

        /// <summary>
        /// For a given schema name (Named resource) this loads the schema XML and
        /// creates an XmlReaderSettings object which can be used to validate any
        /// XML read by the Swiffotron. This is called in the static initialiser.
        /// </summary>
        /// <param name="schemaName">Named resource which is an XSD file.</param>
        /// <returns>Some validation settings useful to an XmlReader.</returns>
        private XmlReaderSettings CreateValidationSettings(string schemaName)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;

            Stream xsd = Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream(@"SWFProcessing.Swiffotron.res." + schemaName);
            settings.Schemas.Add(null, XmlReader.Create(xsd));

            return settings;
        }

        /// <summary>
        /// Gets an integer attribute value from an XML node
        /// </summary>
        /// <param name="nav">A pointer to the node</param>
        /// <param name="name">The attribute to get</param>
        /// <returns>An integer, or null if not present.</returns>
        private int? IntegerAttribute(XPathNavigator nav, string name)
        {
            /* TODO: Why didn't I use ValueAsInt here? Check the behaviour with
             * ropy values. Maybe that was it...*/

            string stringVal = nav.GetAttribute(name, string.Empty);
            if (stringVal != string.Empty)
            {
                return Convert.ToInt32(stringVal);
            }

            return null;
        }

        /// <summary>
        /// Gets an HTML colour attribute value from an XML node
        /// </summary>
        /// <param name="nav">A pointer to the node</param>
        /// <param name="name">The attribute to get</param>
        /// <returns>A colour, or null if not present.</returns>
        private Color? ColorAttribute(XPathNavigator nav, string name)
        {
            string stringVal = nav.GetAttribute(name, string.Empty);
            if (stringVal != string.Empty)
            {
                return ColorTranslator.FromHtml(stringVal);
            }

            return null;
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
                        swfTag.GetAttribute(AttrBase, string.Empty),
                        this.IntegerAttribute(swfTag, AttrWidth),
                        this.IntegerAttribute(swfTag, AttrHeight),
                        this.ColorAttribute(swfTag, AttrBGColor),
                        swfTag.GetAttribute(AttrCachekey, string.Empty),
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
            using (Stream s = this.OpenFromStore(path))
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

                    this.readLogHandler.OnSwiffotronReadSWF(name, sb.ToString());
                    return swf;
                }

                return new SWFReader(
                        s,
                        new SWFReaderOptions() { StrictTagLength = true },
                        null,
                        this.abcInterceptor)
                    .ReadSWF(new SWFContext(name));
#else
                return new SWFReader(s, new SWFReaderOptions() { StrictTagLength = true }, null, null).ReadSWF(name);
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
                swf = new SWF(new SWFContext(swfTag.GetAttribute(AttrID, string.Empty)), true);

                /* TODO: If this SWF has no output, then perhaps we can create it as a movieclip
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
                this.MoveToFirstChildElement(nav);

                do
                {
                    switch (nav.LocalName)
                    {
                        case TagModify:
                            this.ModifyInstance(nav, swf);
                            break;

                        case TagTextReplace:
                            this.TextReplace(nav, swf);
                            break;

                        case TagInstance:
                            this.CreateInstance(nav, swfTag, swf);
                            break;

                        case TagRemove:
                            this.RemoveInstance(nav, swf);
                            break;

                        case TagMovieClip:
                            this.CreateMovieClip(nav, swfTag, swf);
                            break;

                        case TagSwfOut:
                        case TagPngOut:
                        case TagVidOut:
                            /* These are not processing steps. Skip 'em. */
                            break;

                        default:
                            throw new SwiffotronException(
                                    SwiffotronError.UnimplementedFeature, this.Context,
                                    @"Unsupported tag: " + nav.LocalName);
                    }
                }
                while (nav.MoveToNext(XPathNodeType.Element));
            }

            this.processedSWFs.Add(swfTag.GetAttribute(AttrID, string.Empty), swf);

            this.WriteSWFOutput(swfTag, swf, writeLog, abcWriteLog);
        }

        /// <summary>
        /// Processes a text replacement tag
        /// </summary>
        /// <param name="nav">The text replacement node.</param>
        /// <param name="swf">The SWF being processed.</param>
        private void TextReplace(XPathNavigator nav, SWF swf)
        {
            string find = nav.SelectSingleNode(@"swf:find/text()", this.namespaceMgr).Value;
            string replace = nav.SelectSingleNode(@"swf:replace/text()", this.namespaceMgr).Value;

            foreach (XPathNavigator loc in nav.SelectChildren(@"location", SwiffotronNS))
            {
                string type = loc.GetAttribute(AttrType, string.Empty);
                switch (type)
                {
                    case ValActionscript:
                        swf.TextReplaceInCode(find, replace);
                        break;

                    case ValMovieClip:
                        string path = loc.GetAttribute(AttrPath, string.Empty);
                        Timeline[] clips = new Timeline[] { swf };
                        if (path != "*")
                        {
                            clips = SpritesFromQname(path, swf, true);
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
            string qname = nav.GetAttribute(AttrQName, string.Empty);

            string uname;
            Timeline parent = QNameToTimeline(qname, swf, out uname);

            if (!parent.RemoveInstance(uname))
            {
                /* TODO: Unit test for this please. Also for non-existant parent names in the qname. */
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
            string type = insTag.GetAttribute(AttrType, string.Empty);
            string src = insTag.GetAttribute(AttrSrc, string.Empty);
            string className = insTag.GetAttribute(AttrClass, string.Empty);

            /* TODO: If the instance name (id) is the same as the class name, it can
             * cause problems in files generated and decompiled again in sothink. We should
             * probably detect this and warn against it. */

            switch (type)
            {
                case ValSwf:
                    if (className == string.Empty)
                    {
                        throw new SwiffotronException(
                                SwiffotronError.BadInputXML, this.Context,
                                "An instance created from a SWF needs a class name to be defined.");
                    }

                    if (src.StartsWith("store://"))
                    {
                        throw new SwiffotronException(SwiffotronError.BadPathOrID, this.Context, "Unexpected store path. Did you mean for the type to be extern, perhaps?");
                    }

                    if (!this.processedSWFs.ContainsKey(src))
                    {
                        throw new SwiffotronException(
                                SwiffotronError.Internal, this.Context,
                                "Internal error. SWF tags were processed out of order (" + currentSwfTag.GetAttribute(AttrID, string.Empty) + " requres " + src + ").");
                    }

                    SWF processedSWF = this.processedSWFs[src];

                    this.CreateInstanceFromSWF(insTag, swf, className, processedSWF);
                    break;

                case ValMovieClip:
                    Sprite clip = swf.GetCharacter(src) as Sprite;
                    if (clip == null)
                    {
                        throw new SwiffotronException(SwiffotronError.BadInputXML, this.Context, "MovieClip not defined: " + src);
                    }

                    this.CreateInstanceIn(
                            insTag.GetAttribute(AttrID, string.Empty),
                            swf,
                            insTag,
                            clip,
                            className);

                    clip.SpriteProc(delegate(Sprite s)
                    {
                        if (s.Class != null && !(s.Class is AdobeClass))
                        {
                            /* TODO: Only do this if the class hasn't already been bound.
                             * Note that this will be a problem if one movieclip is used to create
                             * several instances. At the time of writing, there is no unit test for
                             * this case. */
                            swf.FirstScript.Code.GenerateClipClassBindingScript(s);
                        }
                    });

                    break;

                case ValInstance:
                    if (className != string.Empty)
                    {
                        throw new SwiffotronException(SwiffotronError.BadInputXML,
                                this.Context.Sentinel("ClassNameInClonedInstance"),
                                "An instance cannot be given a new classname if it is a clone of an existing instance ("+className+")");
                    }

                    Sprite srcSprite = this.SpritesFromQname(src, swf, false)[0];
                    if (!srcSprite.HasClass)
                    {
                        srcSprite.Class = AdobeClass.CreateFlashDisplayMovieClip(srcSprite.Root.FirstScript.Code);
                        className = srcSprite.Class.QualifiedName;
                    }

                    this.CreateInstanceIn(
                            insTag.GetAttribute(AttrID, string.Empty),
                            swf,
                            insTag,
                            srcSprite,
                            className);
                    break;

                case ValExtern:
                    SWF importSwf = this.SwfFromStore(src);

                    this.CreateInstanceFromSWF(insTag, swf, className, importSwf);

                    break;

                default:
                    throw new SwiffotronException(
                            SwiffotronError.UnimplementedFeature, this.Context,
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

            bool isAdobeClassname = (className.StartsWith("flash.")
                || (className.StartsWith("fl."))
                || (className.StartsWith("adobe."))
                || (className.StartsWith("air."))
                || (className.StartsWith("flashx.")));

            if (isAdobeClassname && importSwf.HasClass)
            {
                /* Can't rename a class to an Adobe class name. That's bonkers. */
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML, this.Context.Sentinel("InstanceClassNameInappropriate"),
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
                            SwiffotronError.BadInputXML, this.Context.Sentinel("MainTimelineInstanceNotRenamed"),
                            "An external instance with timeline code must be explicitely renamed with the instance tag's class attribute.");
                }
            }

            if (className == "flash.display.MovieClip")
            {
                Sprite spr = new Sprite(importSwf, swf);

                this.CreateInstanceIn(
                        insTag.GetAttribute(AttrID, string.Empty),
                        swf,
                        insTag,
                        spr,
                        className);
            }
            else
            {
                Sprite spr = new Sprite(importSwf, swf, className);

                this.CreateInstanceIn(
                        insTag.GetAttribute(AttrID, string.Empty),
                        swf,
                        insTag,
                        spr,
                        className);

                spr.SpriteProc(delegate(Sprite s)
                {
                    if (s.Class != null && !(s.Class is AdobeClass))
                    {
                        /* TODO: Only do this if the class hasn't already been bound. */
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
            string type = movieClipTag.GetAttribute(AttrType, string.Empty);
            string src = movieClipTag.GetAttribute(AttrSrc, string.Empty);
            string className = movieClipTag.GetAttribute(AttrClass, string.Empty);
            string swfTag = movieClipTag.GetAttribute(AttrSwf, string.Empty);

            /*
             * TODO: The examples in unit tests all put instances in the same package, e.g.
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
                /* TODO: If swf attribute is present, find the swf. It should be loaded/generated. It should not be the current swf. */
                throw new SwiffotronException(
                        SwiffotronError.UnimplementedFeature, this.Context,
                        "swf attribute is not supported in movieclip tags yet.");
            }

            switch (type)
            {
                case ValSwf:
                    if (fromOtherSwf)
                    {
                        throw new SwiffotronException(
                                SwiffotronError.BadInputXML, this.Context,
                                "The movieclip 'swf' attribute does not make sense when the type is set to 'swf'.");
                    }

                    if (!this.processedSWFs.ContainsKey(src))
                    {
                        throw new SwiffotronException(
                                SwiffotronError.Internal, this.Context,
                                "Internal error. SWF tags were processed out of order (" + currentSwfTag.GetAttribute(AttrID, string.Empty) + " requres " + src + ").");
                    }

                    if (className == string.Empty)
                    {
                        throw new SwiffotronException(
                                SwiffotronError.BadInputXML, this.Context,
                                "An clip created from a SWF needs a class name to be defined.");
                    }

                    SWF importSwf = this.processedSWFs[src];

                    /* TODO: There's a TODO up there next to the declaration of processedSWFs which is intended
                     * to cache the new Sprite() bit below. */
                    swf.AddCharacter(movieClipTag.GetAttribute(AttrID, string.Empty), new Sprite(importSwf, swf, className));

                    /* TODO: The above new Sprite will merge the sprite's code into the SWF regardless of whether
                     * it's instantiated. This should be done based on the exportforscript flag, or if the sprite
                     * is instantiated. Mind you, there is an argument that says if you declare a movieclip and
                     * never use it, and don't mark if for export, then you should probably remove it. Perhaps
                     * a more elegant solution would be to verify this in the XML and give a warning that you're
                     * doing redundant things. */

                    break;

                case ValExtern:
                    if (fromOtherSwf)
                    {
                        throw new SwiffotronException(
                                SwiffotronError.BadInputXML, this.Context,
                                "The movieclip 'swf' attribute does not make sense when the type is set to 'extern'. Try declaring the external SWF in its own tag instead, and reference it by ID.");
                    }

                    if (className == string.Empty)
                    {
                        throw new SwiffotronException(
                                SwiffotronError.BadInputXML, this.Context,
                                "An clip created from a SWF needs a class name to be defined.");
                    }

                    SWF forImport = this.SwfFromStore(src);
                    try
                    {
                        swf.AddCharacter(movieClipTag.GetAttribute(AttrID, string.Empty), new Sprite(forImport, swf, className));
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
                    throw new SwiffotronException(
                            SwiffotronError.UnimplementedFeature, this.Context,
                            "Bad instance type: " + type);
            }
        }

        /// <summary>
        /// Find a list of characters that match a qname pattern. If nothing is found, an
        /// exception is thrown.
        /// </summary>
        /// <param name="qname">The qname to find.</param>
        /// <param name="swf">The SWF to search.</param>
        /// <param name="patternPermitted">If this is false, the returned array will have 1 element.</param>
        /// <returns>An array of characters matching the qname or qname pattern.</returns>
        private Sprite[] SpritesFromQname(string qname, SWF swf, bool patternPermitted)
        {
            /* TODO: If qname is a pattern, we should return more than one character. */
            /* TODO: If qname is a pattern, and patternPermitted is false, throw a wobbler. */

            PlaceObject po = swf.LookupInstance(qname);

            /* TODO: There is a question of whether to error if the instance is not found. Some are
             * found with a pattern rather than a path, and you may not expect it to always find something. 
             * At the moment, we shall throw an exception, because it suits our development, unit testing
             * fail-fast strictness. */
            if (po == null)
            {
                throw new SwiffotronException(
                        SwiffotronError.BadPathOrID, this.Context.Sentinel("FindSpriteByQName"),
                        @"Instance not found: " + qname);
            }

            Sprite sprite = po.Character as Sprite;

            if (sprite == null)
            {
                throw new SwiffotronException(
                        SwiffotronError.BadPathOrID, this.Context,
                        @"Instance does not point to sprite: " + qname);
            }

            return new Sprite[] { sprite };
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
            Timeline parent = QNameToTimeline(qname, swf, out newInsName);

            /* TODO: Position relative to */
            string relativeToQname = transform.GetAttribute(AttrRelativeTo, string.Empty);
            Matrix m = null;
            if (relativeToQname == null || relativeToQname == string.Empty)
            {
                m = this.TransformTagToMatrix(transform);
                if (m.IsSimpleTranslate)
                {
                    m.TransX = m.TransX;
                    m.TransY = m.TransY;
                }
            }
            else
            {
                m = this.PositionFromQname(relativeToQname, swf);
                Matrix rel = this.TransformTagToMatrix(transform);
                if (rel.IsSimpleTranslate)
                {
                    m.Translate(rel.TransX, rel.TransY);
                }
                else
                {
                    m.Apply(rel);
                }
            }

            /* TODO: Find out what 'ratio' does. It's a magical number that makes things work, and
             * I never use it. */
            try
            {
                parent.Instantiate(1, charToInstantiate, Layer.Position.Front, m, newInsName, qClassName);
            }
            catch (SWFModellerException sme)
            {
                throw new SwiffotronException(SwiffotronError.BadInputXML, this.Context.Sentinel("CreateInstanceIn"),
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
                            SwiffotronError.BadPathOrID, this.Context,
                            "Ancestor of '" + uname + "', i.e. '" + parentQname + "' does not exist.");
                }

                ICharacter parentChar = parentIns.Character;

                if (!(parentChar is Timeline))
                {
                    throw new SwiffotronException(
                            SwiffotronError.BadPathOrID, this.Context,
                            "QName '" + parentQname + "' does not refer to a timeline.");
                }

                return (Timeline)parentChar;
            }
        }

        /// <summary>
        /// Gets the transorm position of an instance.
        /// </summary>
        /// <param name="qname">Fully qualified name of an instance.</param>
        /// <param name="swf">The SWF to search in/</param>
        /// <returns>A copy of the instance's position matrix.</returns>
        private Matrix PositionFromQname(string qname, SWF swf)
        {
            /* TODO: Detect paths here, and error, coz they're not valid. */
            PlaceObject po = swf.LookupInstance(qname);
            return po.Matrix.Copy();
        }

        /// <summary>
        /// Convenience method to move a navigator to the first child element, rather
        /// than first child node, which could be text or something.
        /// </summary>
        /// <param name="nav">The navigator to move</param>
        private void MoveToFirstChildElement(XPathNavigator nav)
        {
            nav.MoveToFirstChild();

            if (nav.NodeType == XPathNodeType.Element)
            {
                return;
            }

            nav.MoveToNext(XPathNodeType.Element); /* Bit of an assumption, but never mind. */
        }

        /// <summary>
        /// Executes a modify tag on a SWF
        /// </summary>
        /// <param name="modify">A navigator pointing to the modify element in the XML</param>
        /// <param name="swf">The SWF to modify</param>
        private void ModifyInstance(XPathNavigator modify, SWF swf)
        {
            string qname = modify.GetAttribute(AttrQName, string.Empty);

            PlaceObject po = swf.LookupInstance(qname);

            /* TODO: There is a question of whether to error if the instance is not found. Some are
             * found with a pattern rather than a path, and you may not expect it to always find something. 
             * At the moment, we shall throw an exception, because it suits our development, unit testing
             * fail-fast strictness. */
            if (po == null)
            {
                throw new SwiffotronException(
                        SwiffotronError.BadPathOrID, this.Context.Sentinel("ModifyInstance"),
                        @"Instance not found: " + qname);
            }

            this.MoveToFirstChildElement(modify);

            do
            {
                if (modify.NodeType == XPathNodeType.Element)
                {
                    switch (modify.LocalName)
                    {
                        case TagMoveRel:
                            Matrix rel = this.TransformTagToMatrix(modify);
                            po.Matrix.Apply(rel);
                            break;

                        case TagMoveAbs:
                            Matrix abs = this.TransformTagToMatrix(modify);
                            po.Matrix = abs;
                            break;

                        default:
                            throw new SwiffotronException(
                                    SwiffotronError.UnimplementedFeature, this.Context,
                                    @"Unsupported modification tag: " + modify.LocalName);
                    }
                }
            }
            while (modify.MoveToNext(XPathNodeType.Element));

            modify.MoveToParent();

            /* TODO: finish */
        }

        /// <summary>
        /// Creates a new position matrix from an XML declaration of one.
        /// </summary>
        /// <param name="transform">The navigator pointing to the XML transform element.</param>
        /// <returns>A new Matrix</returns>
        private Matrix TransformTagToMatrix(XPathNavigator transform)
        {
            Matrix m = new Matrix();
            m.TransX = (float)XmlConvert.ToDouble(transform.GetAttribute(AttrX, string.Empty));
            m.TransY = (float)XmlConvert.ToDouble(transform.GetAttribute(AttrY, string.Empty));

            string rot = transform.GetAttribute(AttrRotate, string.Empty);
            if (rot != string.Empty)
            {
                m.RotateToDegrees(XmlConvert.ToDouble(rot));
            }

            string scaleX = transform.GetAttribute(AttrScaleX, string.Empty);
            string scaleY = transform.GetAttribute(AttrScaleY, string.Empty);

            float sx = 1.0f;
            float sy = 1.0f;

            if (scaleX == string.Empty)
            {
                if (scaleY == string.Empty)
                {
                    return m;
                }

                sy = (float)XmlConvert.ToDouble(scaleY);
            }
            else
            {
                sx = (float)XmlConvert.ToDouble(scaleX);
                if (scaleY != string.Empty)
                {
                    sy = (float)XmlConvert.ToDouble(scaleY);
                }
            }

            m.ScaleNoTranslate(sx, sy);

            return m;
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
        private void WriteSWFOutput(XPathNavigator swfNav, SWF swf, StringBuilder writeLog, StringBuilder abcWriteLog)
        {
            byte[] swfData = null;
            foreach (XPathNavigator swfout in swfNav.SelectChildren(@"swfout", SwiffotronNS))
            {
                string swfoutStore = swfout.GetAttribute(@"store", string.Empty);

                if (swfoutStore == string.Empty)
                {
                    throw new SwiffotronException(
                            SwiffotronError.BadInputXML, this.Context,
                            @"The swfout tag needs either a cachekey or a storeput attribute");
                }

                if (swfData == null)
                {
                    swfData = new SWFWriter(swf, this.swfWriterOptions, writeLog, abcWriteLog).ToByteArray();
                }

                this.WriteToOutput(swfoutStore, swfData);
            }

            /* TODO: These should be in another method (And also work): */
            if (swfNav.SelectChildren(@"pngout", SwiffotronNS).Count > 0)
            {
                throw new SwiffotronException(SwiffotronError.UnimplementedFeature, this.Context,
                        "We can't do PNG output yet.");
            }

            if (swfNav.SelectChildren(@"vidout", SwiffotronNS).Count > 0)
            {
                throw new SwiffotronException(SwiffotronError.UnimplementedFeature, this.Context,
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

            foreach (XPathNavigator depID in node.Select(@"swf:movieclip|swf:instance", this.namespaceMgr))
            {
                /* TODO: Work out if this is a movieclip tag. If it's cached, and in the local cache, then
                 * we don't need to add it's dependent SWF. */

                XPathNavigator swfNode = this.SwfTagFromRef(depID);

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
        /// Find a swf xml node by ID
        /// </summary>
        /// <param name="id">The ID of the SWF node</param>
        /// <param name="root">The root of the XML file.</param>
        /// <param name="exclude">If the id matches the exclude id, it throws an exception.</param>
        /// <returns>The node. Throws an exception if one isn't found.</returns>
        private XPathNavigator SwfTagById(string id, XPathNavigator root, XPathNavigator exclude)
        {
            XPathNavigator swfTag = root.SelectSingleNode(@"/swf:swiffotron/swf:swf[@id='" + id + "']", this.namespaceMgr);
            if (swfTag == null)
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML, this.Context,
                        "Invalid swf tag ID: " + id);
            }

            if (exclude != null && swfTag.ComparePosition(exclude) == XmlNodeOrder.Same)
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML, this.Context,
                        "swf tag with ID '" + id + "' refers to itself.");
            }

            return swfTag;
        }

        /// <summary>
        /// Find a swf xml node by a reference
        /// </summary>
        /// <param name="referee">A movieclip, or instance tag</param>
        /// <returns>The SWF tag that it references, or null if it doesn't reference one.</returns>
        private XPathNavigator SwfTagFromRef(XPathNavigator referee)
        {
            string type = referee.GetAttribute(AttrType, string.Empty);

            if (type == ValSwf)
            {
                string src = referee.GetAttribute(AttrSrc, string.Empty);

                if (src.StartsWith("store://"))
                {
                    return null;
                }

                /* This tag declares the swf id directly in its src attribute: */
                XPathNavigator referenced = this.root.SelectSingleNode(@"/swf:swiffotron/swf:swf[@id='" + src + "']", this.namespaceMgr);
                if (referenced == null)
                {
                    throw new SwiffotronException(SwiffotronError.BadPathOrID,
                            this.Context.Sentinel("SrcSwfBadref"),
                            "No such swf element: " + src);
                }
                return referenced;
            }

            if (type == ValMovieClip)
            {
                string src = referee.GetAttribute(AttrSrc, string.Empty);

                if (src.StartsWith("store://"))
                {
                    return null;
                }

                /* This tag declares the swf id directly in its src attribute: */
                XPathNavigator referenced = this.root.SelectSingleNode(@"/swf:swiffotron/swf:swf/swf:movieclip[@id='" + src + "']", this.namespaceMgr);
                if (referenced == null)
                {
                    throw new SwiffotronException(SwiffotronError.BadPathOrID,
                            this.Context.Sentinel("InstanceSrcMovieClipBadref"),
                            "No such movieclip element: " + src);
                }
                return referenced;
            }

            string swf = referee.GetAttribute(AttrSwf, string.Empty);
            if (swf != null && swf != string.Empty)
            {
                /* This tag declares the swf id in its swf attribute: */
                return this.root.SelectSingleNode(@"/swf:swiffotron/swf:swf[@id='" + swf + "']", this.namespaceMgr);
            }

            return null;
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

        /// <summary>
        /// Convenience method for debugging.
        /// </summary>
        /// <param name="node">A node to describe in nice, neat text.</param>
        /// <param name="desc">A string that hopefully describes the node in a way that you
        /// can identify it in the source XML from this string in some console
        /// output.</param>
        [Conditional("DEBUG")]
        private void DescribeNode(XPathNavigator node, ref string desc)
        {
            if (node.LocalName == TagSwf)
            {
                desc = "swf ";

                string baseSwf = node.GetAttribute(AttrBase, string.Empty);
                if (baseSwf != string.Empty)
                {
                    desc += ", base:" + baseSwf;
                }

                string width = node.GetAttribute(AttrWidth, string.Empty);
                if (width != string.Empty)
                {
                    desc += ", width:" + width;
                }

                string height = node.GetAttribute(AttrHeight, string.Empty);
                if (height != string.Empty)
                {
                    desc += ", height:" + height;
                }
            }

            /* For all other nodes: */
            desc = node.LocalName;
        }

        /// <summary>
        /// Writes a block of data to the store specified in the fully qualified
        /// store key of the form [store name]:[store key]
        /// </summary>
        /// <param name="key">The store key</param>
        /// <param name="data">The data to store as a byte array.</param>
        private void WriteToOutput(string key, byte[] data)
        {
            if (this.EnableStoreWrites)
            {
                Uri storeURI = new Uri(key);

                if (storeURI.Scheme != "store") /* TODO: Constants, please. */
                {
                    throw new SwiffotronException(
                            SwiffotronError.BadInputXML, this.Context,
                            @"Store paths should begin with store://");
                }

                string storeId = storeURI.Host;

                if (!stores.ContainsKey(storeId))
                {
                    throw new SwiffotronException(
                            SwiffotronError.BadInputXML, this.Context,
                            @"Store '" + storeId + @"' not registered.");
                }

                ISwiffotronStore store = stores[storeId];

                key = storeURI.AbsolutePath.Substring(1);

                using (Stream s = store.OpenOutput(key))
                {
                    s.Write(data, 0, data.Length);
                }

                store.Commit(key);
            }

            /* An interesting upshot here is that if you know you're never
             * going to save files back to a store, then you can get away with
             * using any string you like in swfout store keys. */

            if (this.commitStore != null)
            {
                /* For debug purposes, we can intercept and inspect every file the
                 * swiffotron ejects from it's glossy, futuristic core. */
                this.commitStore.Add(key, data);
            }
        }

        /// <summary>
        /// Opens a stream from a store by its key string.
        /// </summary>
        /// <param name="key">The key, including store prefix</param>
        /// <returns>A stream, or null if not found.</returns>
        private Stream OpenFromStore(string key)
        {
            Uri storeURI = new Uri(key);

            if (storeURI.Scheme != "store") /* TODO: Constants, please. */
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML, this.Context,
                        @"Store paths should begin with store://");
            }

            string storeId = storeURI.Host;

            if (!stores.ContainsKey(storeId))
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML, this.Context,
                        @"Store '" + storeId + @"' not registered.");
            }

            try
            {
                return stores[storeId].OpenInput(storeURI.LocalPath.Substring(1));
            }
            catch (FileNotFoundException fnfe)
            {
                throw new SwiffotronException(SwiffotronError.BadPathOrID,
                        this.Context.Sentinel("FileNotFoundInStore"),
                        "File not found: " + key, fnfe);
            }
        }

        /// <summary>
        /// Retrieves a cached object given its fully qualified cache key of the form
        /// [cache name]:[cache key]
        /// </summary>
        /// <param name="key">Full cache path</param>
        /// <returns>The cached object, or null.</returns>
        private object GetCacheItem(string key)
        {
            int pos = key.IndexOf(':');
            if (pos < 0)
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML, this.Context,
                        @"Bad cache key (Requires prefix): " + key);
            }

            string cacheId = key.Substring(0, pos);
            key = key.Substring(pos + 1);

            if (!caches.ContainsKey(cacheId))
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML, this.Context,
                        @"Cache '" + cacheId + @"' not registered.");
            }

            return caches[cacheId].Get(key);
        }

        /// <summary>
        /// Caches an object under a key in a given cache, specified as part of the
        /// cache key. <see cref="GetCacheItem"/>
        /// </summary>
        /// <param name="key">Full cache path</param>
        /// <param name="v">The object to cache</param>
        private void SetCacheItem(string key, object v)
        {
            int pos = key.IndexOf(':');
            if (pos < 0)
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML, this.Context,
                        @"Bad cache key (Requires prefix): " + key);
            }

            string cacheId = key.Substring(0, pos);
            key = key.Substring(pos + 1);

            if (!caches.ContainsKey(cacheId))
            {
                throw new SwiffotronException(
                        SwiffotronError.BadInputXML, this.Context,
                        @"Cache '" + cacheId + @"' not registered.");
            }

            caches[cacheId].Put(key, v);
        }

        /// <summary>
        /// Loads a configuration file passed to the new Swiffotron and parses it,
        /// creating any implementing classes named in the configuration.
        /// </summary>
        /// <param name="configXml">A stream ready and primed with lovely XML
        /// configuration data.</param>
        private void LoadConfigXML(Stream configXml)
        {
            XmlDocument config = new XmlDocument();
            config.Load(XmlReader.Create(configXml, configReaderSettings));

            XmlNamespaceManager namespaceMgr = new XmlNamespaceManager(config.NameTable);

            XPathNavigator nav = config.CreateNavigator();
            namespaceMgr.AddNamespace(@"con", ConfigNS);

            /* First, set up any caches: */
            XmlAttribute attrib;
            foreach (XPathNavigator hit in nav.Select(@"/con:config/con:cache", namespaceMgr))
            {
                XmlAttributeCollection attribs = ((XmlElement)hit.UnderlyingObject).Attributes;

                /* The schema defines these attributes as mandatory, so they will exist: */
                string name = attribs[@"name"].Value;
                string classname = attribs[@"classname"].Value;

                /* Optional parameters, which we default to null before we call Initialise on
                 * any implementor. */
                attrib = attribs[@"init"];
                string init = (attrib == null) ? null : attrib.Value;
                attrib = attribs[@"assembly"];
                string assembly = (attrib == null) ? null : attrib.Value;

                /* Create our named cache as specified by our config file. */
                this.CreateCache(name, assembly, classname, init);
            }

            /* Now, set up any stores: */

            foreach (XPathNavigator hit in nav.Select(@"/con:config/con:store", namespaceMgr))
            {
                XmlAttributeCollection attribs = ((XmlElement)hit.UnderlyingObject).Attributes;

                /* The schema defines these attributes as mandatory, so they will exist: */
                string name = attribs[@"name"].Value;
                string classname = attribs[@"classname"].Value;

                /* Optional parameter, which we default to null before we call Initialise on
                 * any implementor. */
                attrib = attribs[@"init"];
                string init = (attrib == null) ? null : attrib.Value;
                attrib = attribs[@"assembly"];
                string assembly = (attrib == null) ? null : attrib.Value;

                /* Create our named store as specified by our config file. */
                this.CreateStore(name, assembly, classname, init);
            }

            /* TODO: Staggeringly inefficient xpath queries that navigate from the root node every damned
             * time. Do we care? */

            this.EnableStoreWrites = nav.SelectSingleNode(@"/con:config/con:swfprefs/con:storeWriteEnabled/text()", namespaceMgr).ValueAsBoolean;


            this.swfReaderOptions = new SWFReaderOptions()
            {
                StrictTagLength = nav.SelectSingleNode(@"/con:config/con:swfprefs/con:stricttaglength/text()", namespaceMgr).ValueAsBoolean
            };

            this.swfWriterOptions = new SWFWriterOptions()
            {
                Compressed = nav.SelectSingleNode(@"/con:config/con:swfprefs/con:compression/text()", namespaceMgr).ValueAsBoolean,
                EnableDebugger = nav.SelectSingleNode(@"/con:config/con:swfprefs/con:debugcode/text()", namespaceMgr).ValueAsBoolean
            };
        }

        /// <summary>
        /// Creates a store object by name, as specified in the config file.
        /// </summary>
        /// <param name="name">The name for this store that will be referenced in
        /// any swiffotron XML files.</param>
        /// <param name="assembly">The fully qualified assembly name. Pass an empty
        /// string or null to reference the currently executing assembly and use
        /// any default implementors.</param>
        /// <param name="classname">The fully qualified class name</param>
        /// <param name="init">An initialisation string passed in the call to Initialise
        /// on the new store object.</param>
        private void CreateStore(string name, string assembly, string classname, string init)
        {
            if (assembly == string.Empty)
            {
                /* Shortcut value to say "Look in the executing assembly" */
                assembly = null;
            }

            /* Class cast problems just get thrown upwards and destroy the app. This
             * is by design. */
            ObjectHandle oh = Activator.CreateInstance(assembly, classname);
            ISwiffotronStore newStore = (ISwiffotronStore)oh.Unwrap();

            newStore.Initialise(init);

            /* Use Add method here rather than the index operator to ensure that the
             * name is unique. Key errors get thrown upwards and destroy the app.
             * Hey, fix your config file, user. */
            stores.Add(name, newStore);
        }

        /// <summary>
        /// Creates a cache object by name, as specified in the config file.
        /// </summary>
        /// <param name="name">The name for this cache that will be referenced in
        /// any swiffotron XML files.</param>
        /// <param name="assembly">The fully qualified assembly name. Pass an empty
        /// string or null to reference the currently executing assembly and use
        /// any default implementors.</param>
        /// <param name="classname">The fully qualified class name</param>
        /// <param name="init">An initialisation string passed in the call to Initialise
        /// on the new cache object.</param>
        private void CreateCache(string name, string assembly, string classname, string init)
        {
            if (assembly == string.Empty)
            {
                /* Shortcut value to say "Look in the executing assembly" */
                assembly = null;
            }

            /* Class cast problems just get thrown upwards and destroy the app */
            ObjectHandle oh = Activator.CreateInstance(assembly, classname);
            ISwiffotronCache newCache = (ISwiffotronCache)oh.Unwrap();

            newCache.Initialise(init);

            /* Use Add method here to ensure that the name is unique. Key errors get thrown
             * upwards and destroy the app. Hey, fix your config file, user. */
            caches.Add(name, newCache);
        }

        /// <summary>
        /// Loads a swiffotron job XML file, validates it and sets the current
        /// namespace manager so that we can do XPath queries in the 'swf' namespace.
        /// </summary>
        /// <param name="swiffotronXml">A stream feeding XML data.</param>
        /// <returns>The DOM of the swiffotron job XML.</returns>
        private XPathNavigator LoadSwiffotronXML(Stream swiffotronXml)
        {
            XmlDocument doc = new XmlDocument();

            doc.Load(XmlReader.Create(swiffotronXml, swiffotronReaderSettings));

            this.namespaceMgr = new XmlNamespaceManager(doc.NameTable);
            this.namespaceMgr.AddNamespace(@"swf", SwiffotronNS);

            return doc.CreateNavigator();
        }

#if(DEBUG)
        public XPathNavigator LoadSwiffotronXML_accesor(Stream swiffotronXml)
        {
            return this.LoadSwiffotronXML(swiffotronXml);
        }
#endif
    }
}
