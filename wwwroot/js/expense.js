(function () {
    var categories = [];

    function readJsonScript(id) {
        var el = document.getElementById(id);
        if (!el || !el.textContent) return [];
        try { return JSON.parse(el.textContent); } catch (e) { return []; }
    }

    function buildCategoryOptions(selectedId) {
        var html = '<option value="">-- Select --</option>';
        categories.forEach(function (c) {
            var id = c.id || c.Id;
            var name = c.name || c.Name;
            var sel = String(id) === String(selectedId) ? ' selected' : '';
            html += '<option value="' + id + '"' + sel + '>' + name + '</option>';
        });
        return html;
    }

    function paymentOptions(selected) {
        var opts = ['Cash', 'Card', 'Bank Transfer', 'Cheque', 'Mobile Wallet'];
        return opts.map(function (o) {
            return '<option value="' + o + '"' + (o === selected ? ' selected' : '') + '>' + o + '</option>';
        }).join('');
    }

    function currencyOptions(selected) {
        var opts = ['PKR', 'USD', 'EUR', 'GBP', 'AED', 'SAR'];
        var val = selected || 'PKR';
        return opts.map(function (o) {
            return '<option value="' + o + '"' + (o === val ? ' selected' : '') + '>' + o + '</option>';
        }).join('');
    }

    function approvalOptions(selected) {
        var opts = ['Pending', 'Approved', 'Rejected', 'On Hold'];
        var val = selected || 'Pending';
        return opts.map(function (o) {
            return '<option value="' + o + '"' + (o === val ? ' selected' : '') + '>' + o + '</option>';
        }).join('');
    }

    window.addExpenseDetailRow = function (data) {
        data = data || {};
        var tbody = document.querySelector('#expenseDetailTable tbody');
        var rowIndex = tbody.querySelectorAll('tr').length;
        var tr = document.createElement('tr');
        tr.setAttribute('data-row-index', rowIndex);

        var receiptPath = data.originalReceiptDocPath || data.OriginalReceiptDocPath || '';
        var receiptLink = receiptPath
            ? '<a class="receipt-link" href="' + receiptPath + '" target="_blank">View</a><br/>'
            : '';

        tr.innerHTML =
            '<td><select class="form-control detail-category">' + buildCategoryOptions(data.expenseCategoryID || data.ExpenseCategoryID) + '</select></td>' +
            '<td><input type="text" class="form-control detail-description" maxlength="500" /></td>' +
            '<td><select class="form-control detail-payment">' + paymentOptions(data.paymentMethod || data.PaymentMethod || '') + '</select></td>' +
            '<td><input type="date" class="form-control detail-txn-date" /></td>' +
            '<td><select class="form-control detail-currency">' + currencyOptions(data.currency || data.Currency) + '</select></td>' +
            '<td><input type="number" step="0.01" class="form-control detail-txn-amount" /></td>' +
            '<td><input type="number" step="0.01" class="form-control detail-amount" /></td>' +
            '<td><select class="form-control detail-approval">' + approvalOptions(data.approvalStatus || data.ApprovalStatus) + '</select></td>' +
            '<td><input type="text" class="form-control detail-receipt-id" maxlength="100" /></td>' +
            '<td>' + receiptLink +
                '<input type="file" class="detail-receipt-file" name="ReceiptDoc_' + rowIndex + '" accept=".pdf,.jpg,.jpeg,.png,.doc,.docx" />' +
                '<input type="hidden" class="detail-receipt-path" />' +
            '</td>' +
            '<td><button type="button" class="btn btn-danger" style="padding:2px 8px;font-size:.75rem;" onclick="removeExpenseDetailRow(this)">Remove</button></td>';

        tbody.appendChild(tr);

        tr.querySelector('.detail-description').value = data.description || data.Description || '';
        tr.querySelector('.detail-txn-date').value = data.transactionDate || data.TransactionDate || '';
        tr.querySelector('.detail-txn-amount').value = data.transactionAmount || data.TransactionAmount || '';
        tr.querySelector('.detail-amount').value = data.amount || data.Amount || '';
        tr.querySelector('.detail-receipt-id').value = data.originalReceiptID || data.OriginalReceiptID || '';
        tr.querySelector('.detail-receipt-path').value = receiptPath;

        reindexDetailRows();
    };

    window.removeExpenseDetailRow = function (btn) {
        var tbody = document.querySelector('#expenseDetailTable tbody');
        if (tbody.querySelectorAll('tr').length <= 1) return;
        btn.closest('tr').remove();
        reindexDetailRows();
    };

    function reindexDetailRows() {
        document.querySelectorAll('#expenseDetailTable tbody tr').forEach(function (tr, idx) {
            tr.setAttribute('data-row-index', idx);
            var fileInput = tr.querySelector('.detail-receipt-file');
            if (fileInput) fileInput.name = 'ReceiptDoc_' + idx;
        });
    }

    function readDetailRows() {
        return Array.from(document.querySelectorAll('#expenseDetailTable tbody tr')).map(function (tr) {
            return {
                expenseCategoryID: parseInt(tr.querySelector('.detail-category').value, 10) || 0,
                description: tr.querySelector('.detail-description').value.trim(),
                paymentMethod: tr.querySelector('.detail-payment').value,
                transactionDate: tr.querySelector('.detail-txn-date').value,
                currency: tr.querySelector('.detail-currency').value,
                transactionAmount: tr.querySelector('.detail-txn-amount').value,
                amount: tr.querySelector('.detail-amount').value,
                approvalStatus: tr.querySelector('.detail-approval').value,
                originalReceiptID: tr.querySelector('.detail-receipt-id').value.trim(),
                originalReceiptDocPath: tr.querySelector('.detail-receipt-path').value
            };
        }).filter(function (r) {
            return r.expenseCategoryID > 0 || r.description || r.originalReceiptID || r.amount || r.transactionAmount;
        });
    }

    window.prepareExpensePayload = function () {
        var employee = document.getElementById('ddlEmployee');
        if (!employee || !employee.value) {
            alert('Please select an employee.');
            return false;
        }
        document.getElementById('DetailsJson').value = JSON.stringify(readDetailRows());
        return true;
    };

    document.addEventListener('DOMContentLoaded', function () {
        categories = readJsonScript('expenseCategoryData');
        var details = readJsonScript('initialExpenseDetailsData');
        if (!details.length) details = [{}];
        details.forEach(function (d) { addExpenseDetailRow(d); });
    });
})();
