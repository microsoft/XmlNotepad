jQuery(document).ready(function ($) {
  const navbar = $('.navbar-default');
  const cookieBanner = $('.cookie-banner');
  const brandHome = $('.brand-home');
  const panelHeading = $('.panel-heading');

  // Shrink nav on scroll - mobile-first UX
  $(window).scroll(function () {
    if ($(document).scrollTop() > 20) {
      navbar.addClass('shrink');
      cookieBanner.addClass('shrink');
    } else {
      navbar.removeClass('shrink');
      cookieBanner.removeClass('shrink');
    }

    if ($(document).scrollTop() > 320) {
      brandHome.addClass('slide-out-top');
    } else {
      brandHome.removeClass('slide-out-top');
    }
  });

  // Toggle sidenav arrows up or down
  $('.panel-collapse').on('show.bs.collapse', function () {
    $(this).siblings('.panel-heading').addClass('active');
  });

  $('.panel-collapse').on('hide.bs.collapse', function () {
    $(this).siblings('.panel-heading').removeClass('active');
  });

  const year = new Date().getFullYear();
  $('#copyright').append(`&copy; ${year} Microsoft`);
});
