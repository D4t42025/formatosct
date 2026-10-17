var btn = $('#button');

$(window).scroll(function () {
  if ($(window).scrollTop() > 300) {
    btn.addClass('show');
  } else {
    btn.removeClass('show');
  }
});
btn.on('click', function (e) {
  e.preventDefault();
  $('html, body').animate({
    scrollTop: 0
  }, '300');
});


$(window).on('load', function () {
  // Preloader
  $('.loader').fadeOut();
  $('.loader-mask').delay(350).fadeOut('slow');
});


$(document).ready(function() {
  var owl = $('.projects-con .owl-carousel');
  owl.owlCarousel({
      margin: 0,
      nav: false,
      loop: true,
      dots: false,
      autoplay: true,
      autoplayTimeout: 4500,
      responsive: {
          0: {
              items: 1
          },
          576: {
              items: 1.3
          },
          768: {
              items: 1.8
          },
          992: {
              items: 2.5
          },
          1200: {
              items: 2.5
          },
          1440:{
              items: 3.4
          }
      }
  })
})
$(document).ready(function() {
  var owl = $('.reviews-con .owl-carousel');
  owl.owlCarousel({
      margin: 0,
      nav: false,
      loop: true,
      dots: true,
      autoplay: true,
      autoplayTimeout: 4500,
      responsive: {
          0: {
              items: 1
          },
          576: {
              items: 1
          },
          768: {
              items: 1
          },
          992: {
              items: 1
          },
          1200: {
              items: 1
          },
          1440:{
              items: 1
          }
      }
  })
})