//-----------------------------------------------------------------------
// AS3Class.cs
//
//
//-----------------------------------------------------------------------


namespace SWFProcessing.SWFModeller.ABC.Code
{
    public abstract class AS3Class
    {
        /// <summary>Gets or sets the class name</summary>
        public Multiname Name { get; set; }

        /// <summary>The name as a string, i.e. in dotted form qualified by namespace, e.g. com.swiffotron.MyClass</summary>
        public string QualifiedName
        {
            get
            {
                return this.Name.QualifiedName;
            }
        }
    }
}
