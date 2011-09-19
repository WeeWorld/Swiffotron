//-----------------------------------------------------------------------
// SwiffotronTest.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.Test
{
#if(EXPRESS2010)
    using ExpressTest.UnitTesting;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using SWFProcessing.Swiffotron.Processor;
#endif

    /// <summary>
    /// Tests for things that don't yet exist, and aren't likely to exist any time soon.
    /// Please run this now and again and ponder upon the possibilities that lay before
    /// you.
    /// </summary>
    [TestClass]
    public class BlueSkyTests : TestingBaseClass
    {
        private bool ENABLED = false; /* Shady way to do this really. Set to true to see the blue-sky tests and
                                       * inevitably, by definition, fail. */

        /// <summary>
        /// Tests a job which produces a PNG file.
        /// </summary>
        [TestMethod]
        public void TestPNGOut()
        {
            if (!ENABLED)
            {
                return;
            }

            Swiffotron swiffotron;

            MockStore store;
            MockCache cache;
            swiffotron = CreateMockSwiffotron(out store, out cache);

            Process(swiffotron, @"TestPNGOut.xml");
        }

        private void Process(Swiffotron swiffotron, string name)
        {
            swiffotron.Process(ResourceAsStream(name), null, null, null, null, null);
        }

        /// <summary>
        /// Tests a job which produces a video file.
        /// </summary>
        [TestMethod]
        public void TestVidOut()
        {
            if (!ENABLED)
            {
                return;
            }

            Swiffotron swiffotron;

            MockStore store;
            MockCache cache;
            swiffotron = CreateMockSwiffotron(out store, out cache);

            Process(swiffotron, @"TestVidOut.xml");
        }
    }
}
