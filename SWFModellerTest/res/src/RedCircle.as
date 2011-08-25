package
{
	import flash.display.MovieClip;	
	import flash.events.Event;
	
	public class RedCircle extends MovieClip
	{
		public function RedCircle()
		{
			this.addEventListener(Event.ENTER_FRAME, moveit);
		}
		
		function moveit(e:Event)
		{
			this.x -=1;
		}
	}
}
