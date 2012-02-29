//-----------------------------------------------------------------------
// XMLHelper.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.Processor
{
    using System.Xml;
    using System.IO;
    using System.Reflection;
    using System.Xml.XPath;
    using System.Diagnostics;
    using SWFProcessing.SWFModeller.Characters.Geom;
    using System.Drawing;
    using System;

    internal class XMLHelper
    {
        /// <summary>The swiffotron XML file namespace</summary>
        private const string SwiffotronNS = @"urn:swiffotron-schemas:swiffotron-job/24/05/2011";

        /* Tag attributes: */

        /// <summary>XML attribute; store reference</summary>
        public const string AttrStore = @"store";

        /// <summary>XML attribute; Some cache key value</summary>
        public const string AttrCachekey = @"cachekey";

        /// <summary>XML attribute; Width pixel value</summary>
        public const string AttrWidth = @"width";

        /// <summary>XML attribute; Height pixel value</summary>
        public const string AttrHeight = @"height";

        /// <summary>XML attribute; X pixel position</summary>
        public const string AttrX = @"x";

        /// <summary>XML attribute; Y pixel position</summary>
        public const string AttrY = @"y";

        /// <summary>XML attribute; Background colour</summary>
        public const string AttrBGColor = @"bgcolor";

        /// <summary>XML attribute; Base SWF ref</summary>
        public const string AttrBase = @"base";

        /// <summary>XML attribute; Qualified name</summary>
        public const string AttrQName = @"qname";

        /// <summary>XML attribute; Copy from reference</summary>
        public const string AttrFrom = @"from";

        /// <summary>XML attribute; Copy to reference</summary>
        public const string AttrTo = @"to";

        /// <summary>XML attribute; Rotate value</summary>
        public const string AttrRotate = @"rotate";

        /// <summary>XML attribute; X-axis scale factor</summary>
        public const string AttrScaleX = @"scalex";

        /// <summary>XML attribute; Y-axis scale factor</summary>
        public const string AttrScaleY = @"scaley";

        /// <summary>XML attribute; Type of reference</summary>
        public const string AttrType = @"type";

        /// <summary>XML attribute; Path to something. A qname or a class. May be * for all.</summary>
        public const string AttrPath = @"path";

        /// <summary>XML attribute; Source reference</summary>
        public const string AttrSrc = @"src";

        /// <summary>XML attribute; Class name</summary>
        public const string AttrClass = @"class";

        /// <summary>XML attribute; SWF reference</summary>
        public const string AttrSwf = @"swf";

        /// <summary>XML attribute; Relative to reference</summary>
        public const string AttrRelativeTo = @"relativeTo";

        /// <summary>XML attribute; Element ID reference</summary>
        public const string AttrID = @"id";

        /* Tag names: */

        /// <summary>XML tag name; A SWF declaration</summary>
        public const string TagSwf = @"swf";

        /// <summary>XML tag name; A SWF output declaration</summary>
        public const string TagSwfOut = @"swfout";

        /// <summary>XML tag name; A PNG image output declaration</summary>
        public const string TagPngOut = @"pngout";

        /// <summary>XML tag name; A video output declaration</summary>
        public const string TagVidOut = @"vidout";

        /// <summary>XML tag name; An SVG output declaration</summary>
        public const string TagSvgOut = @"svgout";

        /// <summary>XML tag name; An instruction to modify a clip instance</summary>
        public const string TagModify = @"modify";

        /// <summary>XML tag name; A text search and replace operation</summary>
        public const string TagTextReplace = @"textreplace";

        /// <summary>XML tag name; An instance declaration</summary>
        public const string TagInstance = @"instance";

        /// <summary>XML tag name; A movie clip declaration</summary>
        public const string TagMovieClip = @"movieclip";

        /// <summary>XML tag name; Relative move instruction</summary>
        public const string TagMoveRel = @"moveRel";

        /// <summary>XML tag name; Absolute move instruction</summary>
        public const string TagMoveAbs = @"moveAbs";

        /// <summary>XML tag name; Absolute position</summary>
        public const string TagAbsolute = @"absolute";

        /// <summary>XML tag name; Relative position</summary>
        public const string TagRelative = @"relative";

        /// <summary>XML tag name; Remove an instance from a SWF</summary>
        public const string TagRemove = @"remove";

        /* Tag attribute values: */

        /// <summary>XML value; SWF type</summary>
        public const string ValSwf = @"swf";

        /// <summary>XML value; Movie clip type</summary>
        public const string ValMovieClip = @"movieclip";

        /// <summary>XML value; Actionscript type</summary>
        public const string ValActionscript = @"actionscript";

        /// <summary>XML value; Instance type</summary>
        public const string ValInstance = @"instance";

        /// <summary>XML value; External SWF type</summary>
        public const string ValExtern = @"extern";

        /// <summary>The namespace manager for the current job XML</summary>
        private XmlNamespaceManager nsMgr;

        /// <summary>XML parsing settings for reading swiffotron job files.</summary>
        private XmlReaderSettings swiffotronReaderSettings;

        private SwiffotronContext Context;

        /// <summary>An XPath navigator that points to the root of the current XML file</summary>
        private XPathNavigator root;

        public XMLHelper()
        {
            this.swiffotronReaderSettings = CreateValidationSettings(@"swiffotron.xsd");
        }

        public void SetContext(SwiffotronContext ctx)
        {
            this.Context = ctx;
        }

        /// <summary>
        /// For a given schema name (Named resource) this loads the schema XML and
        /// creates an XmlReaderSettings object which can be used to validate any
        /// XML read by the Swiffotron. This is called in the static initialiser.
        /// </summary>
        /// <param name="schemaName">Named resource which is an XSD file.</param>
        /// <returns>Some validation settings useful to an XmlReader.</returns>
        public static XmlReaderSettings CreateValidationSettings(string schemaName)
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
        /// Loads a swiffotron job XML file, validates it and sets the current
        /// namespace manager so that we can do XPath queries in the 'swf' namespace.
        /// </summary>
        /// <param name="swiffotronXml">A stream feeding XML data.</param>
        /// <returns>The DOM of the swiffotron job XML.</returns>
        public void LoadSwiffotronXML(Stream swiffotronXml)
        {
            XmlDocument doc = new XmlDocument();

            doc.Load(XmlReader.Create(swiffotronXml, swiffotronReaderSettings));

            this.nsMgr = new XmlNamespaceManager(doc.NameTable);
            this.nsMgr.AddNamespace(@"swf", XMLHelper.SwiffotronNS);

            this.root = doc.CreateNavigator();

            if (this.root == null)
            {
                throw new SwiffotronException(SwiffotronError.BadInputXML, this.Context.Sentinel("LoadSwiffotronXML"));
            }
        }

        /// <summary>
        /// Find a swf xml node by a reference
        /// </summary>
        /// <param name="referee">A movieclip, or instance tag</param>
        /// <returns>The SWF tag that it references, or null if it doesn't reference one.</returns>
        public XPathNavigator SwfTagFromRef(XPathNavigator referee)
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
                XPathNavigator referenced = root.SelectSingleNode(@"/swf:swiffotron/swf:swf[@id='" + src + "']", nsMgr);
                if (referenced == null)
                {
                    throw new SwiffotronException(
                            SwiffotronError.BadPathOrID,
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
                XPathNavigator referenced = root.SelectSingleNode(@"/swf:swiffotron/swf:swf/swf:movieclip[@id='" + src + "']", nsMgr);
                if (referenced == null)
                {
                    throw new SwiffotronException(
                            SwiffotronError.BadPathOrID,
                            this.Context.Sentinel("InstanceSrcMovieClipBadref"),
                            "No such movieclip element: " + src);
                }

                return referenced;
            }

            string swf = referee.GetAttribute(AttrSwf, string.Empty);
            if (swf != null && swf != string.Empty)
            {
                /* This tag declares the swf id in its swf attribute: */
                return root.SelectSingleNode(@"/swf:swiffotron/swf:swf[@id='" + swf + "']", nsMgr);
            }

            return null;
        }

        /// <summary>
        /// Creates a new position matrix from an XML declaration of one.
        /// </summary>
        /// <param name="transform">The navigator pointing to the XML transform element.</param>
        /// <returns>A new Matrix</returns>
        public Matrix TransformTagToMatrix(XPathNavigator transform)
        {
            Matrix m = new Matrix();
            m.TransX = (float)XmlConvert.ToDouble(transform.GetAttribute(AttrX, string.Empty));
            m.TransY = (float)XmlConvert.ToDouble(transform.GetAttribute(AttrY, string.Empty));

            string rot = transform.GetAttribute(XMLHelper.AttrRotate, string.Empty);
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
        /// Convenience method to move a navigator to the first child element, rather
        /// than first child node, which could be text or something.
        /// </summary>
        /// <param name="nav">The navigator to move</param>
        public void MoveToFirstChildElement(XPathNavigator nav)
        {
            nav.MoveToFirstChild();

            if (nav.NodeType == XPathNodeType.Element)
            {
                return;
            }

            nav.MoveToNext(XPathNodeType.Element); /* Bit of an assumption, but never mind. */
        }

        /// <summary>
        /// Gets an HTML colour attribute value from an XML node
        /// </summary>
        /// <param name="nav">A pointer to the node</param>
        /// <param name="name">The attribute to get</param>
        /// <returns>A colour, or null if not present.</returns>
        public Color? ColorAttribute(XPathNavigator nav, string name)
        {
            string stringVal = nav.GetAttribute(name, string.Empty);
            if (stringVal != string.Empty)
            {
                return ColorTranslator.FromHtml(stringVal);
            }

            return null;
        }

        /// <summary>
        /// Gets an integer attribute value from an XML node
        /// </summary>
        /// <param name="nav">A pointer to the node</param>
        /// <param name="name">The attribute to get</param>
        /// <returns>An integer, or null if not present.</returns>
        public int? IntegerAttribute(XPathNavigator nav, string name)
        {
            /* Why didn't I use ValueAsInt here? Check the behaviour with
             * ropy values. Maybe that was it...*/

            string stringVal = nav.GetAttribute(name, string.Empty);
            if (stringVal != string.Empty)
            {
                return Convert.ToInt32(stringVal);
            }

            return null;
        }

        public string SelectString(string path)
        {
            return this.root.SelectSingleNode(path, this.nsMgr).ToString();
        }

        public XPathNodeIterator Select(string path)
        {
            return this.root.Select(path, this.nsMgr);
        }

        public XPathNodeIterator Select(XPathNavigator top, string path)
        {
            return top.Select(path, this.nsMgr);
        }

        public XPathNodeIterator SelectChildren(XPathNavigator top, string path)
        {
            return top.SelectChildren(path, XMLHelper.SwiffotronNS);
        }

        public XPathNavigator SelectNode(string path)
        {
            return this.root.SelectSingleNode(path, this.nsMgr);
        }

        public XPathNavigator SelectNode(XPathNavigator top, string path)
        {
            return top.SelectSingleNode(path, this.nsMgr);
        }
    }
}
