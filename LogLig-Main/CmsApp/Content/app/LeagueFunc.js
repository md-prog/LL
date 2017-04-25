var leagueApp = (function ($, win, doc) {

    var leagueId = 0;

    return {
        init: function () {
            leagueId = $('#LeagueId').val();
            setFileUpload('#detailsform', '#details');
            bindWorkers();
        }
    }

    function bindWorkers()
    {
        $('#league_tabs [href="#workers"]').click(function () {
            loadWorkers(leagueId);
        });

        $(doc).on('click', '.del-worker', function (e) {
            var id = $(this).data('id');
            $.post('/LeagueWorkers/Delete/' + id, function () {
                loadWorkers(leagueId);
            });
        });
    }

    function loadWorkers(id) {
        $("#workers_pl").load("/LeagueWorkers/List/" + id);
    }

})(jQuery, window, document);

function detailsSaved() {
    setFileUpload('#detailsform', '#details');
}

