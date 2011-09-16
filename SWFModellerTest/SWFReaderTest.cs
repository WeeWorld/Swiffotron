//-----------------------------------------------------------------------
// SWFReaderTest.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Test
{
    using System.IO;
    using System.Reflection;
    using System.Text;
#if(EXPRESS2010)
    using ExpressTest.UnitTesting;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using SWFProcessing.SWFModeller.ABC;
    using SWFProcessing.SWFModeller.ABC.Debug;
    using SWFProcessing.SWFModeller.ABC.IO;
    using SWFProcessing.SWFModeller.IO;
    using System;
    using SWFProcessing.SWFModeller.Process;

    /// <summary>
    /// This is a test class for SWFReaderTest and is intended
    /// to contain all SWFReaderTest Unit Tests
    /// </summary>
    [TestClass()]
    public class SWFReaderTest : IABCLoadInterceptor
    {
        private string TestDir;

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext)
        // {
        // }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup()
        // {
        // }
        //
        // Use TestInitialize to run code before running each test
        // [TestInitialize()]
        // public void MyTestInitialize()
        // {
        // }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup()
        // {
        // }
        #endregion


        /// <summary>
        /// Called before each test.
        /// </summary>
        [TestInitialize]
        public void InitilizeTests()
        {
            this.TestDir = this.TestContext.TestDir + @"\Out\" +
                    this.GetType().Name + @"." + this.TestContext.TestName + @"\";

            Directory.CreateDirectory(this.TestDir);
        }

        public void OnLoadAbc(bool lazyInit, SWFContext ctx, string abcName, int doAbcCount, byte[] bytecode)
        {
            string abcDir = this.TestDir + @"\abc\";
            Directory.CreateDirectory(abcDir);

            string name = ctx.Name + "." + abcName + doAbcCount + abcName + ".abc";

            using (FileStream fs = new FileStream(abcDir + name, FileMode.Create))
            {
                fs.Write(bytecode, 0, bytecode.Length);
            }

            StringBuilder readLog = new StringBuilder();
            AbcCode code = null;

            try
            {
                code = new AbcReader().Read(bytecode, readLog);
            }
            catch (Exception e)
            {
                readLog.Append(e.Message);
                throw;
            }
            finally
            {
                using (FileStream fs = new FileStream(abcDir + name + ".readlog.txt", FileMode.Create))
                {
                    byte[] readLogBytes = new ASCIIEncoding().GetBytes(readLog.ToString());
                    fs.Write(readLogBytes, 0, readLogBytes.Length);
                }
            }

            StringBuilder abcd = new StringBuilder();
            code.ToStringModelView(0, abcd);
            byte[] dasmBytes = new ASCIIEncoding().GetBytes(abcd.ToString());

            using (FileStream fs = new FileStream(abcDir + name + ".txt", FileMode.Create))
            {
                fs.Write(dasmBytes, 0, dasmBytes.Length);
            }
        }

        /// <summary>
        /// A test for ReadSWF
        /// </summary>
        [TestMethod()]
        public void ReadSWFTestBlobs()
        {
            ResaveWithPredictedOutput("ReadSWFTestBlobs", false);
        }

        /// <summary>
        /// A test for ReadSWF
        /// </summary>
        [TestMethod()]
        public void ReadSWFHasBool()
        {
            ResaveWithPredictedOutput("ReadSWFHasBool", false);
        }
        
        /// <summary>
        /// A test for shape tweens with bitmaps in them.
        /// </summary>
        [TestMethod()]
        public void ReadShapeTweenWithBitmaps()
        {
            ResaveWithPredictedOutput("ReadShapeTweenWithBitmaps", false);
        }

        /// <summary>
        /// A test for ReadSWF
        /// </summary>
        [TestMethod()]
        public void ReadSWFTestTween()
        {
            ResaveWithPredictedOutput("ReadSWFTestTween", false);
        }

        /// <summary>
        /// A test for some avatar accessories that crashed the parser. Grr.
        /// </summary>
        [TestMethod()]
        public void ReadAccessories()
        {
            ResaveWithPredictedOutput("ReadAccessories", false);
        }

        /// <summary>
        /// A test for the simplest possible SWF with nothing in it but a red background.
        /// </summary>
        [TestMethod()]
        public void ReadSimpleSWFTest()
        {
            ResaveWithPredictedOutput("ReadSimpleSWFTest", false);
        }

        /// <summary>
        /// Read a SWF that has a bunch of sprites with different timelines.
        /// </summary>
        [TestMethod()]
        public void ReadTimelineSWFTest()
        {
            ResaveWithPredictedOutput("ReadTimelineSWFTest", false);
        }

        /// <summary>
        /// Read a SWF that has a blue circle moving to the right.
        /// </summary>
        [TestMethod()]
        public void ReadBlueCircle()
        {
            ResaveWithPredictedOutput("ReadBlueCircle", false);
        }

        /// <summary>
        /// Read a SWF that has a complicated geometry.
        /// </summary>
        [TestMethod()]
        public void ReadComplexShape()
        {
            ResaveWithPredictedOutput("ReadComplexShape", false);
        }

        [TestMethod()]
        public void AssembleRedCircle()
        {
            ResaveWithPredictedOutput(@"AssembleRedCircle", true);
        }

        /// <summary>
        /// Read a SWF that has a lot of deeply nested movie clips.
        /// </summary>
        [TestMethod()]
        public void ReadDeepNestedSWFTest()
        {
            ResaveWithPredictedOutput("ReadDeepNestedSWFTest", false);
        }

        /// <summary>
        /// Read a SWF that has a series of clips on the stage, each with its own
        /// class with different options set.
        /// </summary>
        [TestMethod()]
        public void ReadClassBindingTest()
        {
            ResaveWithPredictedOutput(@"ReadClassBindingTest", false);
        }

        [TestMethod()]
        public void ReadRedCircleTest()
        {
            ResaveWithPredictedOutput(@"ReadRedCircleTest", false);
        }

        [TestMethod()]
        public void ReadSwitch()
        {
            ResaveWithPredictedOutput(@"ReadSwitch", false);
        }

        [TestMethod()]
        public void AssembleSwitch()
        {
            ResaveWithPredictedOutput(@"AssembleSwitch", true);
        }

        [TestMethod()]
        public void ReadExceptions()
        {
            ResaveWithPredictedOutput(@"ReadExceptions", false);
        }

        [TestMethod()]
        public void ReadText()
        {
            ResaveWithPredictedOutput(@"ReadText", false);
        }

        [TestMethod()]
        public void ReadDefineShape2()
        {
            ResaveWithPredictedOutput(@"ReadDefineShape2", false);
        }

        [TestMethod()]
        public void ReadFonts()
        {
            ResaveWithPredictedOutput(@"ReadFonts", false);
        }

        [TestMethod()]
        public void AccessModifiers()
        {
            ResaveWithPredictedOutput(@"AccessModifiers", true);
        }

        [TestMethod()]
        public void ReadImages()
        {
            ResaveWithPredictedOutput(@"ReadImages", false);
        }

        [TestMethod()]
        public void ReadKeyframes()
        {
            ResaveWithPredictedOutput(@"ReadKeyframes", false);
        }

        [TestMethod()]
        public void ReadSwiffotronLogo()
        {
            ResaveWithPredictedOutput(@"ReadSwiffotronLogo", false);
        }

        [TestMethod()]
        public void AssembleExceptions()
        {
            ResaveWithPredictedOutput(@"AssembleExceptions", true);
        }

        [TestMethod()]
        public void ReadMulticlassTest()
        {
            ResaveWithPredictedOutput(@"ReadMulticlassTest", true);
        }

        [TestMethod()]
        public void AssembleSimplestTimelineScript()
        {
            ResaveWithPredictedOutput(@"AssembleSimplestTimelineScript", true);
        }

        /// <summary>
        /// Read a SWF that has a series of clips on the stage, each with its own
        /// class with different options set.
        /// </summary>
        [TestMethod()]
        public void ReadBothCirclesTest()
        {
            ResaveWithPredictedOutput(@"ReadBothCirclesTest", false);
        }

        /// <summary>
        /// Loads a SWF, saves it again and then loads what was saved. The final result is compared with
        /// a predicted output file to make sure it contains what it's supposed to contain.
        /// </summary>
        /// <param name="name">The name of the SWF which is retrieved from the resources folder.</param>
        /// <param name="reAssembleCode">If true, the ABC bytecode will be disassembled and re-assembled
        /// again to test the assembler.</param>
        private void ResaveWithPredictedOutput(string name, bool reAssembleCode)
        {
            StringBuilder binDump = new StringBuilder();
            using (SWFReader sr = new SWFReader(this.ResourceAsStream(name + @".swf"), new SWFReaderOptions() { StrictTagLength = true }, binDump, this))
            {
                SWF swf = null;
                try
                {
                    swf = sr.ReadSWF(new SWFContext(name));
                }
                finally
                {
                    using (FileStream fs = new FileStream(this.TestDir + name + @".bin.dump-1.txt", FileMode.Create))
                    {
                        byte[] dumpbindata = new ASCIIEncoding().GetBytes(binDump.ToString());
                        fs.Write(dumpbindata, 0, dumpbindata.Length);
                    }
                }

                Assert.IsNotNull(swf);

                if (reAssembleCode)
                {
                    swf.MarkCodeAsTampered();
                }

                /* Save it out, then reload it to make sure it can survive intact */
                this.SaveAndVerifyPredictedOutput(swf, name, false);
            }
        }

        /// <summary>
        /// Creates a SWF string dump, saves the SWF, re-loads it and compares a new
        /// string dump. If the SWF files are different, it concludes that something
        /// went wrong.
        /// </summary>
        /// <param name="swf">The SWF to test saving/loading</param>
        /// <param name="name">The SWF file and dumps are saved for inspection to
        /// the test output folder under this name.</param>
        private void SaveAndVerifyPredictedOutput(SWF swf, string name, bool compressed)
        {
            string swfDump1 = this.SwfToString(swf);

            using (FileStream fs = new FileStream(this.TestDir + name + @".model.txt", FileMode.Create))
            {
                byte[] dump1data = new ASCIIEncoding().GetBytes(swfDump1);
                fs.Write(dump1data, 0, dump1data.Length);
            }

            StringBuilder writeLog = new StringBuilder();
            StringBuilder abcWriteLog = new StringBuilder();
            SWFWriterOptions opts = new SWFWriterOptions()
            {
                Compressed = compressed,
                EnableDebugger = true
            };

            byte[] swfData = new SWFWriter(swf, opts, writeLog, abcWriteLog).ToByteArray();

            using (FileStream fs = new FileStream(this.TestDir + name + ".writelog.txt", FileMode.Create))
            {
                byte[] writeLogData = new ASCIIEncoding().GetBytes(writeLog.ToString());
                fs.Write(writeLogData, 0, writeLogData.Length);
            }

            Directory.CreateDirectory(this.TestDir + @"\abc\");
            using (FileStream fs = new FileStream(this.TestDir + @"\abc\" + name + ".writelog.txt", FileMode.Create))
            {
                byte[] writeLogData = new ASCIIEncoding().GetBytes(abcWriteLog.ToString());
                fs.Write(writeLogData, 0, writeLogData.Length);
            }

            using (FileStream fs = new FileStream(this.TestDir + name + ".swf", FileMode.Create))
            {
                fs.Write(swfData, 0, swfData.Length);
            }
            StringBuilder binDump = new StringBuilder();

            string swfDump2 = null;
            try
            {
                swfDump2 = this.SwfToString(
                        new SWFReader(
                                new MemoryStream(swfData),
                                new SWFReaderOptions() { StrictTagLength = true },
                                binDump,
                                this)
                            .ReadSWF(new SWFContext("resaved." + name)));
            }
            finally
            {
                using (FileStream fs = new FileStream(this.TestDir + name + ".bin.dump-2.txt", FileMode.Create))
                {
                    byte[] dump2bindata = new ASCIIEncoding().GetBytes(binDump.ToString());
                    fs.Write(dump2bindata, 0, dump2bindata.Length);
                }
            }

            string finalModelFile = this.TestDir + name + ".model-dump-2.txt";
            using (FileStream fs = new FileStream(finalModelFile, FileMode.Create))
            {
                byte[] dump2data = new ASCIIEncoding().GetBytes(swfDump2);
                fs.Write(dump2data, 0, dump2data.Length);
            }

            string predicted = TestDir + name + ".model.predict.txt";
            using (Stream input = ResourceAsStream("predicted." + name + ".txt"))
            using (FileStream output = new FileStream(TestDir + name + ".model.predict.txt", FileMode.Create))
            {
                Assert.IsNotNull(input, "Predicted output is missing! " + name);
                CopyStream(input, output);
            }

            using (StreamWriter acceptScript = new StreamWriter(new FileStream(TestDir + "accept.bat", FileMode.Create)))
            {
                acceptScript.WriteLine("copy \"" + finalModelFile + "\" \"" + new FileInfo("..\\..\\..\\SWFModellerTest\\res\\predicted\\" + name + ".txt").FullName + "\"");
            }

            using (StreamWriter viewScript = new StreamWriter(new FileStream(TestDir + "viewdiff.bat", FileMode.Create)))
            {
                /* TODO: Since nobody other than me is using this code, I'm hard-coding this. Really though, it should be a diff tool env var */
                viewScript.WriteLine("\"c:\\Program Files (x86)\\WinMerge\\WinMergeU.exe\" \"" + finalModelFile + "\" \"" + new FileInfo("..\\..\\..\\SWFModellerTest\\res\\predicted\\" + name + ".txt").FullName + "\"");
            }

            CompareFiles(
                predicted,
                finalModelFile,
                "Predicted output failure! These files differ: " + name + ".model.predict.txt" + ", " + name + ".model.txt");
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[32768];
            while (true)
            {
                int read = input.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    return;
                output.Write(buffer, 0, read);
            }
        }


        private void CompareFiles(string predicted, string output, string errorMessage)
        {
            /* TODO: I really want to be able to do this, and it does work on VS2010. Unfortunately on VS2010
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

        private string SwfToString(SWF swf)
        {
            StringBuilder sb = new StringBuilder();
            swf.ToStringModelView(0, sb);
            return sb.ToString();
        }


        private Stream ResourceAsStream(string sRes)
        {
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("SWFProcessing.SWFModeller.Test.res." + sRes);
            if (s == null)
            {
                s = Assembly.GetCallingAssembly().GetManifestResourceStream("SWFModellerTestExpress.res." + sRes);
            }
            Assert.IsNotNull(s, "Test input missing! "+sRes);
            return s;
        }
    }
}
