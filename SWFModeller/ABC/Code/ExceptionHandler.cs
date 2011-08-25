//-----------------------------------------------------------------------
// ExceptionHandler.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC.Code
{
    /// <summary>
    /// Closely associated with the Method object. It denotes ranges within the
    /// bytecode that mark try and catch regions.
    /// </summary>
    public class ExceptionHandler
    {
        public OpcodeOffset From { get; set; }
        public OpcodeOffset To { get; set; }
        public OpcodeOffset Target { get; set; }

        /// <summary>
        /// The type of objects caught. "*" means everything.
        /// </summary>
        public Multiname CatchType { get; set; }

        /// <summary>
        /// The name of the variable that is assigned the exception object.
        /// </summary>
        public Multiname VarName { get; set; }

        public bool IsCatchAll { get { return this.CatchType == Multiname.GlobalMultiname; } }
    }
}
