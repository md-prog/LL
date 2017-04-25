(function (commonLib) {
    "use strict";

    var lib = {};

    function findRow(cycleId) {
        var row = $("#row" + cycleId);
        return row;
    }

    function CycleGroupCheckBoxChangeState(state, cycleGroupCheckBoxId, stageId) {
        var cycleGroupCheck = $('#' + cycleGroupCheckBoxId);
        var allCheckBoxesChecked = state;
        if (state) {
            $('.' + cycleGroupCheckBoxId + ' ' + 'tr input.gameCycleItemChbx').each(function () {
                var isChecked = $(this).is(':checked');
                if (!isChecked) {
                    allCheckBoxesChecked = false;
                    return;
                }
            });

            if (allCheckBoxesChecked) {
                cycleGroupCheck.prop('checked', true);
            }
        } else {
            cycleGroupCheck.prop('checked', false);
        }
        StageCheckBoxChangeState(allCheckBoxesChecked, stageId);
    }

    function StageCheckBoxChangeState(state, stageId) {
        var stageCheckBox = $('#stageItemsChbx_' + stageId);
        if (state) {
            var allCheckBoxesChecked = true;
            $("#" + stageId + " .gamePublish").each(function () {
                //console.log($(this));
                if (!$(this).is(':checked')) {
                    allCheckBoxesChecked = false;
                    return;
                }
            });
            if (allCheckBoxesChecked) {
                stageCheckBox.prop('checked', true);
            }
        } else {
            stageCheckBox.prop('checked', false);
        }
    }

    lib.dateFilterTypeChange = function () {
        //console.log('change');
        var selVal = $('#dateFilterType').val();
        var dfd = $('#date-from-div');
        var dtd = $('#date-to-div');
        if (selVal === '1') {
            $(dfd).show();
            $(dtd).show();
        } else {
            $(dfd).hide();
            $(dtd).hide();
        }
    };

    lib.stageOpen = function (id) {
        var idOpen = id + "open";
        var idClose = id + "close";
        document.getElementById(idOpen).style.display = 'none'
        document.getElementById(id).style.display = 'block'
        document.getElementById(idClose).style.display = 'block'
    };

    lib.stageClose = function (id) {
        var idOpen = id + "open";
        var idClose = id + "close";
        document.getElementById(idOpen).style.display = 'block'
        document.getElementById(id).style.display = 'none'
        document.getElementById(idClose).style.display = 'none'
    };

    lib.sectionToggleTeams = function (cycleId, unionId, seasonId) {
        var data = {
            id: cycleId,
            unionId: unionId,
            seasonId: seasonId
        };
        $.post("/Sections/Toggle/" + cycleId, data, function (response) {
            //console.log('Response: ', response);
            if(response.stat === 'ok') {
                var row = findRow(response.id);
                $(row).find('.home-team-text').text(response.homeTeam.Title);
                $(row).find('.guest-team-text').text(response.guestTeam.Title);
                $(row).find('.home-team-score').text(response.homeTeamScore);
                $(row).find('.guest-team-score').text(response.guestTeamScore);
                $(row).find('.guest-team-id').val(response.guestTeam.TeamId);
                $(row).find('.home-team-id').val(response.homeTeam.TeamId);
                $(row).find('[name=AuditoriumId]').val(response.arenaId);
                lib.lockHomeTeam(response.id);
                lib.lockGuestTeam(response.id);
            } else {
                lib.cycleUpdateError(response);
            };
        });
    };

    lib.publishGamesCycle = function (stageId, cycleNum, gameCycleId) {
        var cycleGroupCheckBoxId = 'publish_' + stageId + '_' + cycleNum;

        var currentGameCycleCheckBox = $('#gameCycleItemChbx_' + gameCycleId);
        var currentGameCycleCheckBoxState = currentGameCycleCheckBox.is(':checked');

        CycleGroupCheckBoxChangeState(currentGameCycleCheckBoxState, cycleGroupCheckBoxId, stageId);

        var data = {
            gameCycleId: gameCycleId,
            isPublished: currentGameCycleCheckBoxState
        };

        $.post('/Schedules/PublishGamesCycle', data, function () { })
        .error(function (resp) {
            currentGameCycleCheckBox.prop('checked', !currentGameCycleCheckBoxState);
            CycleGroupCheckBoxChangeState(!currentGameCycleCheckBoxState, cycleGroupCheckBoxId, stageId);

            alert("Error: " + resp.responseText);
            console.log("Response: ", resp);
        });
    };

    lib.publishGamesCyclesByCycleNumber = function (seasonId, leagueId, stageId, cycleNum) {
        var checkBoxId = 'publish_' + stageId + '_' + cycleNum;
        var checkBox = $('#' + checkBoxId);
        var isPublished = $('#' + checkBoxId).is(':checked');
        var gameCyclesCheckBoxes = $('.' + checkBoxId + ' ' + 'tr input.gameCycleItemChbx');

        gameCyclesCheckBoxes.prop('checked', isPublished);

        var data = {
            seasonId: seasonId,
            leagueId: leagueId,
            stageId: stageId,
            cycleNum: cycleNum,
            isPublished: isPublished
        };

        $.post('/Schedules/PublishGamesCyclesByCycleNumber', data, function () {
            StageCheckBoxChangeState(isPublished, stageId);
        })
        .error(function (resp) {
            checkBox.prop('checked', !isPublished);
            gameCyclesCheckBoxes.prop('checked', !isPublished);

            alert("Error: " + resp.responseText);
            console.log("Response: ", resp);
        });
    };

    lib.publishAllLeagueGamesCycles = function publishAllLeagueGamesCycles(seasonId, leagueId, isPublished) {
        function changeCheckBoxesState(state) {
            $('.gamePublish').each(function () {
                $(this).prop('checked', state);

                var gameCylesGroupNumAndStage = $(this).attr('id');
                var gameCyclesTableCheckBoxes = $('.' + gameCylesGroupNumAndStage + ' ' + 'input.gameCycleItemChbx');
                $(gameCyclesTableCheckBoxes).prop('checked', state);
            });
        }

        changeCheckBoxesState(isPublished);

        var data = {
            seasonId: seasonId,
            leagueId: leagueId,
            isPublished: isPublished
        };

        $.post('/Schedules/PublishAllLeagueGamesCycles', data, function () { })
        .error(function (resp) {
            changeCheckBoxesState(!isPublished);

            alert("Error: " + resp.responseText);
            console.log("Response: ", resp);
        });
    };

    lib.allLeaguesCheck = function () {
        var isChecked = $('#all_leagues').is(':checked');
        $(".league-checkbox").prop('checked', isChecked);
    };

    lib.leagueCheck = function () {
        var allAreChecked = $(".league-checkbox:not(:checked)").length === 0;
        $('#all_leagues').prop('checked', allAreChecked);
    };

    lib.allAuditoriumsCheck = function () {
        var isChecked = $("#all_auditoriums").is(':checked');
        $(".auditorium-checkbox").prop('checked', isChecked);
    };

    lib.auditoriumCheck = function () {
        var allAreChecked = $(".auditorium-checkbox:not(:checked)").length === 0;
        $("#all_auditoriums").prop('checked', allAreChecked);
    };

    lib.unlockHomeTeam = function (cycleId) {
        var row = findRow(cycleId);
        $(row).find(".home-team-label").hide();
        $(row).find(".home-team-drop-down").css('display','inline-flex');
    };

    lib.lockHomeTeam = function (cycleId) {
        var row = findRow(cycleId);
        $(row).find(".home-team-label").show();
        $(row).find(".home-team-drop-down").hide();
    };

    lib.unlockGuestTeam = function unlockGuestTeam(cycleId) {
        var row = findRow(cycleId);
        $(row).find(".guest-team-label").hide();
        $(row).find(".guest-team-drop-down").css('display', 'inline-flex');
    };

    lib.lockGuestTeam = function (cycleId) {
        var row = findRow(cycleId);
        $(row).find(".guest-team-label").show();
        $(row).find(".guest-team-drop-down").hide();
    };

    lib.HomeTeamChanged = function (cycleId, newText) {
        var row = findRow(cycleId);
        $(row).find(".home-team-text").text(newText);
    };

    lib.GuestTeamChanged = function (cycleId, newText) {
        var row = findRow(cycleId);
        $(row).find(".guest-team-text").text(newText);
    };

    lib.SubmitGameForm = function (cycleId) {
        var row = findRow(cycleId);
        var homeTeamId = $(row).find(".home-team-id").val();
        //console.log("homeTeamId", homeTeamId);
        var guestTeamId = $(row).find(".guest-team-id").val();
        //console.log("guestTeamId", guestTeamId);
        if (typeof homeTeamId !== 'undefined' && homeTeamId === guestTeamId) {
            alert("Home team cannot be the same as the guest one!");
        }
        else {
            var form = (row).find("gamefrm" + cycleId);
            $(row).find("#gamefrm" + cycleId).submit();
        }
    };

    lib.cycleUpdated = function (res) {
        //console.log('cycle update result: ', res);
        if (res.stat === 'ok') {
            var row = findRow(res.id);
            $(row).find('[name=savebtn]').attr('disabled', true);
            lib.lockHomeTeam(res.id);
            lib.lockGuestTeam(res.id);
        } else if (res.stat === 'error') {
            alert('Error occured while saving game: \r\n' + res.message);
        } else {
            alert('Unknown error occured while saving game.');
        }
    };

    lib.SubmitScheduleCond = function (leagueId, isChronological, desOrder) {
        if (desOrder === undefined) {
            var dOrd = sessionStorage["desOrder"]
            desOrder = dOrd === undefined ? false : dOrd;
        }
        if (isChronological === undefined) {
            isChronological = false;
        }
        var dateFilterType = $('#dateFilterType').val();
        var dateFrom = $('#dateFrom').val();
        var dateTo = $('#dateTo').val();
        var ajaxUrl = '/Schedules/List';
        var params = {
            id: leagueId,
            desOrder: desOrder,
            isChronological: isChronological,
            dateFilterType: dateFilterType,
            dateFrom: dateFrom,
            dateTo: dateTo
        };
        //  Update games list
        $.ajax({
            type: "GET",
            url: ajaxUrl,
            data: params,
            success: function(data) {
                $("#schedules").html(data);
            }
        });
        //  Update URL for Excel file download
        updateExportToExcelUrl();
    };

    function updateExportToExcelUrl() {
        var leagueId = $('#leagueId').val();
        var isChronological = $('#isChronological').val();
        var dateFilterType = $('#dateFilterType').val();
        var dateFrom = $('#dateFrom').val();
        var dateTo = $('#dateTo').val();
        //  Update URL for Excel file download
        var ajaxUrl = '/Schedules/ExportToExcel?';
        var params = {
            leagueId: leagueId,
            dateFilterType: isChronological ? dateFilterType : 2,
            dateFrom: dateFrom,
            dateTo: dateTo
        };
        var newHref = ajaxUrl + $.param(params);
        $('#export-to-excel').attr('href', newHref);
    }

    lib.cycleUpdateError = function (data) {
        if ((typeof data === "object") && (data !== null)) {
            alert("Unknown error: ", JSON.stringify(data));
        } else if (typeof data === "string") {
            alert("Unknown error: ", data);
        } else {
            alert("Unknown error.");
        }
    };

    lib.checkFileExtension = function (sender) {
        var validExts = new Array(".xlsx", ".xls");
        var file = $(sender.target).prop('files');
        if (file.length > 0) {
            var fileExt = file[0].name;
            fileExt = fileExt.substring(fileExt.lastIndexOf('.'));
            if (validExts.indexOf(fileExt) < 0) {
                alert("Invalid file selected, valid files are of " +
                    validExts.toString() + " types.");
                return false;
            } else {
                return true;
            }
        } else {
            alert('No file selected');
            return false;
        }
    }

    //  OnLoad handlers
    lib.documentReady = function () {
        $('[data-toggle="tooltip"]').tooltip();

        $('#res_tbl tbody tr').each(function () {

            var me = $(this);
            var btn = $('[name=savebtn]', me);

            $('select', me).change(function () {
                btn.attr('disabled', false);
            });

            $('.frm-date', me).on('changedatetime.xdsoft', function (e) {
                btn.attr('disabled', false);
            });
        });

        $('.cyclemoveform tr').each(function () {
            var me = $(this);
            $('.frm-date', me).on('changedatetime.xdsoft', function (e) {
                $('button', me).attr('disabled', false);
            });
        });

        $('#uploadFileBtn').change(function (event) {
            var isValidExtension = lib.checkFileExtension(event);
            if (isValidExtension) {
                console.log('Valid ext and procced upload');
                $(this).closest('form').submit();
            } else {
                console.log('not valid extension');
            }
        });

        updateExportToExcelUrl();
        commonLib.initDateTimePickers();
    };

    window.gcList = lib;
})(window.cmn);

