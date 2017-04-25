(function () {
    var lib = {};

    lib.UpdateRank = function (leagueId, seasonId, unionId) {
        var ajaxUrl = "/LeagueRank/Details/" + leagueId + "?seasonId=" + seasonId + "&unionId=" + unionId;
        //console.log("ajax URL: ", ajaxUrl);
        $.ajax({
            type: "GET",
            url: ajaxUrl,
            success: function(data) {
                $("#leagueranktable").html(data);
            }
        });
    }

    window.lgEdit = lib;
})();
