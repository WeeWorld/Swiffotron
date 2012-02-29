//-----------------------------------------------------------------------
// Extern.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.Processor
{
    using SWFProcessing.Swiffotron.IO;
    using System;
    using System.Reflection;
    using System.Runtime.Remoting;

    internal class Extern
    {

        /// <summary>
        /// Creates a cache object by name, as specified in the config file.
        /// </summary>
        /// <param name="name">The name for this cache that will be referenced in
        /// any swiffotron XML files.</param>
        /// <param name="assembly">The fully qualified assembly name. Pass an empty
        /// string or null to reference the currently executing assembly and use
        /// any default implementors.</param>
        /// <param name="classname">The fully qualified class name</param>
        /// <param name="init">An initialisation string passed in the call to Initialise
        /// on the new cache object.</param>
        public static ISwiffotronCache CreateCache(string name, string assembly, string classname, string init)
        {
            if (assembly == string.Empty)
            {
                /* Shortcut value to say "Look in the executing assembly" */
                assembly = null;
            }

            ISwiffotronCache newCache = null;

            if (assembly != null && assembly.ToLower().EndsWith(".dll"))
            {
                /* Load from arbitrary DLL */

                Type type = Assembly.LoadFrom(assembly).GetType(classname);

                /* Class cast problems just get thrown upwards and destroy the app */
                newCache = (ISwiffotronCache)Activator.CreateInstance(type);
            }
            else
            {
                /* Load by named assembly */

                /* Class cast problems just get thrown upwards and destroy the app */
                ObjectHandle oh = Activator.CreateInstance(assembly, classname);
                newCache = (ISwiffotronCache)oh.Unwrap();
            }

            newCache.Initialise(init);

            return newCache;
        }


        /// <summary>
        /// Creates a store object by name, as specified in the config file.
        /// </summary>
        /// <param name="name">The name for this store that will be referenced in
        /// any swiffotron XML files.</param>
        /// <param name="assembly">The fully qualified assembly name. Pass an empty
        /// string or null to reference the currently executing assembly and use
        /// any default implementors.</param>
        /// <param name="classname">The fully qualified class name</param>
        /// <param name="init">An initialisation string passed in the call to Initialise
        /// on the new store object.</param>
        public static ISwiffotronStore CreateStore(string name, string assembly, string classname, string init)
        {
            if (assembly == string.Empty)
            {
                /* Shortcut value to say "Look in the executing assembly" */
                assembly = null;
            }

            ISwiffotronStore newStore = null;

            if (assembly != null && assembly.ToLower().EndsWith(".dll"))
            {
                /* Load from arbitrary DLL */

                Type type = Assembly.LoadFrom(assembly).GetType(classname);

                /* Class cast problems just get thrown upwards and destroy the app */
                newStore = (ISwiffotronStore)Activator.CreateInstance(type);
            }
            else
            {
                /* Load by named assembly */

                /* Class cast problems just get thrown upwards and destroy the app */
                ObjectHandle oh = Activator.CreateInstance(assembly, classname);
                newStore = (ISwiffotronStore)oh.Unwrap();
            }

            newStore.Initialise(init);

            return newStore;
        }

    }
}
