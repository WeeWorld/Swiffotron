package com.acme.stars
{
	import flash.display.MovieClip;	
	import flash.events.Event;
	
	public class BlueStar extends MovieClip
	{
		public function BlueStar()
		{
			this.addEventListener(Event.ENTER_FRAME, risestar);
		}
		
		function risestar(e:Event)
		{
			this.y -=0.5;
		}
	}
}
