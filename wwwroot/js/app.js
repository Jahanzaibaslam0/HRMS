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
function hideClientNotice() {
    var el = document.getElementById('clientNotice');
    if (!el) return;
    el.style.display = 'none';
    el.textContent = '';
}
function showClientNotice(msg) {
    var el = document.getElementById('clientNotice');
    if (!el) return;
    el.textContent = msg;
    el.style.display = 'block';
    window.scrollTo({ top: 0, behavior: 'smooth' });
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
    hideClientNotice();

    // Clear all previous errors
    ['errEmpCode','errFirstName','errLastName','errGender',
     'errDepartment','errDesignation','errDOJ','errSalary',
     'errContactRows','errAddressRows','errBankRows'].forEach(clearError);

    // Employee ID
    if (!val('txtEmpCode')) {
        setError('errEmpCode', 'Employee ID is required.');
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

    // Joining Date
    if (!val('txtDOJ')) {
        setError('errDOJ', 'Joining Date is required.');
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

    if (!valid) {
        showClientNotice('Please fix the highlighted errors before saving.');
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
            alert.style.display = 'none';
        }, 600);
    }, 5000);
}());

document.addEventListener('DOMContentLoaded', function () {
    renderInitialRows();
    var firstTabBtn = document.querySelector('.profile-tab-btn[data-tab-target="contactTab"]');
    if (firstTabBtn) {
        switchProfileTab('contactTab', firstTabBtn);
    }
});

function readJsonScript(id) {
    var el = document.getElementById(id);
    if (!el || !el.textContent.trim()) return [];
    try {
        return JSON.parse(el.textContent);
    } catch (_e) {
        return [];
    }
}

function renderInitialRows() {
    var contacts = readJsonScript('initialContactsData');
    var addresses = readJsonScript('initialAddressesData');
    var family = readJsonScript('initialFamilyData');
    var education = readJsonScript('initialEducationData');
    var certificates = readJsonScript('initialCertificatesData');
    var documents = readJsonScript('initialDocumentsData');
    var banks = readJsonScript('initialBanksData');

    if (!contacts.length) contacts = [{ contactType: 'OfficialEmail', isPrimary: true }];
    if (!addresses.length) addresses = [{ addressType: 'Current', isPrimary: true }];

    contacts.forEach(function (c) { addContactRow(c); });
    addresses.forEach(function (a) { addAddressRow(a); });
    if (!family.length) family = [{}];
    family.forEach(function (f) { addFamilyRow(f); });
    if (!education.length) education = [{}];
    education.forEach(function (e) { addEducationRow(e); });
    if (!certificates.length) certificates = [{}];
    certificates.forEach(function (c) { addCertificateRow(c); });
    if (!documents.length) documents = [{}];
    documents.forEach(function (d) { addDocumentRow(d); });
    if (!banks.length) banks = [{}];
    banks.forEach(function (b) { addBankRow(b); });
}

function addContactRow(data) {
    data = data || {};
    var tbody = document.querySelector('#contactTable tbody');
    if (!tbody) return;

    var tr = document.createElement('tr');
    tr.innerHTML = ''
        + '<td><select class="form-control contact-type">'
        + '  <option value="">-- Select --</option>'
        + '  <option value="PersonalEmail">Personal Email</option>'
        + '  <option value="OfficialEmail">Official Email</option>'
        + '  <option value="PersonalMobile">Personal Mobile</option>'
        + '  <option value="OfficialMobile">Official Mobile</option>'
        + '  <option value="WhatsApp">WhatsApp</option>'
        + '  <option value="Emergency">Emergency Contact</option>'
        + '  <option value="PowerBI ID">Power BI ID</option>'
        + '</select></td>'
        + '<td><input type="text" class="form-control contact-name" maxlength="100" /></td>'
        + '<td><input type="text" class="form-control contact-relationship" maxlength="50" /></td>'
        + '<td><input type="text" class="form-control contact-value" maxlength="255" /></td>'
        + '<td><input type="checkbox" class="contact-primary" /></td>'
        + '<td><button type="button" class="btn-icon btn-delete" onclick="removeRow(this)">X</button></td>';

    tbody.appendChild(tr);
    tr.querySelector('.contact-type').value = data.contactType || data.ContactType || '';
    tr.querySelector('.contact-name').value = data.contactName || data.ContactName || '';
    tr.querySelector('.contact-relationship').value = data.relationship || data.Relationship || '';
    tr.querySelector('.contact-value').value = data.contactValue || data.ContactValue || '';
    tr.querySelector('.contact-primary').checked = !!(data.isPrimary || data.IsPrimary);
}

