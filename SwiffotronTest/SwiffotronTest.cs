//-----------------------------------------------------------------------
// SwiffotronTest.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.Test
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml.Schema;
    using System.Xml.XPath;
#if(EXPRESS2010)
    using ExpressTest.UnitTesting;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using SWFProcessing.SWFModeller;
    using SWFProcessing.SWFModeller.ABC;
    using SWFProcessing.SWFModeller.ABC.Debug;
    using SWFProcessing.SWFModeller.ABC.IO;
    using SWFProcessing.SWFModeller.IO;
    using SWFProcessing.Swiffotron.IO.Debug;
    using SWFProcessing.Swiffotron.IO;
    using SWFProcessing.SWFModeller.Process;
    using SWFProcessing.Swiffotron.Processor;

    /// <summary>
    /// These tests should always pass. Always.
    /// </summary>
    [TestClass]
    public class SwiffotronTest : TestingBaseClass, IABCLoadInterceptor, ISwiffotronReadLogHandler
    {
        private Dictionary<string, string> swfReadLogs;
        private Dictionary<string, string> swfReadModelLogs;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        private string TestDir;

        /// <summary>
        /// In the commit handler, we turn the SWF commit into a model dump. We record the
        /// filename so that we can find it again after a call to Swiffotron.Process in order
        /// to inspect it.
        /// </summary>
        private string lastCommitModelOutput;

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        /// <summary>
        /// Called before each test to clear out all the caches.
        /// </summary>
        [TestInitialize]
        public void InitilizeTests()
        {
            TestDir = TestContext.TestDir + @"\Out\" +
                    this.GetType().Name + @"." + TestContext.TestName + @"\";

            Directory.CreateDirectory(TestDir);

            this.swfReadLogs = new Dictionary<string, string>();
            this.swfReadModelLogs = new Dictionary<string, string>();
        }

        [TestCleanup]
        public void CleanupTests()
        {
            string readswfsDir = TestDir + @"\readswfs\";
            Directory.CreateDirectory(readswfsDir);

            foreach (string name in this.swfReadLogs.Keys)
            {
                using (FileStream fs = new FileStream(readswfsDir + name + ".log.txt", FileMode.Create))
                {
                    byte[] log = new ASCIIEncoding().GetBytes(this.swfReadLogs[name]);
                    fs.Write(log, 0, log.Length);
                }
            }

            foreach (string name in this.swfReadModelLogs.Keys)
            {
                using (FileStream fs = new FileStream(readswfsDir + name + ".model.txt", FileMode.Create))
                {
                    byte[] log = new ASCIIEncoding().GetBytes(this.swfReadModelLogs[name]);
                    fs.Write(log, 0, log.Length);
                }
            }
        }

        public void OnLoadAbc(bool lazyInit, SWFContext ctx, string abcName, int doAbcCount, byte[] bytecode)
        {
            string abcDir = TestDir + @"\abc\";
            Directory.CreateDirectory(abcDir);

            string name = ctx.Name + "." + abcName + doAbcCount + abcName + ".abc";

            using (FileStream fs = new FileStream(abcDir + name, FileMode.Create))
            {
                fs.Write(bytecode, 0, bytecode.Length);
            }

            StringBuilder abcReadLog = new StringBuilder();

            AbcCode code = new AbcReader().Read(bytecode, abcReadLog);

            StringBuilder abcd = new StringBuilder();
            code.ToStringModelView(0, abcd);
            byte[] dasmBytes = new ASCIIEncoding().GetBytes(abcd.ToString());

            using (FileStream fs = new FileStream(abcDir + name + ".txt", FileMode.Create))
            {
                fs.Write(dasmBytes, 0, dasmBytes.Length);
            }

            byte[] abcReadLogBytes = new ASCIIEncoding().GetBytes(abcReadLog.ToString());
            using (FileStream fs = new FileStream(abcDir + name + ".abcread.txt", FileMode.Create))
            {
                fs.Write(abcReadLogBytes, 0, abcReadLogBytes.Length);
            }
        }

        /// <summary>
        /// Tests the LoadSwiffotronXML method. Passes if a navigator is returned and
        /// no exceptions were thrown.
        /// </summary>
        [TestMethod]
        public void TestJobValidate()
        {
            new Swiffotron().LoadSwiffotronXML_accesor(ResourceAsStream(@"TestJobValidate.xml"));
        }

        /// <summary>
        /// Passes a swiffotron a job XML file that is not valid against the schema, and checks
        /// that we get a validation exception.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(XmlSchemaValidationException))]
        public void TestBrokenJobValidate()
        {
            new Swiffotron().LoadSwiffotronXML_accesor(ResourceAsStream(@"TestBrokenJobValidate.xml"));
        }

        /// <summary>
        /// Creates a swiffotron with no config, leading it to use the default config.
        /// If this test doesn't throw any exceptions, the default config file was loaded and
        /// is valid.
        /// </summary>
        [TestMethod]
        public void TestDefaultConfig()
        {
            Swiffotron swiffotron = new Swiffotron(null);
        }

        /// <summary>
        /// Passes a swiffotron a config XML file that is not valid against the schema, and checks
        /// that we get a validation exception.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(XmlSchemaValidationException))]
        public void TestBrokenConfig()
        {
            Swiffotron swiffotron = new Swiffotron(ResourceAsStream(@"TestBrokenConfig.xml"));
        }

        /// <summary>
        /// Tests a job which produces a super-simple SWF file.
        /// </summary>
        [TestMethod]
        public void TestSimpleJob()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(@"TestSimpleJob.xml", @"TestSimpleJob.swf", out swiffotron);
        }

        /// <summary>
        /// Tests a job which instantiates a SWF as a clip with images in it.
        /// </summary>
        [TestMethod]
        public void TestImages()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(@"TestImages.xml", @"TestImages.swf", out swiffotron);
        }

        [TestMethod]
        public void TestOneRedCircle()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(@"TestOneRedCircle.xml", @"TestOneRedCircle.swf", out swiffotron);
        }

        [TestMethod]
        public void TestSimplestTimelineScript()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(
                    @"TestSimplestTimelineScript.xml",
                    @"TestSimplestTimelineScript.swf",
                    out swiffotron);
        }

        [TestMethod]
        public void TestOneBlueCircle()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(@"TestOneBlueCircle.xml", @"TestOneBlueCircle.swf", out swiffotron);
        }

        /// <summary>
        /// Tests a job which combines two files that contain AS3 functionality.
        /// </summary>
        [TestMethod]
        public void TestABCCircles()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(@"TestABCCircles.xml", @"TestABCCircles.swf", out swiffotron);
        }

        /// <summary>
        /// Tests a job which produces a convoy of UFOs.
        /// </summary>
        [TestMethod]
        public void TestUFO()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(@"TestUFO.xml", @"TestUFO.swf", out swiffotron);
        }

        /// <summary>
        /// Tests a job which produces tweens.
        /// </summary>
        [TestMethod]
        public void TestTweens()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(@"TestTweens.xml", @"TestTweens.swf", out swiffotron);
        }

        /// <summary>
        /// Tests a job with a movieclip node of type swf and a broken path
        /// </summary>
        [TestMethod]
        public void TestBrokenMovieclipTypeSWF()
        {
            TestExpectedSwiffotronError(
                    @"TestBrokenMovieclipTypeSWF.xml",
                    SwiffotronError.BadPathOrID,
                    "SrcSwfBadref");
        }

        /// <summary>
        /// Tests a job with a movieclip node of type swf
        /// </summary>
        [TestMethod]
        public void TestMovieclipTypeSWF()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(@"TestMovieclipTypeSWF.xml", @"TestMovieclipTypeSWF.swf", out swiffotron);
        }


        /// <summary>
        /// Tests text replacement
        /// </summary>
        [TestMethod]
        public void TestTextReplace()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(@"TestTextReplace.xml", @"TestTextReplace.swf", out swiffotron);
        }

        /// <summary>
        /// Tests text replacement with empty strings
        /// </summary>
        [TestMethod]
        public void TestTextReplaceEmptyString()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(@"TestTextReplaceEmptyString.xml", @"TestTextReplaceEmptyString.swf", out swiffotron);
        }

        /// <summary>
        /// Tests a job with an instance node of type extern. This test is functionally equivalent to
        /// TestMovieclipTypeExtern in its output.
        /// </summary>
        [TestMethod]
        public void TestInstanceTypeExtern()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(@"TestInstanceTypeExtern.xml", @"TestInstanceTypeExtern.swf", out swiffotron);
        }

        /// <summary>
        /// Tests a job with a movieclip node of type extern. This test is functionally equivalent to
        /// TestInstanceTypeExtern in its output.
        /// </summary>
        [TestMethod]
        public void TestMovieclipTypeExtern()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(@"TestMovieclipTypeExtern.xml", @"TestMovieclipTypeExtern.swf", out swiffotron);
        }

        /// <summary>
        /// Tests a job with a movieclip node of type extern with a broken store path.
        /// </summary>
        [TestMethod]
        public void TestBrokenMovieclipTypeExtern()
        {
            TestExpectedSwiffotronError(
                    @"TestBrokenMovieclipTypeExtern.xml",
                    SwiffotronError.BadPathOrID,
                    "FileNotFoundInStore");
        }

        /// <summary>
        /// Tests a job with an instance of type movieclip and a bad src.
        /// </summary>
        [TestMethod]
        public void TestBrokenInstanceTypeMovieClip()
        {
            TestExpectedSwiffotronError(
                    @"TestBrokenInstanceTypeMovieClip.xml",
                    SwiffotronError.BadPathOrID,
                    "InstanceSrcMovieClipBadref");
        }

        /// <summary>
        /// Tests a job with an instance with a main timeline class that was not renamed.
        /// </summary>
        [TestMethod]
        public void TestNotRenamedMainTimeline()
        {
            TestExpectedSwiffotronError(
                    @"TestNotRenamedMainTimeline.xml",
                    SwiffotronError.BadInputXML,
                    "MainTimelineInstanceNotRenamed");
        }

        /// <summary>
        /// Tests a job with an instance of type movieclip and a bad src.
        /// </summary>
        [TestMethod]
        public void TestInappropriateClassName()
        {
            TestExpectedSwiffotronError(
                    @"TestInappropriateClassName.xml",
                    SwiffotronError.BadInputXML,
                    "InstanceClassNameInappropriate");
        }

        /// <summary>
        /// Tests a job which instiantiates clips that have code and that do not have code. Both
        /// types of clips should have instance variables automatically declared.
        /// </summary>
        [TestMethod]
        public void TestClasslessInstantiation()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(
                    @"TestClasslessInstantiation.xml",
                    @"TestClasslessInstantiation.swf",
                    out swiffotron);
        }

        /// <summary>
        /// Tests a job which merges embedded fonts.
        /// </summary>
        [TestMethod]
        public void TestFontMerge()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(@"TestFontMerge.xml", @"TestFontMerge.swf", out swiffotron);
        }

        /// <summary>
        /// Tests a job which uses an instance of a referenced SWF node (type swf).
        /// </summary>
        [TestMethod]
        public void TestInstanceTypeSWF()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(@"TestInstanceTypeSWF.xml", @"TestInstanceTypeSWF.swf", out swiffotron);
        }

        /// <summary>
        /// Tests a job which uses an instance of type instance and a broken src value
        /// </summary>
        [TestMethod]
        public void TestBrokenInstanceTypeInstance()
        {
            TestExpectedSwiffotronError(
                    @"TestBrokenInstanceTypeInstance.xml",
                    SwiffotronError.BadPathOrID,
                    "FindSpriteByQName");
        }

        /// <summary>
        /// Tests a job which removes an instance
        /// </summary>
        [TestMethod]
        public void TestRemoveInstance()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(@"TestRemoveInstance.xml", @"TestRemoveInstance.swf", out swiffotron);
        }

        /// <summary>
        /// Tests a job with 2 movieclips with the same class name.
        /// </summary>
        [TestMethod]
        public void TestUnqualifiedGeneratedTimelineClass()
        {
            TestExpectedSwiffotronError(
                    @"TestUnqualifiedGeneratedTimelineClass.xml",
                    SwiffotronError.BadInputXML,
                    "CreateInstanceIn", 
                   SWFModellerError.CodeMerge,
                   "[SWF Context: abccircles.circle.swf, TimelineDefaultPackage]");
        }

        /// <summary>
        /// Tests a job with 2 movieclips with the same class name.
        /// </summary>
        [TestMethod]
        public void TestCollidingClassname()
        {
            TestExpectedSwiffotronError(
                    @"TestCollidingClassname.xml",
                    SwiffotronError.BadInputXML,
                    "ClassNameCollision",
                    SWFModellerError.CodeMerge,
                    "[SWF Context: TestCollidingClassnameSWF, ClassNameCollision]");
        }

        private void TestExpectedSwiffotronError(
                string inputXML,
                SwiffotronError error,
                string errorSentinel,
                SWFModellerError? innerError = null,
                string innerSentinel = null)
        {
            /* Why not use expected exception attributes from MSTest? Well because we want to inspect not
             * only the extra error information that our fabulous custom exceptions provide, but we want to
             * inspect inner exceptions too. We're that thorough. */

            MockStore store;
            MockCache cache;
            Swiffotron swiffotron = CreateMockSwiffotron(out store, out cache);

            StringBuilder writeLog = new StringBuilder();
            StringBuilder abcWriteLog = new StringBuilder();

            try
            {
                swiffotron.Process(ResourceAsStream(inputXML), null, null, null, null, null);
                Assert.Fail("An exception of type " + error + " was expected. No exception was thrown.");
            }
            catch (SwiffotronException se)
            {
                Assert.AreEqual(error, se.Error, "Error mismatch: " + se.Message);
                Assert.AreEqual(errorSentinel, se.Sentinel, "Error sentinel mismatch: " + se.Message);
                if (innerError != null)
                {
                    Assert.IsTrue(se.InnerException is SWFModellerException, "Expected an inner exception of type SWFModellerException");
                    Assert.AreEqual(innerError, (se.InnerException as SWFModellerException).Error, "Error mismatch: " + se.InnerException.Message);
                    Assert.AreEqual(innerSentinel, (se.InnerException as SWFModellerException).Sentinel, "Error sentinel mismatch: " + se.InnerException.Message);
                }
            }

            /* And as a parting shot: */
            Assert.AreNotEqual(error, SwiffotronError.Internal, "Can't test for Internal errors. This test is clearly not written properly.");
        }

        /// <summary>
        /// Tests a job which uses an instance of type extern and a broken src value
        /// </summary>
        [TestMethod]
        public void TestBrokenInstanceTypeExtern()
        {
            TestExpectedSwiffotronError(@"TestBrokenInstanceTypeExtern.xml", SwiffotronError.BadPathOrID, "FileNotFoundInStore");
        }

        /// <summary>
        /// Tests a job which uses an instance of a referenced SWF node (type swf) with
        /// a broken src value.
        /// </summary>
        [TestMethod]
        public void TestBrokenInstanceTypeSWF()
        {
            TestExpectedSwiffotronError(@"TestBrokenInstanceTypeSWF.xml", SwiffotronError.BadPathOrID, "SrcSwfBadref");
        }

        private void ImageOutputTest(string xmlIn, string imageOut, out Swiffotron swiffotron)
        {
            MockStore store;
            MockCache cache;
            swiffotron = CreateMockSwiffotron(out store, out cache);

            Dictionary<string, byte[]> commits = new Dictionary<string, byte[]>();
            swiffotron.Process(ResourceAsStream(xmlIn), commits, null, null, this, this);
            CheckCommits(commits);

            CopyStoreToTestDir(store);

            Assert.IsTrue(store.Has(imageOut), @"Output was not saved");
        }

        private void PredictedOutputTest(string xmlIn, string swfOut, out Swiffotron swiffotron)
        {
            MockStore store;
            MockCache cache;
            swiffotron = CreateMockSwiffotron(out store, out cache);

            StringBuilder writeLog = new StringBuilder();
            StringBuilder abcWriteLog = new StringBuilder();
            Dictionary<string, byte[]> commits = new Dictionary<string, byte[]>();
            swiffotron.Process(ResourceAsStream(xmlIn), commits, writeLog, abcWriteLog, this, this);
            CheckCommits(commits);

            using (FileStream fs = new FileStream(TestDir + xmlIn + ".writelog.txt", FileMode.Create))
            {
                byte[] writeLogData = new ASCIIEncoding().GetBytes(writeLog.ToString());
                fs.Write(writeLogData, 0, writeLogData.Length);
            }

            using (FileStream fs = new FileStream(TestDir + xmlIn + ".abcwritelog.txt", FileMode.Create))
            {
                byte[] writeLogData = new ASCIIEncoding().GetBytes(abcWriteLog.ToString());
                fs.Write(writeLogData, 0, writeLogData.Length);
            }

            CopyStoreToTestDir(store);

            Assert.IsTrue(store.Has(swfOut), @"Output was not saved");

            /* Check that a valid SWF file was produced by reading it back: */
            StringBuilder binDump = new StringBuilder();

            SWF swf = null;

            SWFReader sr = new SWFReader(
                    store.OpenInput(swfOut),
                    new SWFReaderOptions() { StrictTagLength = true },
                    binDump,
                    this); /* constantFilter is a delegate function that will throw an exception
                            * if it spots something objectionable in the SWF's constants. */

            try
            {
                swf = sr.ReadSWF(new SWFContext(swfOut));

                /* The delegate we gave to the SWF reader for trapping ABC constants will
                 * not have been run yet since the SWF reader doesn't parse the ABC unless
                 * it really needs to. Here's how we force it to, and run the filter
                 * delegate... */

                swf.ScriptProc(delegate(DoABC abc)
                {
                    AbcCode code = abc.Code; /* Transparently parses the ABC */
                });
            }
            finally
            {
                using (FileStream fs = new FileStream(TestDir + xmlIn + ".bin.dump.txt", FileMode.Create))
                {
                    byte[] dumpbindata = new ASCIIEncoding().GetBytes(binDump.ToString());
                    fs.Write(dumpbindata, 0, dumpbindata.Length);
                }
            }

            string predicted = TestDir + swfOut + ".model.predict.txt";
            using (Stream input = ResourceAsStream("predicted." + swfOut + ".txt"))
            using (FileStream output = new FileStream(predicted, FileMode.Create))
            {
                Assert.IsNotNull(input, "Predicted output is missing! "+swfOut);
                CopyStream(input, output);
            }

            using (StreamWriter acceptScript = new StreamWriter(new FileStream(TestDir + "accept.bat", FileMode.Create)))
            {
                acceptScript.WriteLine("copy \"" + lastCommitModelOutput + "\" \"" + new FileInfo("..\\..\\..\\SwiffotronTest\\res\\predicted\\" + swfOut + ".txt").FullName + "\"");
            }

            using (StreamWriter viewScript = new StreamWriter(new FileStream(TestDir + "viewdiff.bat", FileMode.Create)))
            {
                /* ISSUE 44: This should be a diff tool env var */
                viewScript.WriteLine("\"c:\\Program Files (x86)\\WinMerge\\WinMergeU.exe\" \"" + lastCommitModelOutput + "\" \"" + new FileInfo("..\\..\\..\\SwiffotronTest\\res\\predicted\\" + swfOut + ".txt").FullName + "\"");
            }

            CompareFiles(
                predicted,
                lastCommitModelOutput,
                "Predicted output failure! These files differ: " + swfOut + ".model.predict.txt" + ", " + swfOut + ".model.txt");
        }

        private void CompareFiles(string predicted, string output, string errorMessage)
        {
            /* I really want to be able to do this, and it does work on VS2010. Unfortunately on VS2010
             * Express, StringBuilder.AppendLine seems to use a different line ending, so the files end up different lengths
             * and this isn't simple to fix. I wish I knew why I can have 2 Windows boxes, one of which appends
             * in Windows format, the other in Unix. */
            //Assert.AreEqual(
            //        new FileInfo(predicted).Length,
            //        new FileInfo(output).Length,
            //        errorMessage);

            using (StreamReader predIn = new StreamReader(predicted))
            using (StreamReader outIn = new StreamReader(output))
            {
                string pline;
                do
                {
                    pline = predIn.ReadLine();
                    Assert.AreEqual(pline, outIn.ReadLine(), errorMessage);
                } while (pline != null);
            }
        }

        /// <summary>
        /// Takes a SWF and turns it into a string representation of the in-memory model so
        /// that it can be dumped to disk and inspected.
        /// </summary>
        /// <param name="swf">The SWF to dump</param>
        /// <returns>A string. It might be a very very long string, just so's
        /// you know.</returns>
        private string SwfToString(SWF swf)
        {
            StringBuilder sb = new StringBuilder();
            swf.ToStringModelView(0, sb);
            return sb.ToString();
        }

        /// <summary>
        /// Tests a job XML with a comprehensive set of features in it.
        /// </summary>
        [TestMethod]
        public void TestForwardReference()
        {
            Swiffotron swiffotron;
            PredictedOutputTest(@"TestForwardReference.xml", @"TestForwardReference.swf", out swiffotron);

            AssertProcessingOrder(new string[] {
                @"myswf1",
                @"myswf2",
                @"myswf"
            }, swiffotron.processingList_accessor);
        }

        private void CopyStoreToTestDir(MockStore store)
        {
            foreach (string commit in store.Commits)
            {
                if (store.Has(commit)) /* Well it might have been deleted */
                {
                    using (Stream input = store.OpenInput(commit))
                    using (FileStream output = new FileStream(TestDir + commit, FileMode.Create))
                    {
                        CopyStream(input, output);
                    }
                }
            }
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[32768];
            while (true)
            {
                int read = input.Read(buffer, 0, buffer.Length);

                if (read <= 0)
                {
                    return;
                }

                output.Write(buffer, 0, read);
            }
        }

        private void AssertProcessingOrder(string[] expected, List<XPathNavigator> list)
        {
            Assert.AreEqual(expected.Length, list.Count, @"Processing order log is the wrong length");

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], list[i].GetAttribute(@"id", string.Empty).ToString(), @"Processing order incorrect");
            }
        }


        private void AssertCacheLog(string[] expected, string[] log)
        {
            Assert.AreEqual(expected.Length, log.Length, @"Cache log is the wrong length");

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], log[i], @"Cache log unexpected");
            }
        }

        #region SwiffotronReadLogHandler Members

        public void OnSwiffotronReadSWF(string name, SWF swf, string log)
        {
            while (swfReadLogs.ContainsKey(name))
            {
                name = name + "_1";
            }
            swfReadLogs[name] = log;

            StringBuilder sbModel = new StringBuilder();
            swf.ToStringModelView(0, sbModel);

            swfReadModelLogs[name] = sbModel.ToString();
        }

        #endregion

        public void CheckCommits(Dictionary<string, byte[]> commits)
        {
            foreach (string key in commits.Keys)
            {
                byte[] data = commits[key];

                if (key.ToLower().EndsWith(".swf"))
                {
                    string swfDump = null;
                    try
                    {
                        using (MemoryStream ms = new MemoryStream(data))
                        using (SWFReader reader = new SWFReader(ms, new SWFReaderOptions() { StrictTagLength = true }, null, null))
                        {
                            swfDump = SwfToString(reader.ReadSWF(new SWFContext(key)));
                        }
                    }
                    finally
                    {
                        lastCommitModelOutput = TestDir + key + ".model.txt";
                        using (FileStream fs = new FileStream(lastCommitModelOutput, FileMode.Create))
                        {
                            if (swfDump != null)
                            {
                                byte[] modeldata = new ASCIIEncoding().GetBytes(swfDump.ToString().Replace("\r", ""));
                                fs.Write(modeldata, 0, modeldata.Length);
                            }
                        }
                    }
                }
                else if (key.ToLower().EndsWith(".png"))
                {
                    /* TODO: Check that a real PNG was committed. */
                }
                else if (key.ToLower().EndsWith(".svg"))
                {
                    /* TODO: Check that a real SVG was committed. */
                }
                else
                {
                    Assert.Fail("For unit tests, a file extension is required on output keys");
                }
            }
        }

        /// <summary>
        /// Tests a job which produces a PNG file.
        /// </summary>
        [TestMethod]
        public void TestPNGOut()
        {
            Swiffotron swiffotron;
            ImageOutputTest(@"TestPNGOut.xml", @"TestPNGOut.png", out swiffotron);
        }

        /// <summary>
        /// Tests a job which produces an SVG file.
        /// </summary>
        [TestMethod]
        public void TestSVGOut()
        {
            Swiffotron swiffotron;
            ImageOutputTest(@"TestSVGOut.xml", @"TestSVGOut.svg", out swiffotron);
        }
    }
}
