package  {
	
	import flash.display.MovieClip;
	
	
	public class Arrow extends ArrowBaseClass {
		
		public var pubSub :int = 11;
		protected var protSub :int = 13;
		private var privSub :int = 17;
		internal var intSub :int = 19;

		public function Arrow() {
			// constructor code
		}
		
					
		public function getProtected() :int
		{
			return protSub + protBase;
		}
	
		public function getPrivateSub() :int
		{
			return privSub;
		}
		
		public function getInternalSub() :int
		{
			return intSub;
		}

	}
	
}
