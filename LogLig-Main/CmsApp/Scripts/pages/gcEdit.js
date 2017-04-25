(function () {
    var lib = {};

    lib.resetGame = function (warnOnReset, msg, cycleId) {
        if (!warnOnReset || confirm(msg)) {
            window.location.replace("/GameCycle/ResetGame/" + cycleId);
        }
    }

    lib.updateGameResult = function (warnOnReset, msg, cycleId, isWaterpoloOrBasketball) {
        if (!warnOnReset || confirm(msg)) {
            window.location.replace("/GameCycle/UpdateGameResults/" + cycleId + "?isWaterpoloOrBasketball=" + isWaterpoloOrBasketball);
        }
    }

    lib.documentReady = function () {
        $('#gamefrm').validateBootstrap(true);
    };

    window.gcEdit = lib;
})();