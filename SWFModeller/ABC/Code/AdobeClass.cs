//-----------------------------------------------------------------------
// AdobeClass.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller.ABC.Code
{
    /// <summary>
    /// Dummy class that represents flash player classes, such as flash.display.MovieClip
    /// </summary>
    public class AdobeClass : AS3Class
    {
        /// <summary>
        /// Creates the MovieClip class. Or something that looks like it from the outside.
        /// </summary>
        /// <param name="abc">Where to put the namespace</param>
        /// <returns>The MovieClip class.</returns>
        public static AdobeClass CreateFlashDisplayMovieClip(AbcCode abc)
        {
            Namespace nsFlashDisplay = abc.CreateNamespace(Namespace.NamespaceKind.Package, "flash.display");
            Multiname mnMovieClip = abc.CreateMultiname(Multiname.MultinameKind.QName, "MovieClip", nsFlashDisplay, null);

            return new AdobeClass()
            {
                Name = mnMovieClip
            };
        }
    }
}
