(function ($) {

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

    function swiffoid_init(opts, dict) {
        /* Initialise */
    }

    $.fn.swiffoid = function (opts, dict) {
        var defaults = {
            'fps': 30
        };

        if (opts) {
            $.extend(defaults, opts);
        }

        swiffoid_init(defaults, dict);

        return this;
    };

})(jQuery);
