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
