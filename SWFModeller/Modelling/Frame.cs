//-----------------------------------------------------------------------
// Frame.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
#if DEBUG
    using SWFProcessing.SWFModeller.Debug;
#endif

    /// <summary>
    /// A frame on the timeline, which comprises a display list of instructions
    /// detailing what has changed on the stage for this frame.
    /// </summary>
    public class Frame
    {
        /// <summary>
        /// The list of display list items on this frame.
        /// </summary>
        private List<IDisplayListItem> displayList;

        private Dictionary<string, PlaceObject> placeObjectMap;

        /// <summary>
        /// Initializes a new instance of a Frame.
        /// </summary>
        public Frame()
        {
            this.displayList = new List<IDisplayListItem>();
            this.placeObjectMap = new Dictionary<string, PlaceObject>();
        }

        public string Label { get; set; }

        public string SceneName { get; set; }

        public bool HasLabel
        {
            get
            {
                return this.Label != null;
            }
        }

        /// <summary>
        /// Gets an iterable list of display list items.
        /// </summary>
        public IEnumerable<IDisplayListItem> DisplayList
        {
            get
            {
                return this.displayList.AsEnumerable();
            }
        }

        /// <summary>
        /// Add something to the display list. TODO: Rename this method?
        /// </summary>
        /// <param name="dlistItem">The item to add.</param>
        public void AddTag(IDisplayListItem dlistItem)
        {
            this.displayList.Add(dlistItem);

            PlaceObject po = dlistItem as PlaceObject;

            if (po != null && po.Name != null)
            {
                this.placeObjectMap.Add(po.Name, po);
            }
        }

        /// <summary>
        /// Dumps this frame content to a string model for display and
        /// debug inspection.
        /// </summary>
        /// <param name="nest">The nest level for the output</param>
        /// <param name="sb">The string builder being used to build the
        /// model view.</param>
        [Conditional("DEBUG")]
        public void ToStringModelView(int nest, StringBuilder sb)
        {
#if DEBUG
            if (this.HasLabel)
            {
                string indent = new string(' ', nest * 4);

                sb.Append(indent + "Frame Label:" + this.Label + "\n");
            }

            foreach (IDisplayListItem dli in this.displayList)
            {
                DLItemDump.ToStringModelView(dli, nest, sb);
                sb.Append("\n");
            }
#endif
        }

        internal PlaceObject FindInstance(string name)
        {
            if (placeObjectMap.ContainsKey(name))
            {
                return placeObjectMap[name];
            }

            return null;
        }

        internal void RemoveDisplayListItem(IDisplayListItem dli)
        {
            this.displayList.Remove(dli);

            PlaceObject po = dli as PlaceObject;

            if (po != null && po.Name != null)
            {
                this.placeObjectMap.Remove(po.Name);
            }
        }
    }
}
