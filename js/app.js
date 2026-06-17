/* =========================================================
   HRMS – Employee Master  |  Client-side Logic
   ========================================================= */

'use strict';

/* ---- Live Clock ---- */
(function initClock() {
    function tick() {
        var el = document.getElementById('clock');
        if (!el) return;
        var now = new Date();
        var d   = now.toLocaleDateString('en-PK', { weekday:'short', year:'numeric', month:'short', day:'numeric' });
        var t   = now.toLocaleTimeString('en-PK');
        el.textContent = d + '  ' + t;
    }
    tick();
    setInterval(tick, 1000);
}());

/* ---- ASP.NET control ID helpers ---- */
function $id(partialId) {
    // ASP.NET may mangle control IDs; find the element whose id ends with the partial id.
    var els = document.querySelectorAll('[id$="' + partialId + '"]');
    return els.length ? els[0] : null;
}
function val(partialId) {
    var el = $id(partialId);
    return el ? el.value.trim() : '';
}
function clearError(id) {
    var el = document.getElementById(id);
    if (el) el.textContent = '';
}
function setError(id, msg) {
    var el = document.getElementById(id);
    if (el) el.textContent = msg;
}
function focusCtrl(partialId) {
    var el = $id(partialId);
    if (el) el.focus();
}

/* ---- Client-side Form Validation ---- */
function validateForm() {
    var valid = true;

    // Clear all previous errors
    ['errEmpCode','errFirstName','errLastName','errGender',
     'errEmail','errPhone','errDepartment','errDesignation',
     'errDOJ','errSalary'].forEach(clearError);

    // Employee Code
    if (!val('txtEmpCode')) {
        setError('errEmpCode', 'Employee Code is required.');
        if (valid) { focusCtrl('txtEmpCode'); valid = false; }
    }

    // First Name
    if (!val('txtFirstName')) {
        setError('errFirstName', 'First Name is required.');
        if (valid) { focusCtrl('txtFirstName'); valid = false; }
    }

    // Last Name
    if (!val('txtLastName')) {
        setError('errLastName', 'Last Name is required.');
        if (valid) { focusCtrl('txtLastName'); valid = false; }
    }

    // Gender
    if (!val('ddlGender')) {
        setError('errGender', 'Please select Gender.');
        if (valid) { focusCtrl('ddlGender'); valid = false; }
    }

    // Email
    var email = val('txtEmail');
    if (!email) {
        setError('errEmail', 'Email is required.');
        if (valid) { focusCtrl('txtEmail'); valid = false; }
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
        setError('errEmail', 'Enter a valid email address.');
        if (valid) { focusCtrl('txtEmail'); valid = false; }
    }

    // Phone
    var phone = val('txtPhone');
    if (!phone) {
        setError('errPhone', 'Phone number is required.');
        if (valid) { focusCtrl('txtPhone'); valid = false; }
    } else if (!/^[0-9+\-\s()]{7,20}$/.test(phone)) {
        setError('errPhone', 'Enter a valid phone number.');
        if (valid) { focusCtrl('txtPhone'); valid = false; }
    }

    // Department
    if (!val('ddlDepartment')) {
        setError('errDepartment', 'Please select a Department.');
        if (valid) { focusCtrl('ddlDepartment'); valid = false; }
    }

    // Designation
    if (!val('txtDesignation')) {
        setError('errDesignation', 'Designation is required.');
        if (valid) { focusCtrl('txtDesignation'); valid = false; }
    }

    // Date of Joining
    if (!val('txtDOJ')) {
        setError('errDOJ', 'Date of Joining is required.');
        if (valid) { focusCtrl('txtDOJ'); valid = false; }
    }

    // Salary
    var salary = val('txtSalary');
    if (!salary) {
        setError('errSalary', 'Basic Salary is required.');
        if (valid) { focusCtrl('txtSalary'); valid = false; }
    } else if (isNaN(salary) || parseFloat(salary) < 0) {
        setError('errSalary', 'Enter a valid positive salary.');
        if (valid) { focusCtrl('txtSalary'); valid = false; }
    }

    return valid;
}

/* ---- Delete Confirmation ---- */
function confirmDelete() {
    return confirm('Are you sure you want to delete this employee record?\nThis action cannot be undone.');
}

/* ---- Client-side Grid Search / Filter ---- */
function searchTable(keyword) {
    var kw    = keyword.toLowerCase();
    var table = document.querySelector('.data-table');
    if (!table) return;

    var rows = table.querySelectorAll('tbody tr');
    var visible = 0;

    rows.forEach(function (row) {
        var text = row.textContent.toLowerCase();
        var show = text.indexOf(kw) !== -1;
        row.style.display = show ? '' : 'none';
        if (show) visible++;
    });

    // Update count label
    var lbl = document.querySelector('[id$="lblRecordCount"]');
    if (lbl) {
        lbl.textContent = keyword
            ? ('Showing ' + visible + ' of ' + rows.length + ' record(s)')
            : ('Total Records: ' + rows.length);
    }
}

/* ---- Auto-dismiss alert after 5 s ---- */
(function autoDismissAlert() {
    var alert = document.querySelector('.alert');
    if (!alert) return;
    setTimeout(function () {
        alert.style.transition = 'opacity .6s';
        alert.style.opacity    = '0';
        setTimeout(function () {
            var panel = alert.closest('[id$="pnlMessage"]') || alert.parentElement;
            if (panel) panel.style.display = 'none';
        }, 600);
    }, 5000);
}());
