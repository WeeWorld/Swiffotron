//-----------------------------------------------------------------------
// CLI.cs
//
//
//-----------------------------------------------------------------------

namespace SwiffotronCLI
{
    using System;
    using System.IO;
    using SWFProcessing.Swiffotron;

    /// <summary>
    /// The main CLI application class. Initially this was meant to be a sample app that
    /// put a front end onto swiffotron features. It has become that, and a kinda proxy for
    /// mstest when developing using Visual Studio express with debug-oriented features.
    /// What would be nice would be a test runner for express. Hey ho.
    /// </summary>
    public class CLI
    {
        /// <summary>
        /// App entry point
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static void Main(string[] args)
        {
            string config;

            string job = ParseArguments(args, out config);
            if (job == null)
            {
                PrintUsage();
                Environment.Exit(-1);
            }

            Swiffotron swiffotron;
            if (config == null)
            {
                swiffotron = new Swiffotron(null);
            }
            else
            {
                using (FileStream cfs = new FileStream(config, FileMode.Open))
                {
                    swiffotron = new Swiffotron(cfs);
                }
            }

            using (FileStream jobfs = new FileStream(job, FileMode.Open))
            {
                swiffotron.Process(jobfs);
            }
        }

        /// <summary>
        /// Prints usage hints so that the user knows what it is that they did to offend us.
        /// </summary>
        private static void PrintUsage()
        {
            Console.WriteLine("S W I F F O T R O N");
            Console.WriteLine("Usage:");
            Console.WriteLine("> swiffotron [-option value?]* <job>");
            Console.WriteLine("Options:");
            Console.WriteLine(" -config <file>; Points to a configuration XML file");
        }

        /// <summary>
        /// Parses arguments assuming the following general CLI form:
        /// [-switch value?]+ job.xml
        /// </summary>
        /// <param name="args">The command line arguments</param>
        /// <param name="config">Output parameter; The config file path</param>
        /// <returns>The file path of the Swiffotron job XML</returns>
        private static string ParseArguments(string[] args, out string config)
        {
            /* Defaults: */
            config = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))
                {
                    switch (args[i])
                    {
                        case "-config":
                            i++;
                            config = args[i];
                            break;

                        default:
                            return null;
                    }
                }
                else
                {
                    if (i == args.Length - 1)
                    {
                        return args[i];
                    }

                    return null;
                }
            }

            return null;
        }
    }
}
