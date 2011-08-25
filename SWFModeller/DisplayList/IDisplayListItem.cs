//-----------------------------------------------------------------------
// IDisplayListItem.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller
{
    using SWFProcessing.SWFModeller.Modelling;

    /// <summary>
    /// Since we need to check the type of objects implementing this interface quite often,
    /// we make the implementation expose this code so that we can test it with a more efficient
    /// switch statement instead.
    /// </summary>
    public enum DisplayListItemType
    {
        /// <summary>Place an object on the timeline.</summary>
        PlaceObjectX,

        /// <summary>Remove an object from the timeline.</summary>
        RemoveObjectX
    }

    /// <summary>
    /// An entry on a frame's display list. Marker interface.
    /// </summary>
    public interface IDisplayListItem
    {
        /// <summary>
        /// The type of display list instruction, e.g. placing or removing a
        /// clip from the stage.
        /// </summary>
        DisplayListItemType Type { get; }

        /// <summary>
        /// The layer on which this display list instruction operates.
        /// </summary>
        Layer Layer { get; set; }

        /// <summary>
        /// This creates a clone of this display list item. Useful when cloning one
        /// Timeline to create another.
        /// </summary>
        /// <param name="layer">The cloned DLI can't point at the same layer as its
        /// clone, because it's normally intended for a different timeline. Pass
        /// in a new layer.</param>
        /// <returns>A clone of this DLI.</returns>
        IDisplayListItem Clone(Layer layer, bool rename);
    }
}
