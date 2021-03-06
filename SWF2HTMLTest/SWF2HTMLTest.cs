﻿//-----------------------------------------------------------------------
// SWF2SVGTest.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWF2HTML.Test
{
    using System.IO;
#if(EXPRESS2010)
    using ExpressTest.UnitTesting;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using SWFProcessing.SWF2HTML;
    using SWFProcessing.SWFModeller;
    using SWFProcessing.SWFModeller.Process;
    using System.Reflection;
    using System;

    /// <summary>
    ///This is a test class for SWF2HTMLTest and is intended
    ///to contain all SWF2HTMLTest Unit Tests
    ///</summary>
    [TestClass]
    public class SWF2HTMLTest
    {
        private string TestDir;
        private string TestDumpDir;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        /// <summary>
        /// Called before each test.
        /// </summary>
        [TestInitialize]
        public void InitilizeTests()
        {
            DirectoryInfo di = new DirectoryInfo(this.TestContext.TestDir);
            if (this.TestContext.TestDir.ToLower().Contains("ncrunch"))
            {
                this.TestDumpDir = di.Parent.Parent.Parent.Parent.FullName + @"\FullDump\";
            }
            else
            {
                this.TestDumpDir = di.Parent.FullName + @"\FullDump\";
            }

            this.TestDir = this.TestContext.TestDir + @"\Out\" +
                    this.GetType().Name + @"." + this.TestContext.TestName + @"\";

            Directory.CreateDirectory(this.TestDumpDir);
            Directory.CreateDirectory(this.TestDir);
        }

        [TestCleanup]
        public void CopyToDump()
        {
            string[] files = Directory.GetFiles(
                    this.TestDir,
                    "*",
                    SearchOption.AllDirectories);

            // Copy all the files.
            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);

                File.Copy(file, this.TestDumpDir + fi.Name, true);
            }
        }


        /// <summary>
        /// A test for the simplest of SWF files.
        /// </summary>
        [TestMethod]
        public void ConvertSimplestSWF()
        {
            string name = "SWF2HTMLTest.ConvertSimplestSWF";
            SWF swf = new SWF(new SWFContext(name), false);
            ConvertSWF(name, swf);
        }

        /// <summary>
        /// A test for the simplest of SWF files.
        /// </summary>
        [TestMethod]
        public void ConvertSimpleAnimation()
        {
            TestSWF("simple-animation.swf");
        }

        private void TestSWF(string name)
        {
            using (SWFReader swfIn = new SWFReader(ResourceAsStream(name), new SWFModeller.IO.SWFReaderOptions(), null, null))
            {
                SWF swf = swfIn.ReadSWF(new SWFContext(name));
                ConvertSWF(name, swf);
            }
        }

        protected Stream ResourceAsStream(string sRes)
        {
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(@"SWFProcessing.SWF2HTML.Test.res." + sRes);

            if (s == null)
            {
                /* TODO: Fix this. */
                s = Assembly.GetCallingAssembly().GetManifestResourceStream("SwiffotronTestExpress.res." + sRes);
            }
            Assert.IsNotNull(s, "Test input missing! " + sRes);
            return s;
        }

        private void ConvertSWF(string name, SWF swf)
        {
            SWF2HTML converter = new SWF2HTML(swf, name, new SWF2HTMLOptions() { 
                OutputComments = true,
                ConsoleLogging = true
            });

            using (FileStream output = new FileStream(TestDir + name + ".html", FileMode.Create))
            using (Stream htmlOut = converter.GetHTML(true))
            {
                byte[] buffer = new byte[32768];
                while (true)
                {
                    int read = htmlOut.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return;
                    output.Write(buffer, 0, read);
                }
            }
        }
    }
}
