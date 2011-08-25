//-----------------------------------------------------------------------
// MethodTrait.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC.Code
{
    using System.Text;

    /// <summary>
    /// A function trait on an object, which is a method
    /// </summary>
    public class MethodTrait : FunctionalTrait
    {
        public Method OverriddenMethod { get; set; }

        /// <summary>
        /// Unit test method present in the debug build. Renders the object as text
        /// that can be written to a diffable output file.
        /// </summary>
        /// <param name="nest">The current nest level in our output</param>
        /// <param name="sb">The buffer that is accumulating output.</param>
        public override void ToStringModelView(int nest, StringBuilder sb, string modifiers)
        {
#if DEBUG
            if (modifiers != string.Empty)
            {
                modifiers += " ";
            }

            string fnName = Fn.Name;
            if (fnName == string.Empty)
            {
                fnName = "(anon)";
            }
            else if (fnName == ABCValues.AnyName)
            {
                fnName = "(anon *)";
            }

            this.Fn.ToStringModelView(
                    nest,
                    sb,
                    "[" + this.Name + "," + this.Kind + "] " + modifiers + "function " + fnName);
#endif
        }
    }
}