function addAddressRow(data) {
    data = data || {};
    var tbody = document.querySelector('#addressTable tbody');
    if (!tbody) return;

    var tr = document.createElement('tr');
    tr.innerHTML = ''
        + '<td><select class="form-control address-type">'
        + '  <option value="Current">Current</option>'
        + '  <option value="Permanent">Permanent</option>'
        + '  <option value="Temporary">Temporary</option>'
        + '  <option value="Other">Other</option>'
        + '</select></td>'
        + '<td><textarea class="form-control address-line" rows="2"></textarea></td>'
        + '<td><input type="text" class="form-control address-city" maxlength="100" /></td>'
        + '<td><input type="text" class="form-control address-province" maxlength="100" /></td>'
        + '<td><input type="text" class="form-control address-postal" maxlength="10" /></td>'
        + '<td><input type="checkbox" class="address-primary" /></td>'
        + '<td><button type="button" class="btn-icon btn-delete" onclick="removeRow(this)">X</button></td>';

    tbody.appendChild(tr);
    tr.querySelector('.address-type').value = data.addressType || data.AddressType || 'Current';
    tr.querySelector('.address-line').value = data.addressLine || data.AddressLine || '';
    tr.querySelector('.address-city').value = data.city || data.City || '';
    tr.querySelector('.address-province').value = data.provinceState || data.ProvinceState || '';
    tr.querySelector('.address-postal').value = data.postalCode || data.PostalCode || '';
    tr.querySelector('.address-primary').checked = !!(data.isPrimary || data.IsPrimary);
}

function addFamilyRow(data) {
    data = data || {};
    var tbody = document.querySelector('#familyTable tbody');
    if (!tbody) return;

    var tr = document.createElement('tr');
    tr.innerHTML = ''
        + '<td><input type="text" class="form-control family-name" maxlength="150" /></td>'
        + '<td><input type="text" class="form-control family-relationship" maxlength="50" /></td>'
        + '<td><select class="form-control family-gender"><option value="">--</option><option value="Male">Male</option><option value="Female">Female</option><option value="Other">Other</option></select></td>'
        + '<td><input type="date" class="form-control family-dob" /></td>'
        + '<td><input type="text" class="form-control family-contact" maxlength="20" /></td>'
        + '<td><input type="checkbox" class="family-dependent" /></td>'
        + '<td><button type="button" class="btn-icon btn-delete" onclick="removeRow(this)">X</button></td>';

    tbody.appendChild(tr);
    tr.querySelector('.family-name').value = data.memberName || data.MemberName || '';
    tr.querySelector('.family-relationship').value = data.relationship || data.Relationship || '';
    tr.querySelector('.family-gender').value = data.gender || data.Gender || '';
    tr.querySelector('.family-dob').value = data.dateOfBirth || data.DateOfBirth || '';
    tr.querySelector('.family-contact').value = data.contactNumber || data.ContactNumber || '';
    tr.querySelector('.family-dependent').checked = !!(data.isDependent || data.IsDependent);
}

