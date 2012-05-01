/* requestAnimationFrame shim; http://paulirish.com/2011/requestanimationframe-for-smart-animating/ */
(function () {
    var lastTime = 0;
    var vendors = ['ms', 'moz', 'webkit', 'o'];
    for (var x = 0; x < vendors.length && !window.requestAnimationFrame; ++x) {
        window.requestAnimationFrame = window[vendors[x] + 'RequestAnimationFrame'];
        window.cancelAnimationFrame =
          window[vendors[x] + 'CancelAnimationFrame'] || window[vendors[x] + 'CancelRequestAnimationFrame'];
    }

    if (!window.requestAnimationFrame)
        window.requestAnimationFrame = function (callback, element) {
            var currTime = new Date().getTime();
            var timeToCall = Math.max(0, 16 - (currTime - lastTime));
            var id = window.setTimeout(function () { callback(currTime + timeToCall); },
              timeToCall);
            lastTime = currTime + timeToCall;
            return id;
        };

    if (!window.cancelAnimationFrame)
        window.cancelAnimationFrame = function (id) {
            clearTimeout(id);
        };
} ());

/* Swiffoid jQuery flash player for Swiffotron */

Swiffoid = function (ele, opts) {

    var _this = this;

    this.sprites = { };

    this.stage = null;

    this.opts = opts;

    var defaults = {
        'fps': 30,
        'populate': $.noop,
        'consoleLog': false
    };

    if (opts) {
        $.extend(defaults, opts);
    }

    opts.populate(this, ele);

    return this;
};

Swiffoid.prototype.drawStage = function (t) {
    //console.log("tick "+t);
};

Swiffoid.prototype.loop = function (t) {
    var _this = this;
    requestAnimationFrame(function (t) {
        _this.loop(t);
    });
    this.drawStage(t);
}

Swiffoid.prototype.play = function () {
    if (this.stage === null) {
        if (this.opts.consoleLog) { console.log("Can't play SWF data. No stage was defined."); };
        return;
    };
    this.loop(+new Date());
};

Swiffoid.prototype.pause = function () {
    /* TODO */
};

Swiffoid.prototype.addClip = function (name, constructor) {
    constructor.prototype.dictionaryName = name;
    this.sprites[name] = constructor;
};

Swiffoid.prototype.instantiateClip = function (name) {
    var instance = new this.sprites[name]();
    return instance;
}

Swiffoid.prototype.createMovieClipClass = function (timelineArray) {
    var MovieClip = function () {
        this.playHead = 0;
    }

    MovieClip.prototype.step = function () {
    }

    MovieClip.prototype.timeline = timelineArray;

    return MovieClip;
};

/* jQuery extension for easy invocation */

(function ($) {

    $.fn.swiffoid = function (opts) {
        return new Swiffoid(this, opts);
    };

})(jQuery);
