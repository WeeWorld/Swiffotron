//-----------------------------------------------------------------------
// Trait.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC.Code
{
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    /// Different types of object trait
    /// </summary>
    public enum TraitKind
    {
        /// <summary>A trait slot</summary>
        Slot = 0,

        /// <summary>A method reference</summary>
        Method = 1,

        /// <summary>A getter method reference</summary>
        Getter = 2,

        /// <summary>A setter method reference</summary>
        Setter = 3,

        /// <summary>A class reference</summary>
        Class = 4,

        /// <summary>A function reference</summary>
        Function = 5,

        /// <summary>A constant value</summary>
        Const = 6
    }

    /// <summary>
    /// A trait (or property) of an object.
    /// </summary>
    public abstract class Trait
    {
        public Multiname Name { get; set; }

        public TraitKind Kind { get; set; }

        /// <summary>
        /// Unit test method present in the debug build. Renders the object as text
        /// that can be written to a diffable output file.
        /// </summary>
        /// <param name="nest">The current nest level in our output</param>
        /// <param name="sb">The buffer that is accumulating output.</param>
        [Conditional("DEBUG")]
        public abstract void ToStringModelView(int nest, StringBuilder sb, string modifiers);
    }
}