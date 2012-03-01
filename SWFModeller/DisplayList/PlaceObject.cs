//-----------------------------------------------------------------------
// PlaceObject.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller
{
    using System.Diagnostics;
    using System.Text;
    using SWFProcessing.ModellingUtils.Geom;
    using SWFProcessing.SWFModeller.Characters;
    using SWFProcessing.SWFModeller.DisplayList;
    using SWFProcessing.SWFModeller.Modelling;

    /// <summary>
    /// A display list instruction that places an item onto the stage.
    /// </summary>
    public class PlaceObject : IDisplayListItem, ICharacterReference
    {
        /// <summary>
        /// Initializes a new instance of the PlaceObject class as a display
        /// list instruction to put an item onto the stage.
        /// </summary>
        public PlaceObject(
                ICharacter character,
                Layer layer,
                int? clipDepth,
                Matrix matrix,
                string name,
                bool isMove,
                ColorTransform cxform,
                string className,
                int? ratio)
        {
            this.Character = character;
            this.ClipDepth = clipDepth;
            this.Matrix = matrix;
            this.Name = name;
            this.IsMove = isMove;
            this.CXForm = cxform;
            this.Layer = layer;
            this.ClassName = className;
            this.Ratio = ratio;
        }

        public DisplayListItemType Type
        {
            get
            {
                return DisplayListItemType.PlaceObjectX;
            }
        }

        public string ClassName { get; private set; }

        public string Name { get; private set; }

        public bool IsMove { get; private set; }

        public int? ClipDepth { get; private set; }

        public Layer Layer { get; set; }

        public Matrix Matrix { get; set; }

        public ColorTransform CXForm { get; private set; }

        public int? Ratio { get; private set; }

        /// <summary>
        /// Gets a flag indicating if this instruction includes a class name
        /// </summary>
        public bool HasClassName
        {
            get
            {
                return this.ClassName != null;
            }
        }

        /// <summary>
        /// Gets a flag indicating if this instruction includes clip actions
        /// </summary>
        public bool HasClipActions
        {
            get
            {
                /* AS3 apps do not have clip actions. */
                return false;
            }
        }

        /// <summary>
        /// Gets a flag indicating if this instruction includes a clip depth
        /// </summary>
        public bool HasClipDepth
        {
            get
            {
                return this.ClipDepth != null;
            }
        }

        /// <summary>
        /// Gets a flag indicating if this instruction includes a name
        /// </summary>
        public bool HasName
        {
            get
            {
                return this.Name != null;
            }
        }

        /// <summary>
        /// Gets a flag indicating if this instruction includes a ratio
        /// </summary>
        public bool HasRatio
        {
            get
            {
                return this.Ratio != null;
            }
        }

        /// <summary>
        /// Gets a flag indicating if this instruction inlcludes a colour transform
        /// </summary>
        public bool HasColorTransform
        {
            get
            {
                return this.CXForm != null;
            }
        }

        /// <summary>
        /// Gets a flag indicating if this instruction inlcludes a colour transform with alpha
        /// </summary>
        public bool HasColorTransformWithAlpha
        {
            get
            {
                return this.CXForm != null && this.CXForm.HasAlpha;
            }
        }

        /// <summary>
        /// Gets a flag indicating if this instruction inlcludes a position matrix
        /// </summary>
        public bool HasMatrix
        {
            get
            {
                return this.Matrix != null;
            }
        }

        /// <summary>
        /// Gets a flag indicating if this instruction inlcludes a character
        /// </summary>
        public bool HasCharacter
        {
            get
            {
                return this.Character != null;
            }
        }

        /// <summary>
        /// Gets the layer index for this instruction
        /// </summary>
        public int LayerIndex
        {
            get
            {
                return this.Layer.Timeline.GetLayerIndex(Layer);
            }
        }

        /// <summary>
        /// Gets the character for this instruction, or null if there is none.
        /// </summary>
        public ICharacter Character { get; private set; }

        public IDisplayListItem Clone(Layer l, bool rename)
        {
            /* ISSUE 19: Can't decide if we want to clone the characters, or clone them if we
             * pass in a flag and leave it to the swiffotron's logic to decide. */

            string name = this.Name;
            if (rename && name != null)
            {
                name += "_copy";
            }

            PlaceObject po = new PlaceObject(
                    this.Character,
                    l,
                    this.ClipDepth,
                    this.Matrix == null ? null : this.Matrix.Copy(),
                    name,
                    this.IsMove,
                    this.CXForm == null ? null : this.CXForm.Clone(),
                    this.ClassName,
                    this.Ratio);

            /* ISSUE 19: Check that the name doesn't exist on the target layer's timeline. Remember the target
             * timeline might be different to the source timeline, so name collisions may or may not happen.
             * you might think passing in 'rename == true' all the time would be a good thing, but actually
             * this just breaks all the bytecode. I'm not entirely sure it's possible to fix that.
             * Best advice.. be careful with your names.
             */

            return po;
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

            sb.Append(indent + "PlaceObject" + (this.IsMove ? " (move)" : string.Empty) +
                    " char=" + (this.Character == null ? "none" : this.Character.ToString()) +
                    ", layer=" + this.Layer +
                    ", name=" + (this.Name == null ? "none" : this.Name) +
                    ", ratio=" + (this.Ratio == null ? "none" : this.Ratio.ToString()) +
                    ", matrix=" + (this.Matrix == null ? "none" : this.Matrix.ToString()) +
                    ", cxform=" + (this.CXForm == null ? "none" : this.CXForm.ToString()) +
                    ", clipDepth=" + (this.ClipDepth == null ? "none" : this.ClipDepth.ToString()));
        }
    }
}
