//-----------------------------------------------------------------------
// FunctionTrait.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC.Code
{
    using System.Text;

    /// <summary>
    /// A function reference trait on an object
    /// </summary>
    public class FunctionTrait : FunctionalTrait, IHasSlotID
    {
        /// <summary>
        /// The slot index for this trait.
        /// </summary>
        public uint SlotID { get; set; }

        /// <summary>
        /// Unit test method present in the debug build. Renders the object as text
        /// that can be written to a diffable output file.
        /// </summary>
        /// <param name="nest">The current nest level in our output</param>
        /// <param name="sb">The buffer that is accumulating output.</param>
        public override void ToStringModelView(int nest, StringBuilder sb, string modifiers)
        {
#if DEBUG
            modifiers = "[" + this.Name + "," + this.Kind + "] " + modifiers;
            this.Fn.ToStringModelView(nest + 1, sb, string.Join(" ", new string[] { modifiers, "function" }));
#endif
        }
    }
}
