//-----------------------------------------------------------------------
// SlotTrait.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC.Code
{
    using System.Text;

    /// <summary>
    /// A trait which is a slot on an object
    /// </summary>
    public class SlotTrait : Trait, IHasSlotID
    {
        public Multiname TypeName { get; set; }

        public ConstantKind ValKind { get; set; }

        public object Val { get; set; }

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
            sb.AppendLine(indent + "[" + this.Name + "," + this.Kind + "] " + modifiers + "property type : [" + this.TypeName + "], val: " + this.ValKind);
#endif
        }
    }
}
