package
{
	import flash.display.MovieClip;	
	import flash.events.Event;
	
	public class YellowTriangle extends MovieClip
	{
		public function YellowTriangle()
		{
			this.addEventListener(Event.ENTER_FRAME, movetriangle);
		}
		
		function movetriangle(e:Event)
		{
			this.x +=1.5;
		}
	}
}
