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
#endif

    /// <summary>
    /// Tests for things that don't yet exist, and aren't likely to exist any time soon.
    /// Please run this now and again and ponder upon the possibilities that lay before
    /// you.
    /// </summary>
    [TestClass]
    public class BlueSkyTests : TestingBaseClass
    {
        private bool ENABLED = false; /* TODO: Shady way to do this really. */

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

            /* TODO: Write a new commit listener that looks for PNG files and pass it in here. */

            swiffotron.Process(ResourceAsStream(@"TestPNGOut.xml"), null, null, null, null, null);
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

            /* TODO: Write a new commit listener that looks for video files and pass it in here. */

            swiffotron.Process(ResourceAsStream(@"TestVidOut.xml"), null, null, null, null, null);
        }
    }
}
