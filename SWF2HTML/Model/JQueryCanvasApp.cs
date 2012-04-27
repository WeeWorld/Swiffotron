//-----------------------------------------------------------------------
// CanvasApp.cs
//
//
//-----------------------------------------------------------------------

namespace SWFProcessing.SWF2HTML.Model
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using SWFProcessing.SWF2HTML.IO;
    using SWFProcessing.SWFModeller;
    using SWFProcessing.SWFModeller.Characters;
    using SWFProcessing.SWFModeller.Characters.Shapes;
    using SWFProcessing.SWFModeller.Modelling;

    class JQueryCanvasApp
    {
        private HTMLAssist html;

        private static string JQueryPlayerScript;

        static JQueryCanvasApp()
        {
            List<byte> bytes = new List<byte>();
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("SWFProcessing.SWF2HTML.res.lib.swiffotron-jquery-player-1.0.0.3.js"))
            using (StreamReader sr = new StreamReader(s))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    JQueryPlayerScript += line + "\n";
                }
            }
        }

        private int Width { get; set; }
        private int Height { get; set; }

        public SWF Swf { get; set; }

        Dictionary<IDisplayListItem, string> Dict;

        Dictionary<Timeline, int> TimelineIDs;

        Dictionary<string, Timeline> NameTimelineMap;

        private string RootID;

        private bool OutputComments;

        public JQueryCanvasApp(string ID, SWF swf, SWF2HTMLOptions options)
        {
            this.html = new HTMLAssist(ID);
            this.Swf = swf;
            this.Width = (int)Swf.FrameWidth;
            this.Height = (int)Swf.FrameHeight;
            this.OutputComments = options.OutputComments;
        }

        /// <summary>
        /// Render the SWF as a canvas element
        /// </summary>
        /// <param name="standalone">If true, the output HTML will be a complete
        /// HTML file containing all that it needs to run. If false, you need to make
        /// sure you put it in a friendly HTML5 environment where jQuery and the
        /// player script are available.</param>
        /// <returns>HTML/JS in a solid lump.</returns>
        public byte[] Render(bool standalone)
        {
            StringBuilder buff = new StringBuilder(16 * 1024);

            if (standalone)
            {
                buff.AppendLine("<!doctype html>");
            }

            RootID = html.OpenTag(buff, "div", new string[][] {
                new string[] {"style", "display:inline-block;width:"+this.Width+"px;height:"+this.Height+"px"}
            });

            buff.AppendLine("</div>");

            string populateFn = "populate_" + RootID;

            if (standalone)
            {
                buff.AppendLine("<script src=\"http://ajax.googleapis.com/ajax/libs/jquery/1.7.1/jquery.min.js\"></script>");
                buff.AppendLine("<script type='text/javascript'>");
                buff.Append(JQueryPlayerScript);
                buff.AppendLine("</script>");
            }

            buff.AppendLine("<script type='text/javascript'>");
            buff.AppendLine("function " + populateFn + "(swiffoid, root) {");
            buff.AppendLine("  var $root = $(root);");
            buff.AppendLine("  var dict = {};");
            buff.AppendLine("  var o = jQuery.noop;");

            this.Dict = new Dictionary<IDisplayListItem, string>();
            this.TimelineIDs = new Dictionary<Timeline, int>();
            this.NameTimelineMap = new Dictionary<string, Timeline>();
            RegisterTimeline(this.Swf);
            BuildDictionary(this.Swf, buff);

            buff.AppendLine("}");

            buff.AppendLine("jQuery(function() {");
            buff.AppendLine("  var player = jQuery('#" + this.RootID + "').swiffoid({'fps':" + Swf.Fps + ", populate: " + populateFn + " });");
            buff.AppendLine("  player.play();");
            buff.AppendLine("});");

            buff.AppendLine("</script>");

            return UTF8Encoding.Default.GetBytes(buff.ToString());
        }

        private string RegisterTimeline(Timeline t)
        {
            int num = TimelineIDs.Count;
            TimelineIDs.Add(t, num);

            string name = "s" + num;

            this.NameTimelineMap.Add(name, t);

            return name;
        }

        private void BuildDictionary(Timeline timeline, StringBuilder buff)
        {
            if (this.OutputComments)
            {
                buff.AppendLine("  /* timeline for "+timeline.ToString()+" */");
            }

            foreach (Frame f in Swf.Frames)
            {
                foreach (IDisplayListItem dli in f.DisplayList)
                {
                    if (!Dict.ContainsKey(dli) && dli.Type == DisplayListItemType.PlaceObjectX)
                    {
                        string name = "mc" + Dict.Count + 1;
                        Dict.Add(dli, name);

                        PlaceObject po = dli as PlaceObject;
                        ICharacter ch = po.Character;

                        if (ch is Shape)
                        {
                            buff.AppendLine("  dict['" + name + "'] = \"" + ShapeToJS(ch as Shape) + "\";");
                        }
                        else if (ch is Sprite)
                        {
                            string clipName = RegisterTimeline((Timeline)ch);

                            buff.AppendLine("  var " + clipName + " = swiffoid.createMovieClipClass([o,o,o,o,o]);");
                            buff.AppendLine("  swiffoid.addClip('" + clipName + "', " + clipName + ");");
                            buff.AppendLine("  var instance = swiffoid.instantiateClip('" + clipName + "');");
                            buff.AppendLine("  console.log(instance);");

                            html.JQueryAppendNew(buff, "  ", "$root", "canvas", new string[][] {
                                new string[] {"width", this.Width + "px"},
                                new string[] {"height", this.Height + "px"},
                            });

                            BuildDictionary((Sprite)ch, buff);
                        }
                    }
                }
            }
        }

        private string ShapeToJS(Shape shape)
        {
            return shape.ToString();
        }
    }
}
