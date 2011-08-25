using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace WeeWorld.SWFProcessing.SWFModeller
{
    /// <summary>
    /// A shape object that can be placed onto the stage.
    /// </summary>
    public interface Shape
    {
        /// <summary>
        /// A shape should have a bounding box.
        /// </summary>
        /// <returns>A bounding rectangle.</returns>
        Rect GetBounds();

        /// <summary>
        /// TODO: What's the difference between bounds and edge bounds? See
        /// spec.
        /// </summary>
        /// <returns>A bounding rectangle.</returns>
        Rect GetEdgeBounds();
    }
}
