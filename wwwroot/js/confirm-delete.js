/* Global delete confirmation for setup, master, and process pages */
(function () {
    'use strict';

    function formAction(form) {
        return (form.getAttribute('action') || (window.location.pathname + window.location.search)).toLowerCase();
    }

    function isDeleteForm(form) {
        if (!form || form.tagName !== 'FORM') return false;
        if ((form.method || 'get').toLowerCase() !== 'post') return false;
        if (form.hasAttribute('data-no-delete-confirm')) return false;

        var action = formAction(form);
        if (action.indexOf('handler=delete') >= 0) return true;
        if (action.indexOf('handler=deleteentitlement') >= 0) return true;
        if (action.indexOf('handler=unlinkbenefit') >= 0) return true;
        if (form.querySelector('input[name="deleteId"]')) return true;
        return false;
    }

    function rowLabel(form) {
        var row = form.closest('tr');
        if (!row) return '';

        var cells = row.querySelectorAll('td');
        if (!cells.length) return '';

        var text = (cells[0].textContent || '').trim();
        if (!text || text.toLowerCase() === 'no records found.') return '';
        return text;
    }

    function confirmMessage(form) {
        if (form.dataset.deleteMessage) return form.dataset.deleteMessage;

        var action = formAction(form);
        var label = rowLabel(form);

        if (action.indexOf('handler=unlinkbenefit') >= 0) {
            return 'Are you sure you want to remove this benefit from the entitlement?';
        }

        if (action.indexOf('handler=deleteentitlement') >= 0) {
            return label
                ? 'Are you sure you want to delete entitlement "' + label + '"?\nThis action cannot be undone.'
                : 'Are you sure you want to delete this entitlement record?\nThis action cannot be undone.';
        }

        if (label) {
            return 'Are you sure you want to delete "' + label + '"?\nThis action cannot be undone.';
        }

        return 'Are you sure you want to delete this record?\nThis action cannot be undone.';
    }

    function stripInlineConfirm(form) {
        form.removeAttribute('onsubmit');

        form.querySelectorAll('[onclick]').forEach(function (el) {
            var handler = el.getAttribute('onclick') || '';
            if (handler.indexOf('confirm(') >= 0) {
                el.removeAttribute('onclick');
            }
        });
    }

    function bindForm(form) {
        if (form.dataset.deleteConfirmBound === 'true') return;
        form.dataset.deleteConfirmBound = 'true';
        stripInlineConfirm(form);

        form.addEventListener('submit', function (e) {
            if (form.dataset.deleteConfirmed === 'yes') {
                delete form.dataset.deleteConfirmed;
                return;
            }

            e.preventDefault();
            e.stopPropagation();

            if (window.confirm(confirmMessage(form))) {
                form.dataset.deleteConfirmed = 'yes';
                if (typeof form.requestSubmit === 'function') {
                    form.requestSubmit(e.submitter || undefined);
                } else {
                    form.submit();
                }
            }
        }, true);
    }

    function init() {
        document.querySelectorAll('form').forEach(function (form) {
            if (isDeleteForm(form)) bindForm(form);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
}());
