(function ($) {

    $.fn.swiffoid = function (opts, dict) {
        var defaults = {
            'fps': 30
        };

        if (opts) {
            $.extend(defaults, opts);
        }

        init(defaults, dict);

        return this;
    };

    function init(opts, dict) {
        /* Initialise */
    }

})(jQuery);
