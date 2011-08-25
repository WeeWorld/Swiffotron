//-----------------------------------------------------------------------
// ICharacterReference.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.DisplayList
{
    using SWFProcessing.SWFModeller.Characters;

    /// <summary>
    /// Marker interface for objects that reference characters. Supplies a property
    /// with which objects can expose that character. It also has an awesome class name.
    /// </summary>
    public interface ICharacterReference
    {
        ICharacter Character { get; }
    }
}
