//-----------------------------------------------------------------------
// TestingBaseClass.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.Test
{
    using System.IO;
#if(EXPRESS2010)
    using ExpressTest.UnitTesting;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using System.Reflection;

    public class TestingBaseClass
    {
        protected string TestDir;

        protected Stream ResourceAsStream(string sRes)
        {
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(@"SWFProcessing.Swiffotron.Test.res." + sRes);

            if (s == null)
            {
                s = Assembly.GetCallingAssembly().GetManifestResourceStream("SwiffotronTestExpress.res." + sRes);
            }
            Assert.IsNotNull(s, "Test input missing! " + sRes);
            return s;
        }

        /// <summary>
        /// Creates a swiffotron from the mock config profile and checks that everything is created
        /// correctly.
        /// </summary>
        /// <param name="mockStore">Returns a reference to the private store for testing.</param>
        /// <param name="htCache">Returns a reference to the private cache for testing.</param>
        /// <returns>A private accessor reference to the new Swiffotron.</returns>
        protected Swiffotron CreateMockSwiffotron(out MockStore mockStore, out MockCache htCache)
        {
#if(EXPRESS2010)
            Swiffotron swiffotron = new Swiffotron(ResourceAsStream(@"mock-config-express.xml"));
#else
            Swiffotron swiffotron = new Swiffotron(ResourceAsStream(@"mock-config.xml"));
#endif

            mockStore = (MockStore)swiffotron.conf_accessor.Stores.stores_accessor[@"store"];
            Assert.IsNotNull(mockStore, @"The store object was not created.");
            Assert.IsTrue(mockStore.InitialisedProperly, @"The store was not initialised properly");

            htCache = (MockCache)swiffotron.conf_accessor.Caches.caches_accessor[@"cache"];

            Assert.IsNotNull(htCache, @"The cache object was not created.");

            return swiffotron;
        }

        protected void CopyStoreToTestDir(MockStore store)
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
    }
}
