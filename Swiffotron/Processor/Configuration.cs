//-----------------------------------------------------------------------
// Configuration.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.Processor
{
    using System.IO;
    using System.Xml;
    using System.Xml.XPath;
    using SWFProcessing.Swiffotron.IO;
    using System.Collections.Generic;
    using SWFProcessing.SWFModeller.IO;
    using SWFProcessing.SWFModeller;
    using System;
    using SWFProcessing.SWF2HTML;

    public class Configuration
    {
        /// <summary>Settings to pass to the SWF parser</summary>
        public SWFReaderOptions swfReaderOptions { get; private set;}

        /// <summary>Settings to pass to the SWF writer</summary>
        public SWFWriterOptions swfWriterOptions { get; private set; }

        /// <summary>
        /// Prevents Swiffotron saving files to the store. Why would you do this? Well you
        /// might prefer to get the data from a commit listener (Which will still be enabled)
        /// and squirt it out over a network or something instead.
        /// </summary>
        public bool EnableStoreWrites { get; set; }

        public bool HTMLStandalone { get; set; }

        public Caches Caches { get; private set; }

        public Stores Stores { get; private set; }

        public SWF2HTMLOptions.FrameworkType HTMLType { get; private set; }

        private XMLHelper Xml;

        /// <summary>
        /// Input stream of configuration XML.
        /// </summary>
        /// <param name="xmlIn"></param>
        public Configuration(Stream xmlIn)
        {
            this.Caches = new Caches();
            this.Stores = new Stores(this);

            this.LoadConfigXML(xmlIn);
        }

        /// <summary>
        /// Loads a configuration file passed to the new Swiffotron and parses it,
        /// creating any implementing classes named in the configuration.
        /// </summary>
        /// <param name="configXml">A stream ready and primed with lovely XML
        /// configuration data.</param>
        private void LoadConfigXML(Stream configXml)
        {
            this.Xml = new XMLHelper();
            this.Xml.SetContext(new SwiffotronContext("Swiffotron configuration"));

            Xml.LoadConfigurationXML(configXml);

            /* First, set up any caches: */
            XmlAttribute attrib;
            foreach (XPathNavigator hit in Xml.Select(@"/con:config/con:cache"))
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
                ISwiffotronCache newCache = Extern.CreateCache(name, assembly, classname, init);

                /* Use Add method here to ensure that the name is unique. Key errors get thrown
                 * upwards and destroy the app. Hey, fix your config file, user. */
                this.Caches.Register(name, newCache);

            }

            /* Now, set up any stores: */

            foreach (XPathNavigator hit in Xml.Select(@"/con:config/con:store"))
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
                ISwiffotronStore newStore = Extern.CreateStore(name, assembly, classname, init);

                /* Use Add method here rather than the index operator to ensure that the
                 * name is unique. Key errors get thrown upwards and destroy the app.
                 * Hey, fix your config file, user. */
                this.Stores.Register(name, newStore);

            }

            /* ISSUE 68: Staggeringly inefficient xpath queries that navigate from the root node every damned
             * time. Do we care? */

            this.EnableStoreWrites = Xml.SelectBoolean(@"/con:config/con:swfprefs/con:storeWriteEnabled/text()");

            this.swfReaderOptions = new SWFReaderOptions()
            {
                StrictTagLength = Xml.SelectBoolean(@"/con:config/con:swfprefs/con:stricttaglength/text()")
            };

            this.swfWriterOptions = new SWFWriterOptions()
            {
                Compressed = Xml.SelectBoolean(@"/con:config/con:swfprefs/con:compression/text()"),
                EnableDebugger = Xml.SelectBoolean(@"/con:config/con:swfprefs/con:debugcode/text()")
            };

            string htmlType = Xml.SelectString(@"/con:config/con:htmlType/text()", "JQuery");
            this.HTMLType = (SWF2HTMLOptions.FrameworkType)Enum.Parse(typeof(SWF2HTMLOptions.FrameworkType), htmlType);
            this.HTMLStandalone = Xml.SelectBoolean(@"/con:config/con:htmlStandalone/text()", false);
        }

        /// <summary>
        /// Get some useful information for debug purposes letting us find out how things
        /// are set up. I should list them all here, really.
        /// </summary>
        /// <param name="info">An accumulating big map of arbitrary string->string data
        /// that you can pick apart and use as you so desire.</param>
        public void Interrogate(Dictionary<string, string> info)
        {
            this.Stores.Interrogate(info);
            this.Caches.Interrogate(info);
        }
    }
}
