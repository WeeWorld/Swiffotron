//-----------------------------------------------------------------------
// SwiffotronSVGTest.cs
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
    public class SwiffotronSVGTest : TestingBaseClass
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

        public void CheckCommits(Dictionary<string, byte[]> commits)
        {
            foreach (string key in commits.Keys)
            {
                byte[] data = commits[key];

                if (key.ToLower().EndsWith(".svg"))
                {
                    /* TODO: Check that a real SVG was committed. */
                }
                else
                {
                    Assert.Fail("For unit tests, a file extension is required on output keys");
                }
            }
        }

        private void SvgOutputTest(string xmlIn, string imageOut, out Swiffotron swiffotron)
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
        /// Tests a job which produces an SVG file.
        /// </summary>
        [TestMethod]
        public void TestSVGOut()
        {
            Swiffotron swiffotron;
            SvgOutputTest(@"TestSVGOut.xml", @"TestSVGOut.svg", out swiffotron);
        }
    }
}
