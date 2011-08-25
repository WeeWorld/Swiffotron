package  {
	
	import flash.display.MovieClip;
	
	
	public class ArrowBaseClass extends MovieClip {
		
		public var pubBase :int = 2;
		protected var protBase :int = 3;
		private var privBase :int = 5;
		internal var intBase :int = 7;
		
		public function Arrow() {
			// constructor code
		}
		
		public function getPrivateBase() :int
		{
			return privBase;
		}
		
		public function getInternalBase() :int
		{
			return intBase;
		}
	}
	
}
