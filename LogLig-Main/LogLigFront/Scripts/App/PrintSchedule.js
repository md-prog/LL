
$(document).ready(function () {
    //$('#print').prependTo('div.body-content');
    $('#print').click(function () {
        if ($('.res-table').length > 0) {
            windowPrint($('div.col-sm-12'));
        }
    });
});

function windowPrint(element) {

    var wnd = window.open('', 'Game schedules', 'height=600, width=600');
    wnd.document.write('<html><head><title>Game schedules</title>');
    //if need to add styles
    wnd.document.write('<link rel="stylesheet" href="/Content/bootstrap.css" type="text/css" media="print" />');
    wnd.document.write('<link rel="stylesheet" href="/Content/css/bootstrap-rtl.css" type="text/css" />');
    wnd.document.write('<link rel="stylesheet" href="/Content/site.css" type="text/css" />');

    wnd.document.write('<body>');
    wnd.document.write($(element).prop('outerHTML'));

    $(wnd.document).find('.remove_print').remove();
    $(wnd.document).find('.main-btn').remove();
    $(wnd.document).find('img').remove();
    $(wnd.document).find('a').replaceWith(function () { return this.innerHTML });
    $(wnd.document).find('footer').remove();
    wnd.document.write('</body></html>');
    wnd.document.close();
    wnd.focus();
    setTimeout(function () {
        wnd.print();
    }, 1000);
}