function addEducationRow(data) {
    data = data || {};
    var tbody = document.querySelector('#educationTable tbody');
    if (!tbody) return;

    var tr = document.createElement('tr');
    tr.innerHTML = ''
        + '<td><select class="form-control edu-qualification">'
        + '  <option value="">-- Select --</option>'
        + '  <option value="Matric / O-Level">Matric / O-Level</option>'
        + '  <option value="Intermediate / A-Level">Intermediate / A-Level</option>'
        + '  <option value="Diploma">Diploma</option>'
        + '  <option value="Certificate">Certificate</option>'
        + '  <option value="Bachelor">Bachelor</option>'
        + '  <option value="Master">Master</option>'
        + '  <option value="MPhil">MPhil</option>'
        + '  <option value="PhD">PhD</option>'
        + '  <option value="Other">Other</option>'
        + '</select></td>'
        + '<td><input type="text" class="form-control edu-degree" maxlength="150" /></td>'
        + '<td><input type="text" class="form-control edu-specialization" maxlength="150" /></td>'
        + '<td><input type="text" class="form-control edu-institution" maxlength="200" /></td>'
        + '<td><input type="number" class="form-control edu-year" min="1950" max="2100" /></td>'
        + '<td><input type="text" class="form-control edu-grade" maxlength="20" placeholder="e.g. 3.5 / A+" /></td>'
        + '<td><button type="button" class="btn-icon btn-delete" onclick="removeRow(this)">X</button></td>';

    tbody.appendChild(tr);
    tr.querySelector('.edu-qualification').value = data.highestQualification || data.HighestQualification || '';
    tr.querySelector('.edu-degree').value = data.degreeCertificate || data.DegreeCertificate || '';
    tr.querySelector('.edu-specialization').value = data.specialization || data.Specialization || '';
    tr.querySelector('.edu-institution').value = data.institution || data.Institution || '';
    tr.querySelector('.edu-year').value = data.yearOfPassing || data.YearOfPassing || '';
    tr.querySelector('.edu-grade').value = data.gradeCGPA || data.GradeCGPA || '';
}

function reindexCertificateRows() {
    document.querySelectorAll('#certificateTable tbody tr').forEach(function (tr, idx) {
        tr.setAttribute('data-row-index', idx);
        var fileInput = tr.querySelector('.cert-copy-file');
        if (fileInput) {
            fileInput.name = 'CertCopy_' + idx;
            fileInput.setAttribute('form', 'certificateSaveForm');
        }
    });
}

function addCertificateRow(data) {
    data = data || {};
    var tbody = document.querySelector('#certificateTable tbody');
    if (!tbody) return;

    var rowIndex = tbody.querySelectorAll('tr').length;
    var copyPath = data.certificateCopyPath || data.CertificateCopyPath || '';
    var copyLink = copyPath
        ? '<a class="cert-copy-link" href="' + escapeHtml(copyPath) + '" target="_blank">View</a><br/>'
        : '';

    var tr = document.createElement('tr');
    tr.setAttribute('data-row-index', rowIndex);
    tr.innerHTML = ''
        + '<td><input type="text" class="form-control cert-name" maxlength="200" /></td>'
        + '<td><input type="text" class="form-control cert-body" maxlength="200" /></td>'
        + '<td><input type="text" class="form-control cert-number" maxlength="100" /></td>'
        + '<td><input type="date" class="form-control cert-issue-date" /></td>'
        + '<td><input type="date" class="form-control cert-expiry-date" /></td>'
        + '<td style="text-align:center;"><input type="checkbox" class="cert-renewal" /></td>'
        + '<td>' + copyLink
        + '<input type="file" class="cert-copy-file" form="certificateSaveForm" name="CertCopy_' + rowIndex + '" accept=".pdf,.jpg,.jpeg,.png,.doc,.docx" />'
        + '<input type="hidden" class="cert-copy-path" /></td>'
        + '<td><button type="button" class="btn-icon btn-delete" onclick="removeCertificateRow(this)">X</button></td>';

    tbody.appendChild(tr);
    tr.querySelector('.cert-name').value = data.certificationName || data.CertificationName || '';
    tr.querySelector('.cert-body').value = data.certificationBody || data.CertificationBody || '';
    tr.querySelector('.cert-number').value = data.certificateNumber || data.CertificateNumber || '';
    tr.querySelector('.cert-issue-date').value = data.issueDate || data.IssueDate || '';
    tr.querySelector('.cert-expiry-date').value = data.expiryDate || data.ExpiryDate || '';
    tr.querySelector('.cert-renewal').checked = !!(data.renewalRequired || data.RenewalRequired);
    tr.querySelector('.cert-copy-path').value = copyPath;
    reindexCertificateRows();
}

