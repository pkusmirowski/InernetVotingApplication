/**
* Based on the Ninestars template by BootstrapMade.com (https://bootstrapmade.com/license/).
* Only the parts this application uses are kept: the back-to-top button and the mobile menu toggle.
*/
(function () {
  "use strict";

  const backToTop = document.querySelector('.back-to-top');
  if (backToTop) {
    const toggleBackToTop = () => backToTop.classList.toggle('active', window.scrollY > 100);
    window.addEventListener('load', toggleBackToTop);
    document.addEventListener('scroll', toggleBackToTop);
  }

  const mobileToggle = document.querySelector('.mobile-nav-toggle');
  if (mobileToggle) {
    mobileToggle.addEventListener('click', function () {
      document.querySelector('#navbar').classList.toggle('navbar-mobile');
      this.classList.toggle('bi-list');
      this.classList.toggle('bi-x');
    });
  }
})();
