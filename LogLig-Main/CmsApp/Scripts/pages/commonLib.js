(function () {
    'use strict';

    var lib = {};

    lib.initDateTimePickers = function () {
        $(".frm-date").datetimepicker({
            format: 'd/m/Y H:i',
            formatTime: 'H:i',
            formatDate: 'd/m/Y',

            step: 15,
            closeOnDateSelect: false,
            onChangeDateTime: function () {
                $(this).data("input").trigger("changedatetime.xdsoft");
            }
        });
        $(".frm-date-wo-time").datetimepicker({
            format: 'd/m/Y',
            timepicker: false
        });
    };

    window.cmn = lib;
})();