function removeCertificateRow(btn) {
    var tbody = document.querySelector('#certificateTable tbody');
    if (!tbody || tbody.querySelectorAll('tr').length <= 1) return;
    btn.closest('tr').remove();
    reindexCertificateRows();
}

function reindexDocumentRows() {
    document.querySelectorAll('#documentTable tbody tr').forEach(function (tr, idx) {
        tr.setAttribute('data-row-index', idx);
        var fileInput = tr.querySelector('.doc-file');
        if (fileInput) {
            fileInput.name = 'DocFile_' + idx;
            fileInput.setAttribute('form', 'documentSaveForm');
        }
    });
}

function addDocumentRow(data) {
    data = data || {};
    var tbody = document.querySelector('#documentTable tbody');
    if (!tbody) return;

    var rowIndex = tbody.querySelectorAll('tr').length;
    var docPath = data.documentPath || data.DocumentPath || '';
    var fileName = data.originalFileName || data.OriginalFileName || '';
    var viewLabel = fileName || 'View Document';
    var viewLink = docPath
        ? '<a class="doc-view-link" href="' + escapeHtml(docPath) + '" target="_blank" title="' + escapeHtml(viewLabel) + '">View</a><br/>'
        : '';

    var docTypes = readJsonScript('documentTypeLookupData');
    var typeOptions = '<option value="">-- Select --</option>';
    docTypes.forEach(function (dt) {
        var id = dt.id || dt.Id;
        var name = dt.name || dt.Name || '';
        typeOptions += '<option value="' + escapeHtml(id) + '">' + escapeHtml(name) + '</option>';
    });

    var tr = document.createElement('tr');
    tr.setAttribute('data-row-index', rowIndex);
    tr.innerHTML = ''
        + '<td><select class="form-control doc-type">' + typeOptions + '</select></td>'
        + '<td><input type="text" class="form-control doc-number" maxlength="100" /></td>'
        + '<td><input type="date" class="form-control doc-issue-date" /></td>'
        + '<td><input type="date" class="form-control doc-expiry-date" /></td>'
        + '<td><input type="text" class="form-control doc-remarks" maxlength="250" /></td>'
        + '<td>' + viewLink
        + '<input type="file" class="doc-file" form="documentSaveForm" name="DocFile_' + rowIndex + '" accept=".pdf,.jpg,.jpeg,.png,.doc,.docx" />'
        + '<input type="hidden" class="doc-path" />'
        + '<input type="hidden" class="doc-original-name" /></td>'
        + '<td><select class="form-control doc-verification">'
        + '  <option value="Pending">Pending</option>'
        + '  <option value="Verified">Verified</option>'
        + '  <option value="Rejected">Rejected</option>'
        + '</select></td>'
        + '<td><button type="button" class="btn-icon btn-delete" onclick="removeDocumentRow(this)">X</button></td>';

    tbody.appendChild(tr);
    tr.querySelector('.doc-type').value = data.documentTypeID || data.DocumentTypeID || '';
    tr.querySelector('.doc-number').value = data.documentNumber || data.DocumentNumber || '';
    tr.querySelector('.doc-issue-date').value = data.issueDate || data.IssueDate || '';
    tr.querySelector('.doc-expiry-date').value = data.expiryDate || data.ExpiryDate || '';
    tr.querySelector('.doc-remarks').value = data.remarks || data.Remarks || '';
    tr.querySelector('.doc-path').value = docPath;
    tr.querySelector('.doc-original-name').value = fileName;
    tr.querySelector('.doc-verification').value = data.verificationStatus || data.VerificationStatus || 'Pending';
    reindexDocumentRows();
}

