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
        /// <summary>
        /// The offset where the 'try' begins
        /// </summary>
        public OpcodeOffset From { get; set; }

        /// <summary>
        ///  The offset where the 'try' ends
        /// </summary>
        public OpcodeOffset To { get; set; }

        /// <summary>
        /// The offset to jump to if an exception is thrown.
        /// </summary>
        public OpcodeOffset Target { get; set; }

        /// <summary>
        /// The type of objects caught. "*" means everything.
        /// </summary>
        public Multiname CatchType { get; set; }

        /// <summary>
        /// The name of the variable that is assigned the exception object.
        /// </summary>
        public Multiname VarName { get; set; }

        /// <summary>
        /// Is this a catch-all block?
        /// </summary>
        public bool IsCatchAll
        {
            get
            {
                return this.CatchType == Multiname.GlobalMultiname;
            }
        }
    }
}
