// Application behaviour that must not be inline (the Content Security Policy allows scripts from this origin only).
(function () {
    'use strict';

    // Forms with data-confirm="..." ask the user before submitting.
    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (form instanceof HTMLFormElement && form.dataset.confirm && !window.confirm(form.dataset.confirm)) {
            event.preventDefault();
        }
    });
})();
