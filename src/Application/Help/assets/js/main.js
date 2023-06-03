jQuery(document).ready(function($) {
  // Shrink nav on scroll - mobile-first UX
  $(window).scroll(function() {
    var scrollTop = $(document).scrollTop();

    $('.navbar-default, .cookie-banner').toggleClass('shrink', scrollTop > 20);
    $('.brand-home').toggleClass('slide-out-top', scrollTop > 320);
  });

  // Toggle sidenav arrows up or down
  $('.panel-collapse').on('show.bs.collapse', function() {
    $(this).siblings('.panel-heading').addClass('active');
  });

  $('.panel-collapse').on('hide.bs.collapse', function() {
    $(this).siblings('.panel-heading').removeClass('active');
  });

  // Homepage slider
  $("#carousel_home").carousel({
    interval: 15000, // Timeout between carousel slides in milliseconds
    pause: "hover"
  });

  // Make external links that start with http and don't go to our own site open in a new tab
  $('a[href^="http"]').not('a[href*="microsoft.github.io"]').attr('target', '_blank');

  // Add new feature: Smooth scrolling to anchor links
  $('a[href^="#"]').on('click', function(event) {
    var target = $(this.getAttribute('href'));
    if (target.length) {
      event.preventDefault();
      $('html, body').animate({
        scrollTop: target.offset().top
      }, 500);
    }
  });
});
