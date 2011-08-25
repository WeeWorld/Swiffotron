//-----------------------------------------------------------------------
// FunctionalTrait.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC.Code
{
    /// <summary>
    /// Convenient marker type for identifying traits that encapsulate Methods, i.e.
    /// FunctionTraits and MethodTraits
    /// </summary>
    public abstract class FunctionalTrait : Trait
    {
        /// <summary>
        /// The actual function
        /// </summary>
        public Method Fn { get; set; }
    }
}
