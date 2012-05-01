//-----------------------------------------------------------------------
// SwiffotronHTMLTest.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.Test
{
#if(EXPRESS2010)
    using ExpressTest.UnitTesting;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// These tests should always pass. Always.
    /// </summary>
    [TestClass]
    public class SwiffotronHTMLTest : TestingBaseClass
    {
        private Dictionary<string, string> swfReadLogs;
        private Dictionary<string, string> swfReadModelLogs;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

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

        public void CheckCommits(Dictionary<string, byte[]> commits)
        {
            foreach (string key in commits.Keys)
            {
                byte[] data = commits[key];

                if (key.ToLower().EndsWith(".html"))
                {
                    /* TODO: Check that a real HTML was committed. */
                }
                else
                {
                    Assert.Fail("For unit tests, a file extension is required on output keys");
                }
            }
        }

        private void HTMLOutputTest(string xmlIn, string imageOut, out Swiffotron swiffotron)
        {
            MockStore store;
            MockCache cache;
            swiffotron = CreateMockSwiffotron(out store, out cache);

            Dictionary<string, byte[]> commits = new Dictionary<string, byte[]>();
            swiffotron.Process(ResourceAsStream(xmlIn), commits, null, null, null, null);
            CheckCommits(commits);

            CopyStoreToTestDir(store);

            Assert.IsTrue(store.Has(imageOut), @"Output was not saved");
        }

        /// <summary>
        /// Tests a job which produces an HTML file.
        /// </summary>
        [TestMethod]
        public void TestHTMLOut()
        {
            Swiffotron swiffotron;
            HTMLOutputTest(@"TestHTMLOut.xml", @"TestHTMLOut.html", out swiffotron);
        }
    }
}
