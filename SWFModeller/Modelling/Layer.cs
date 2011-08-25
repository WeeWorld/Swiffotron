//-----------------------------------------------------------------------
// Layer.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.Modelling
{
    /// <summary>
    /// Represents a layer on a timeline.
    /// </summary>
    public class Layer
    {
        /// <summary>
        /// Initializes a new instance of the Layer class and puts it
        /// on the timeline.
        /// </summary>
        /// <param name="timeline">The timeline on which to create
        /// the layer.</param>
        public Layer(Timeline timeline)
        {
            Timeline = timeline;
        }

        /// <summary>
        /// Used in some layer related methods, e.g. "Create me a layer in front of other
        /// layers".
        /// </summary>
        public enum Position
        {
            /// <summary> Specifies a layer in front of others. </summary>
            Front,

            /// <summary> Specifies a layer behind others. </summary>
            Back
        }

        /// <summary>
        /// Gets the timeline to which this layer belongs. A convenience property.
        /// </summary>
        public Timeline Timeline { get; private set; }

        /// <summary>
        /// Renders the layer as a string. Used only in test/debug console output.
        /// </summary>
        /// <returns>A string rendition of this layer.</returns>
        public override string ToString()
        {
            return "[layer]";
        }
    }
}
