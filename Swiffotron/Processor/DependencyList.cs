//-----------------------------------------------------------------------
// DependencyList.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.Processor
{
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.XPath;

    /// <summary>
    /// Maps nodes in the XML to their dependency nodes for the purposes of working out
    /// what to do first.
    /// </summary>
    internal class DependencyList
    {
        /// <summary>
        /// Initializes a new instance of the DependencyList class.
        /// </summary>
        /// <param name="n">A navigator node</param>
        /// <param name="d">The current list of dependencies</param>
        public DependencyList(XPathNavigator n, List<XPathNavigator> d)
        {
            this.Node = n;
            this.Dependencies = d;
        }

        /// <summary>
        /// Gets or sets the node for which we have a list of dependencies.
        /// </summary>
        public XPathNavigator Node { get; set; }

        /* I want Dependencies to be a HashSet, but because two different navigators that
         * point to the same node are not equal, we need to just do things the hard way
         * and slog through lists. If you're in a refactoring mood, you might want to
         * create a navigator wrapper that allows it to be put into hashes. */

        /// <summary>
        /// Gets the list of dependencies.
        /// </summary>
        public List<XPathNavigator> Dependencies { get; private set; }

        /// <summary>
        /// Gets the number of dependencies.
        /// </summary>
        public int Count
        {
            get { return (this.Dependencies == null) ? 0 : this.Dependencies.Count; }
        }

        /// <summary>
        /// Add a new dependency for this node.
        /// </summary>
        /// <param name="dep">The dependency node.</param>
        public void AddDependency(XPathNavigator dep)
        {
            if (this.Dependencies == null)
            {
                this.Dependencies = new List<XPathNavigator>();
            }
            else
            {
                foreach (XPathNavigator d in this.Dependencies)
                {
                    if (d.ComparePosition(dep) == XmlNodeOrder.Same)
                    {
                        return;
                    }
                }
            }

            this.Dependencies.Add(dep);
        }

        /// <summary>
        /// Remove a dependency from this node.
        /// </summary>
        /// <param name="dep">The dependency to remove.</param>
        public void RemoveDependency(XPathNavigator dep)
        {
            if (this.Dependencies == null)
            {
                return;
            }
            else
            {
                List<XPathNavigator> newDeps = new List<XPathNavigator>();
                foreach (XPathNavigator d in this.Dependencies)
                {
                    if (d.ComparePosition(dep) != XmlNodeOrder.Same)
                    {
                        newDeps.Add(d);
                    }
                }

                this.Dependencies = newDeps;
            }
        }
    }
}
