package
{
	import flash.display.MovieClip;	
	import flash.events.Event;
	
	public class Eyeball extends MovieClip
	{
		public function Eyeball()
		{
			this.addEventListener(Event.ENTER_FRAME, rotateit);
		}
		
		function rotateit(e:Event)
		{
			this.rotation += 10;
		}
	}
}
