//-----------------------------------------------------------------------
// RemoveObject.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using SWFProcessing.SWFModeller.Characters;
    using SWFProcessing.SWFModeller.Modelling;

    /// <summary>
    /// A display list instruction which removes an object at a certain depth.
    /// </summary>
    public class RemoveObject : IDisplayListItem
    {
        /// <summary>
        /// Initializes a new instance of the RemoveObject class as an instruction to
        /// remove the object at a specific depth.
        /// </summary>
        /// <param name="c">The character to remove, or null to remove whatever is
        /// there.</param>
        public RemoveObject(Layer l, ICharacter c)
        {
            this.Layer = l;
            this.Character = c;
        }

        /// <summary>
        /// Initializes a new instance of the RemoveObject class as an instruction to
        /// remove an object at a certain depth, whatever that object may be.
        /// </summary>
        /// <param name="l">The layer to remove an object from.</param>
        public RemoveObject(Layer l)
                : this(l, null)
        {
            /* Nothing more to do */
        }

        public DisplayListItemType Type
        {
            get
            {
                return DisplayListItemType.RemoveObjectX;
            }
        }

        public Layer Layer { get; set; }

        public ICharacter Character { get; set; }

        public int LayerIndex
        {
            get
            {
                return Layer.Timeline.GetLayerIndex(Layer);
            }
        }

        public bool HasCharacter
        {
            get
            {
                return this.Character != null;
            }
        }

        /// <summary>
        /// Unit test method present in the debug build. Renders the object as text
        /// that can be written to a diffable output file.
        /// </summary>
        /// <param name="nest">The current nest level in our output</param>
        /// <param name="sb">The buffer that is accumulating output.</param>
        [Conditional("DEBUG")]
        public void ToStringModelView(int nest, StringBuilder sb)
        {
            string indent = new string(' ', nest * 4);

            sb.Append(indent + "RemoveObject layer=" + Layer);
        }

        public IDisplayListItem Clone(Layer l, bool rename)
        {
            /* Kinda easy this. We don't have any data. */
            return new RemoveObject(l, this.Character);
        }
    }
}
