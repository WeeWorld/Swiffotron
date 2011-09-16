//-----------------------------------------------------------------------
// ClassTrait.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC.Code
{
    using System.Text;

    /// <summary>
    /// A class reference trait on an object
    /// </summary>
    public class ClassTrait : Trait, IHasSlotID
    {
        /// <summary>
        /// Index, not a class ref coz the refs aren't available when we create these things.
        /// </summary>
        public AS3ClassDef As3class;

        /// <summary>
        /// Numeric index of a property slot on an object.
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
            if (modifiers.Length > 0)
            {
                modifiers += " ";
            }
            string indent = new string(' ', nest * 4);
            sb.AppendLine(indent + "[" + this.Name + "," + this.Kind + "] " + modifiers + "class trait : " + this.As3class.Name);
#endif
        }
    }
}
