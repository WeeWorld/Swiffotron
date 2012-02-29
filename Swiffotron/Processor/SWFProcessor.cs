//-----------------------------------------------------------------------
// SWFProcessor.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.Swiffotron.Processor
{
    using SWFProcessing.SWFModeller;
    using SWFProcessing.SWFModeller.Characters;
    using SWFProcessing.SWFModeller.Characters.Geom;

    /// <summary>
    /// Support class for Swiffotron-oriented SWF manipulation tasks
    /// </summary>
    internal class SWFProcessor
    {
        private SwiffotronContext Context;

        public SWFProcessor(SwiffotronContext context)
        {
            this.Context = context;
        }

        /// <summary>
        /// Find a list of characters that match a qname pattern. If nothing is found, an
        /// exception is thrown.
        /// </summary>
        /// <param name="qname">The qname to find.</param>
        /// <param name="swf">The SWF to search.</param>
        /// <param name="patternPermitted">If this is false, the returned array will have 1 element.</param>
        /// <returns>An array of characters matching the qname or qname pattern.</returns>
        public Sprite[] SpritesFromQname(string qname, SWF swf, bool patternPermitted)
        {
            /* ISSUE 62: If qname is a pattern, we should return more than one character. */
            /* ISSUE 62: If qname is a pattern, and patternPermitted is false, throw a wobbler. */

            PlaceObject po = swf.LookupInstance(qname);

            /* ISSUE 63: There is a question of whether to error if the instance is not found. Some are
             * found with a pattern rather than a path, and you may not expect it to always find something. 
             * At the moment, we shall throw an exception, because it suits our development, unit testing
             * fail-fast strictness. */
            if (po == null)
            {
                throw new SwiffotronException(
                        SwiffotronError.BadPathOrID,
                        this.Context.Sentinel("FindSpriteByQName"),
                        @"Instance not found: " + qname);
            }

            Sprite sprite = po.Character as Sprite;

            if (sprite == null)
            {
                throw new SwiffotronException(
                        SwiffotronError.BadPathOrID,
                        this.Context,
                        @"Instance does not point to sprite: " + qname);
            }

            return new Sprite[] { sprite };
        }


        /// <summary>
        /// Gets the transform position of an instance.
        /// </summary>
        /// <param name="qname">Fully qualified name of an instance.</param>
        /// <param name="swf">The SWF to search in/</param>
        /// <returns>A copy of the instance's position matrix.</returns>
        public Matrix PositionFromQname(string qname, SWF swf)
        {
            PlaceObject po = swf.LookupInstance(qname);
            return po.Matrix.Copy();
        }
    }
}