function removeDocumentRow(btn) {
    var tbody = document.querySelector('#documentTable tbody');
    if (!tbody || tbody.querySelectorAll('tr').length <= 1) return;
    btn.closest('tr').remove();
    reindexDocumentRows();
}

function escapeHtml(value) {
    return String(value || '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function addBankRow(data) {
    data = data || {};
    var tbody = document.querySelector('#bankTable tbody');
    if (!tbody) return;

    var bankOptions = readJsonScript('bankLookupData');
    var optionHtml = '<option value="">-- Select Bank --</option>';
    bankOptions.forEach(function (bank) {
        var id = bank.id || bank.Id;
        var name = bank.name || bank.Name || '';
        optionHtml += '<option value="' + escapeHtml(id) + '">' + escapeHtml(name) + '</option>';
    });

    var tr = document.createElement('tr');
    tr.innerHTML = ''
        + '<td><select class="form-control bank-id">' + optionHtml + '</select></td>'
        + '<td><input type="text" class="form-control bank-branch-code" maxlength="50" /></td>'
        + '<td><input type="text" class="form-control bank-branch-name" maxlength="150" /></td>'
        + '<td><input type="text" class="form-control bank-account-title" maxlength="150" /></td>'
        + '<td><input type="text" class="form-control bank-iban" maxlength="50" /></td>'
        + '<td><input type="text" class="form-control bank-swift" maxlength="50" /></td>'
        + '<td><select class="form-control bank-account-type">'
        + '  <option value="">-- Select --</option>'
        + '  <option value="Current">Current</option>'
        + '  <option value="Savings">Savings</option>'
        + '  <option value="Salary">Salary</option>'
        + '  <option value="Other">Other</option>'
        + '</select></td>'
        + '<td><select class="form-control bank-verification-status">'
        + '  <option value="Pending">Pending</option>'
        + '  <option value="Verified">Verified</option>'
        + '  <option value="Rejected">Rejected</option>'
        + '</select></td>'
        + '<td><input type="checkbox" class="bank-primary" /></td>'
        + '<td><button type="button" class="btn-icon btn-delete" onclick="removeRow(this)">X</button></td>';

    tbody.appendChild(tr);
    tr.querySelector('.bank-id').value = data.bankID || data.BankID || '';
    tr.querySelector('.bank-branch-code').value = data.branchCode || data.BranchCode || '';
    tr.querySelector('.bank-branch-name').value = data.branchName || data.BranchName || '';
    tr.querySelector('.bank-account-title').value = data.accountTitle || data.AccountTitle || '';
    tr.querySelector('.bank-iban').value = data.iban || data.IBAN || '';
    tr.querySelector('.bank-swift').value = data.swiftBICCode || data.SwiftBICCode || '';
    tr.querySelector('.bank-account-type').value = data.accountType || data.AccountType || '';
    tr.querySelector('.bank-verification-status').value = data.accountVerificationStatus || data.AccountVerificationStatus || 'Pending';
    tr.querySelector('.bank-primary').checked = !!(data.isPrimary || data.IsPrimary);
}

function removeRow(button) {
    var tr = button.closest('tr');
    if (tr) tr.remove();
}

function readContactRows() {
    return Array.from(document.querySelectorAll('#contactTable tbody tr'))
        .map(function (tr) {
            return {
                contactType: tr.querySelector('.contact-type').value,
                contactName: tr.querySelector('.contact-name').value.trim(),
                relationship: tr.querySelector('.contact-relationship').value.trim(),
                contactValue: tr.querySelector('.contact-value').value.trim(),
                isPrimary: tr.querySelector('.contact-primary').checked
            };
        })
        .filter(function (c) { return c.contactValue || c.contactName; });
}

function readAddressRows() {
    return Array.from(document.querySelectorAll('#addressTable tbody tr'))
        .map(function (tr) {
            return {
                addressType: tr.querySelector('.address-type').value,
                addressLine: tr.querySelector('.address-line').value.trim(),
                city: tr.querySelector('.address-city').value.trim(),
                provinceState: tr.querySelector('.address-province').value.trim(),
                postalCode: tr.querySelector('.address-postal').value.trim(),
                isPrimary: tr.querySelector('.address-primary').checked
            };
        })
        .filter(function (a) { return a.addressLine; });
}

function readFamilyRows() {
    return Array.from(document.querySelectorAll('#familyTable tbody tr'))
        .map(function (tr) {
            return {
                memberName: tr.querySelector('.family-name').value.trim(),
                relationship: tr.querySelector('.family-relationship').value.trim(),
                gender: tr.querySelector('.family-gender').value,
                dateOfBirth: tr.querySelector('.family-dob').value,
                contactNumber: tr.querySelector('.family-contact').value.trim(),
                isDependent: tr.querySelector('.family-dependent').checked
            };
        })
        .filter(function (f) { return f.memberName; });
}

function readEducationRows() {
    return Array.from(document.querySelectorAll('#educationTable tbody tr'))
        .map(function (tr) {
            return {
                highestQualification: tr.querySelector('.edu-qualification').value,
                degreeCertificate: tr.querySelector('.edu-degree').value.trim(),
                specialization: tr.querySelector('.edu-specialization').value.trim(),
                institution: tr.querySelector('.edu-institution').value.trim(),
                yearOfPassing: tr.querySelector('.edu-year').value,
                gradeCGPA: tr.querySelector('.edu-grade').value.trim()
            };
        })
        .filter(function (e) {
            return e.highestQualification || e.degreeCertificate || e.institution;
        });
}

function readCertificateRows() {
    return Array.from(document.querySelectorAll('#certificateTable tbody tr'))
        .map(function (tr) {
            return {
                certificationName: tr.querySelector('.cert-name').value.trim(),
                certificationBody: tr.querySelector('.cert-body').value.trim(),
                certificateNumber: tr.querySelector('.cert-number').value.trim(),
                issueDate: tr.querySelector('.cert-issue-date').value,
                expiryDate: tr.querySelector('.cert-expiry-date').value,
                renewalRequired: tr.querySelector('.cert-renewal').checked,
                certificateCopyPath: tr.querySelector('.cert-copy-path').value
            };
        });
}

function readDocumentRows() {
    return Array.from(document.querySelectorAll('#documentTable tbody tr'))
        .map(function (tr) {
            return {
                documentTypeID: parseInt(tr.querySelector('.doc-type').value || '0', 10),
                documentNumber: tr.querySelector('.doc-number').value.trim(),
                issueDate: tr.querySelector('.doc-issue-date').value,
                expiryDate: tr.querySelector('.doc-expiry-date').value,
                remarks: tr.querySelector('.doc-remarks').value.trim(),
                documentPath: tr.querySelector('.doc-path').value,
                originalFileName: tr.querySelector('.doc-original-name').value,
                verificationStatus: tr.querySelector('.doc-verification').value
            };
        });
}

function readBankRows() {
    return Array.from(document.querySelectorAll('#bankTable tbody tr'))
        .map(function (tr) {
            return {
                bankID: parseInt(tr.querySelector('.bank-id').value || '0', 10),
                branchCode: tr.querySelector('.bank-branch-code').value.trim(),
                branchName: tr.querySelector('.bank-branch-name').value.trim(),
                accountTitle: tr.querySelector('.bank-account-title').value.trim(),
                iban: tr.querySelector('.bank-iban').value.trim(),
                swiftBICCode: tr.querySelector('.bank-swift').value.trim(),
                accountType: tr.querySelector('.bank-account-type').value,
                accountVerificationStatus: tr.querySelector('.bank-verification-status').value,
                isPrimary: tr.querySelector('.bank-primary').checked
            };
        })
        .filter(function (b) { return b.bankID > 0; });
}

function prepareEmployeePayload() {
    if (!validateForm()) return false;

    var contacts = readContactRows();
    var addresses = readAddressRows();
    var family = readFamilyRows();
    var education = readEducationRows();
    var banks = readBankRows();

    document.getElementById('ContactsJson').value = JSON.stringify(contacts);
    document.getElementById('AddressesJson').value = JSON.stringify(addresses);
    document.getElementById('FamilyMembersJson').value = JSON.stringify(family);
    document.getElementById('EducationJson').value = JSON.stringify(education);
    document.getElementById('BanksJson').value = JSON.stringify(banks);
    hideClientNotice();
    return true;
}

function submitProfileSection(section) {
    hideClientNotice();

    var employeeIdEl = document.querySelector('input[name="EmployeeID"]');
    var employeeId = employeeIdEl ? employeeIdEl.value : '';
    var employeeCode = val('txtEmpCode');

    if (!employeeId && !employeeCode) {
        showClientNotice('Please save or select an employee before saving profile details.');
        return false;
    }

    var form;
    if (section === 'contacts') {
        form = document.getElementById('contactSaveForm');
        document.getElementById('contactSaveEmployeeID').value = employeeId;
        document.getElementById('contactSaveEmployeeCode').value = employeeCode;
        document.getElementById('contactSaveJson').value = JSON.stringify(readContactRows());
    } else if (section === 'addresses') {
        form = document.getElementById('addressSaveForm');
        document.getElementById('addressSaveEmployeeID').value = employeeId;
        document.getElementById('addressSaveEmployeeCode').value = employeeCode;
        document.getElementById('addressSaveJson').value = JSON.stringify(readAddressRows());
    } else if (section === 'family') {
        form = document.getElementById('familySaveForm');
        document.getElementById('familySaveEmployeeID').value = employeeId;
        document.getElementById('familySaveEmployeeCode').value = employeeCode;
        document.getElementById('familySaveJson').value = JSON.stringify(readFamilyRows());
    } else if (section === 'education') {
        form = document.getElementById('educationSaveForm');
        document.getElementById('educationSaveEmployeeID').value = employeeId;
        document.getElementById('educationSaveEmployeeCode').value = employeeCode;
        document.getElementById('educationSaveJson').value = JSON.stringify(readEducationRows());
    } else if (section === 'certificates') {
        form = document.getElementById('certificateSaveForm');
        document.getElementById('certificateSaveEmployeeID').value = employeeId;
        document.getElementById('certificateSaveEmployeeCode').value = employeeCode;
        document.getElementById('certificateSaveJson').value = JSON.stringify(readCertificateRows());
        reindexCertificateRows();
    } else if (section === 'documents') {
        form = document.getElementById('documentSaveForm');
        document.getElementById('documentSaveEmployeeID').value = employeeId;
        document.getElementById('documentSaveEmployeeCode').value = employeeCode;
        document.getElementById('documentSaveJson').value = JSON.stringify(readDocumentRows());
        reindexDocumentRows();
    } else if (section === 'banks') {
        form = document.getElementById('bankSaveForm');
        document.getElementById('bankSaveEmployeeID').value = employeeId;
        document.getElementById('bankSaveEmployeeCode').value = employeeCode;
        document.getElementById('bankSaveJson').value = JSON.stringify(readBankRows());
    }

    if (form) form.submit();
    return false;
}

function switchProfileTab(targetTabId, clickedButton) {
    var panels = document.querySelectorAll('.profile-tab-panel');
    var buttons = document.querySelectorAll('.profile-tab-btn');

    panels.forEach(function (panel) {
        panel.classList.toggle('active', panel.id === targetTabId);
    });

    buttons.forEach(function (btn) {
        btn.classList.toggle('active', btn === clickedButton);
    });
}
