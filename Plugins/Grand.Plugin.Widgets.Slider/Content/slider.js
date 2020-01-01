$(document).ready(function () {

    var $item = $('.carousel-item');
    $item.addClass('full-screen');

    SliderPosition();
    $("#GrandCarousel .carousel-indicators:first, #GrandCarousel .carousel-inner .carousel-item:first").addClass("active");
    $("#GrandCarousel .carousel-indicators li").each(function () {
        var car_ind = $(this).index();
        $(this).attr("data-slide-to", car_ind);
    });

    $('.carousel img').each(function () {
        var $src = $(this).attr('src');
        $(this).parent().parent().parent().css({
            'background-image': 'url(' + $src + ')',
        });
        $(this).remove();
    });
});
$(window).resize(function () {
    SliderPosition();
});
$("#next").click(function () {
    $('#grandCarousel').carousel('next');
});

$("#prev").click(function () {
    $('#grandCarousel').carousel('prev');
});

$(".carousel").on("touchstart", function (event) {
    var xClick = event.originalEvent.touches[0].pageX;
    $(this).one("touchmove", function (event) {
        var xMove = event.originalEvent.touches[0].pageX;
        if (Math.floor(xClick - xMove) > 5) {
            $(".carousel").carousel('next');
        }
        else if (Math.floor(xClick - xMove) < -5) {
            $(".carousel").carousel('prev');
        }
    });
    $(".carousel").on("touchend", function () {
        $(this).off("touchmove");
    });
});

function SliderPosition() {

    var $item = $('.carousel-item');

    var $mainNavHeight = $('.headBottom ').height();
    var $headerHeight = $('.navbar ').height() + $mainNavHeight;
   
    var $wHeight = $(window).height() - $headerHeight;
    $item.height($wHeight); 

    var FixSliderPos = $('.custom-container').position().left + 'px';
    $('#GrandCarousel').css('left', '-' + FixSliderPos);
}
function getOrientation(){
    return Math.abs(window.orientation) - 90 == 0 ? "landscape" : "portrait";
};
function getMobileWidth(){
    return getOrientation() == "landscape" ? screen.availHeight : screen.availWidth;
};
function getMobileHeight(){
    return getOrientation() == "landscape" ? screen.availWidth : screen.availHeight;
};