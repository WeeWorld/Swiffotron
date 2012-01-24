//-----------------------------------------------------------------------
// Script.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC.Code
{
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// A script is a collection of code-related traits such as methods and classes.
    /// </summary>
    public class Script
    {
        private List<Trait> traits;

        public Script()
        {
            this.traits = new List<Trait>();
        }

        public Method Method { get; set; }

        public int TraitCount
        {
            get
            {
                return this.traits.Count;
            }
        }

        public IEnumerator<Trait> Traits
        {
            get
            {
                return this.traits.GetEnumerator();
            }
        }

        public void AddTrait(Trait t)
        {
            this.traits.Add(t);
        }

        internal void ToStringModelView(int nest, StringBuilder sb)
        {
            Method.ToStringModelView(nest, sb, "script");
            foreach (Trait t in this.traits)
            {
                t.ToStringModelView(nest, sb, "script");
            }
        }
    }
}
