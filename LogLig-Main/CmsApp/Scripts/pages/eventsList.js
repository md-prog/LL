(function (commonLib) {
    "use strict";

    var lib = {};

    lib.eventsOpen = function () {
        document.getElementById("eventsOpen").style.display = 'none';
        document.getElementById("events_tbl").style.display = 'block';
        document.getElementById("eventsClose").style.display = 'block';
    };

    lib.eventsClose = function () {
        document.getElementById("eventsOpen").style.display = 'block';
        document.getElementById("events_tbl").style.display = 'none';
        document.getElementById("eventsClose").style.display = 'none';
    };

    lib.publishEvent = function (eventId) {
        var eventCheckBoxId = "#EventChbx_" + eventId;
        var currCheckBox = $(eventCheckBoxId)[0];
        var data = {
            eventId: eventId,
            isPublished: $(currCheckBox).is(':checked')
        };
        $.post('/Events/PublishEvent', data, function () {
            })
            .error(function (resp) {
                $(currCheckBox).prop('checked', !data.isPublished);
                alert("Error: " + resp.responseText);
                console.log("Response: ", resp);
            });
    };

    lib.UpdateEvent = function (eventId) {
        var event_line = $("table#events_tbl tbody tr#event_" + eventId);
        //console.log(event_line);
        var dStr = $(event_line).find("input[name=EventTime]").val();
        //console.log("sStr", dStr);
        dStr = dStr.replace(/(\d{2})\/(\d{2})\/(\d{4}) (\d{2}):(\d{2})/, "$2/$1/$3 $4:$5:00")
        //console.log("sStr", dStr);
        var d = new Date(Date.parse(dStr));
        //console.log("d = ", d);
        var evTimeStr = d.getFullYear() + '-' + ("0" + (d.getMonth() + 1)).slice(-2) + '-' + ("0" + d.getDate()).slice(-2)
            + ' ' + ("0" + (d.getHours())).slice(-2) + ':' + ("0" + d.getMinutes()).slice(-2) + ':' + ("0" + d.getSeconds()).slice(-2);
        //console.log("evTimeStr", evTimeStr);
        var data = {
            EventId: eventId,
            Place: $(event_line).find("input[name=Place]").val(),
            Title: $(event_line).find("input[name=Title]").val(),
            EventTime: evTimeStr,
            IsPublished: $(event_line).find("input[name=IsPublished]").is(":checked"),
            LeagueId: $(event_line).find("input[name=LeagueId]").val(),
            ClubId: $(event_line).find("input[name=ClubId]").val(),
            CreateDate: $(event_line).find("input[name=CreateDate]").val()
        };
        //console.log(data);
        $.post("/Events/UpdateEvent", data, function (response) {
            if (response.stat === 'ok') {
                //console.log('Successfully updated event', response.id);
                $(event_line).find("a[name=savebtn]").attr('disabled', true);
            }
        });
    };

    lib.hideModalDialog = function () {
        var dial = $('#addevent');
        if (($(dial).data('bs.modal') || {}).isShown) {
            $(dial).modal('hide');
        }
    };

    lib.documentReady = function () {
        $('#events_tbl tbody tr').each(function () {
            var me = $(this);
            var btn = $('[name=savebtn]', me);

            $('.form-control', me).change(function () {
                btn.attr('disabled', false);
            });

            $('.frm-date', me).on('changedatetime.xdsoft', function (e) {
                btn.attr('disabled', false);
            });
        });

        commonLib.initDateTimePickers();
    }

    window.evList = lib;
})(cmn);