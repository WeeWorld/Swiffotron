/* requestAnimationFrame shim */

window.requestAnimationFrame || (window.requestAnimationFrame =
window.webkitRequestAnimationFrame ||
window.mozRequestAnimationFrame ||
window.oRequestAnimationFrame ||
window.msRequestAnimationFrame ||
function (callback, element) {
    return window.setTimeout(function () {
        callback(+new Date());
    }, 1000 / 60);
});

/* Swiffoid jQuery flash player for Swiffotron */

Swiffoid = function (ele, opts) {

    var _this = this;

    this.sprites = {};

    var init = function (ele) {
        opts.populate(_this, ele); /* TODO: Inline? */
    };

    var defaults = {
        'fps': 30,
        'populate': $.noop
    };

    if (opts) {
        $.extend(defaults, opts);
    }

    init(ele);

    return this;
};

Swiffoid.prototype.drawStage = function () {
    console.log("tick");
};

Swiffoid.prototype.play = function () {
    var _this = this;
    requestAnimationFrame(function () {
        _this.play();
    });
    this.drawStage();
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
