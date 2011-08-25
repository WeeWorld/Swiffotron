//-----------------------------------------------------------------------
// SWFValues.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWFModeller
{
    /// <summary>
    /// Tag codes, as specified by Adobe's spec.
    /// </summary>
    public enum Tag
    {
        /// <summary>Internal kludge for debugging.</summary>
        None = -1,

        /// <summary>End marker</summary>
        End = 0,

        /// <summary>Frame end marker</summary>
        ShowFrame = 1,

        /// <summary>Shape definition</summary>
        DefineShape = 2,

        /// <summary>Place object display list instruction</summary>
        PlaceObject = 4,

        /// <summary>Remove object display list instruction</summary>
        RemoveObject = 5,

        /// <summary>A JPEG image - only the image data part. The encoding table will be
        /// preceeding in a JPEGTables tag.</summary>
        DefineBits = 6,

        /// <summary>Shared JPEG encoding tables block</summary>
        JPEGTables = 8,

        /// <summary>Stage colour instruction</summary>
        SetBackgroundColor = 9,

        /// <summary>Simple static text</summary>
        DefineText = 11,

        /// <summary>Ye olde script tag</summary>
        DoAction = 12,

        /// <summary>Audio instruction</summary>
        StartSound = 15,

        /// <summary>Audio stream marker</summary>
        SoundStreamHead = 18,

        /// <summary>Audio stream data</summary>
        SoundStreamBlock = 19,

        /// <summary>Simple bitmap data</summary>
        DefineBitsLossless = 20,

        /// <summary>A JPEG image. Except it might be a PNG. Or a GIF89. Really.
        /// Oh flash, you senseless buffoon.</summary>
        DefineBitsJPEG2 = 21,

        /// <summary>Shape definition v2</summary>
        DefineShape2 = 22,

        /// <summary>Is the SWF protected?</summary>
        Protect = 24,

        /// <summary>Place object display list instruction v2</summary>
        PlaceObject2 = 26,

        /// <summary>Remove object display list instruction v2</summary>
        RemoveObject2 = 28,

        /// <summary>Shape definition v3</summary>
        DefineShape3 = 32,

        /// <summary>Simple static text with alpha</summary>
        DefineText2 = 33,

        /// <summary>Simple bitmap data with alpha</summary>
        DefineBitsLossless2 = 36,

        /// <summary>An edit text field</summary>
        DefineEditText = 37,

        /// <summary>Sprite definition</summary>
        DefineSprite = 39,

        /// <summary>Text label on a frame</summary>
        FrameLabel = 43,

        /// <summary>Audio stream marker v2</summary>
        SoundStreamHead2 = 45,

        /// <summary>Morphable shape definition</summary>
        DefineMorphShape = 46,

        /// <summary>Instructs the player to enable the debugger on this SWF</summary>
        EnableDebugger2 = 64,

        /// <summary>Script limitations, e.g. stack requirements</summary>
        ScriptLimits = 65,

        /// <summary>File attributes</summary>
        FileAttributes = 69,

        /// <summary>Place object display list instruction v3</summary>
        PlaceObject3 = 70,

        /// <summary>Font pixel alignment information</summary>
        DefineFontAlignZones = 73,

        /// <summary>Defines a font</summary>
        DefineFont3 = 75,

        /// <summary>Bind code to clips</summary>
        SymbolClass = 76,

        /// <summary>XML Metadata</summary>
        Metadata = 77,

        /// <summary>Some ABC bytecode</summary>
        DoABC = 82,

        /// <summary>Shape definition v4</summary>
        DefineShape4 = 83,

        /// <summary>Morphable shape definition v2</summary>
        DefineMorphShape2 = 84,

        /// <summary>Scene/frame label table at the start of the file</summary>
        DefineSceneAndFrameLabelData = 86,

        /// <summary>Copyright info for a font.</summary>
        DefineFontName = 88
    }

    /// <summary>
    /// Values useful in understanding a SWF
    /// </summary>
    public abstract class SWFValues
    {
        /// <summary>
        /// Twips per pixel
        /// </summary>
        public const int TwipsFactor = 20;
    }
}
