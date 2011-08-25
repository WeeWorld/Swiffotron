//-----------------------------------------------------------------------
// MockStore.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using SWFProcessing.Swiffotron.IO;

    public class MockStore : ISwiffotronStore
    {
        private string initString;

        private Dictionary<string, FileStream> storedData;

        private HashSet<string> commited;

        public IEnumerable<string> Commits
        {
            get { return this.commited.Select(o => o.Substring(6)); }
        }

        /// <summary>
        /// The config must pass the string 'mock' for this mock
        /// object to be satisfied that it's initialised properly.
        /// </summary>
        public bool InitialisedProperly
        {
            get { return this.initString == "mock"; }
        }

        public void Initialise(string init)
        {
            this.initString = init;
            this.storedData = new Dictionary<string, FileStream>();
            this.commited = new HashSet<string>();

            Directory.CreateDirectory("store");
        }

        public Stream OpenOutput(string id)
        {
            id = @"store\" + id;

            FileStream fs = new FileStream(id, FileMode.Create);
            this.storedData[id] = fs;
            if (this.commited.Contains(id))
            {
                this.commited.Remove(id);
            }
            return fs;
        }

        public void Commit(string id)
        {
            id = @"store\" + id;

            /* Assume it exists. Null refs will end up failing a unit test,
             * which is what we want to happen. */
            FileStream fs = this.storedData[id];
            fs.Close();
            this.commited.Add(id);
        }

        public Stream OpenInput(string id)
        {
            Stream s = null;
            try
            {
                s = new FileStream(@"store\" + id, FileMode.Open);
            }
            catch
            {
                /* Ignore. Pick this up when s is null in next block. */
            }

            if (s == null)
            {
                string[] names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                s = Assembly.GetExecutingAssembly().GetManifestResourceStream(@"SWFProcessing.Swiffotron.Test.res.store." + id);
                if (s == null)
                {
                    s = Assembly.GetExecutingAssembly().GetManifestResourceStream(@"SwiffotronTestExpress.res.store." + id);
                }

                if (s == null)
                {
                    throw new FileNotFoundException(@"Store does not contain " + id);
                }
            }

            return s;
        }



        public bool Has(string id)
        {
            id = @"store\" + id;

            if (File.Exists(id))
            {
                if (this.commited.Contains(id))
                {
                    return true;
                }

                /* Oh dear.. the unit test will want to know about this... */
                throw new ApplicationException(@"Data was written but not commited.");
            }

            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    @"SWFProcessing.Swiffotron.Test.res.store." + id);

            return s != null;
        }
    }
}
