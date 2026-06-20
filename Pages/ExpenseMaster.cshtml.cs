using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace HRMS.Pages;

public class ExpenseListItem
{
    public int ExpenseID { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public DateTime? ExpenseDate { get; set; }
    public string LocationName { get; set; } = "";
    public string ExpensePurpose { get; set; } = "";
    public string WorkflowStatus { get; set; } = "";
    public string DocumentStatus { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public int LineCount { get; set; }
}

public class ExpenseInput
{
    public int ExpenseID { get; set; }
    public int EmployeeID { get; set; }
    public string ExpenseDate { get; set; } = "";
    public int LocationID { get; set; }
    public string ExpensePurpose { get; set; } = "";
    public string WorkflowStatus { get; set; } = "Draft";
    public string VehicleNo { get; set; } = "";
    public string MeterReading { get; set; } = "";
    public string DocumentStatus { get; set; } = "Pending";
}

public class ExpenseDetailInput
{
    public int ExpenseCategoryID { get; set; }
    public string Description { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string TransactionDate { get; set; } = "";
    public string Currency { get; set; } = "PKR";
    public string TransactionAmount { get; set; } = "";
    public string Amount { get; set; } = "";
    public string ApprovalStatus { get; set; } = "Pending";
    public string OriginalReceiptID { get; set; } = "";
    public string OriginalReceiptDocPath { get; set; } = "";
}

public class ExpenseMasterModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;
    private readonly IWebHostEnvironment _env;

    public ExpenseMasterModel(IConfiguration config, AuthService auth, IWebHostEnvironment env)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
        _env = env;
    }

    public List<ExpenseListItem> Expenses { get; set; } = new();
    public List<LookupItem> Employees { get; set; } = new();
    public List<LookupItem> Locations { get; set; } = new();
    public List<LookupItem> ExpenseCategories { get; set; } = new();
    public ExpenseInput Input { get; set; } = new();
    public List<ExpenseDetailInput> DetailRecords { get; set; } = new();
    public bool EditMode { get; set; }
    public bool ShowForm { get; set; }
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet([FromQuery] int? editId, [FromQuery] bool? newExpense)
    {
        LoadAlert();
        ShowForm = (editId.HasValue && editId > 0) || newExpense == true;

        if (ShowForm)
        {
            LoadLookups();
            if (editId.HasValue && editId > 0)
            {
                LoadForEdit(editId.Value);
                EditMode = true;
            }
            else
            {
                EnsureDefaultDetailRow();
            }
        }
        else
        {
            LoadExpenses();
        }
    }

    public IActionResult OnPost(
        int ExpenseID,
        bool EditMode,
        int EmployeeID,
        string ExpenseDate,
        int LocationID,
        string ExpensePurpose,
        string WorkflowStatus,
        string VehicleNo,
        string MeterReading,
        string DocumentStatus,
        string DetailsJson)
    {
        this.EditMode = EditMode;
        Input = new ExpenseInput
        {
            ExpenseID = ExpenseID,
            EmployeeID = EmployeeID,
            ExpenseDate = ExpenseDate ?? "",
            LocationID = LocationID,
            ExpensePurpose = ExpensePurpose?.Trim() ?? "",
            WorkflowStatus = WorkflowStatus ?? "Draft",
            VehicleNo = VehicleNo?.Trim() ?? "",
            MeterReading = MeterReading?.Trim() ?? "",
            DocumentStatus = DocumentStatus ?? "Pending"
        };
        DetailRecords = DeserializeList<ExpenseDetailInput>(DetailsJson);

        if (EmployeeID <= 0)
        {
            AlertMessage = "Employee is required.";
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            return Page();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            int expenseId = SaveExpenseCore(conn, tx, Input);
            ReplaceExpenseDetails(conn, tx, expenseId, DetailRecords);

            tx.Commit();

            TempData["Alert"] = EditMode ? "Expense updated successfully." : "Expense added successfully.";
            TempData["AlertType"] = "success";
            return RedirectToPage(new { editId = expenseId });
        }
        catch (Exception ex)
        {
            AlertMessage = "Error: " + ex.Message;
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            return Page();
        }
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var delDetails = new SqlCommand("DELETE FROM tblExpenseDetail WHERE ExpenseID = @Id;", conn);
            delDetails.Parameters.AddWithValue("@Id", deleteId);
            delDetails.ExecuteNonQuery();

            using var delHeader = new SqlCommand("DELETE FROM tblExpense WHERE ExpenseID = @Id;", conn);
            delHeader.Parameters.AddWithValue("@Id", deleteId);
            delHeader.ExecuteNonQuery();

            TempData["Alert"] = "Expense deleted successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error deleting expense: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private int SaveExpenseCore(SqlConnection conn, SqlTransaction tx, ExpenseInput input)
    {
        DateTime? expenseDate = ParseDate(input.ExpenseDate);

        if (input.ExpenseID > 0)
        {
            using var cmd = new SqlCommand(@"
                UPDATE tblExpense
                SET EmployeeID = @EmployeeID,
                    ExpenseDate = @ExpenseDate,
                    LocationID = @LocationID,
                    ExpensePurpose = @ExpensePurpose,
                    WorkflowStatus = @WorkflowStatus,
                    VehicleNo = @VehicleNo,
                    MeterReading = @MeterReading,
                    DocumentStatus = @DocumentStatus,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE ExpenseID = @ExpenseID;", conn, tx);

            cmd.Parameters.AddWithValue("@ExpenseID", input.ExpenseID);
            cmd.Parameters.AddWithValue("@EmployeeID", input.EmployeeID);
            cmd.Parameters.AddWithValue("@ExpenseDate", (object?)expenseDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LocationID", input.LocationID > 0 ? input.LocationID : DBNull.Value);
            cmd.Parameters.AddWithValue("@ExpensePurpose", input.ExpensePurpose);
            cmd.Parameters.AddWithValue("@WorkflowStatus", input.WorkflowStatus);
            cmd.Parameters.AddWithValue("@VehicleNo", input.VehicleNo);
            cmd.Parameters.AddWithValue("@MeterReading", input.MeterReading);
            cmd.Parameters.AddWithValue("@DocumentStatus", input.DocumentStatus);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            cmd.ExecuteNonQuery();
            return input.ExpenseID;
        }

        using var ins = new SqlCommand(@"
            INSERT INTO tblExpense
                (EmployeeID, ExpenseDate, LocationID, ExpensePurpose, WorkflowStatus,
                 VehicleNo, MeterReading, DocumentStatus, CreatedOn, CreatedByUserID)
            VALUES
                (@EmployeeID, @ExpenseDate, @LocationID, @ExpensePurpose, @WorkflowStatus,
                 @VehicleNo, @MeterReading, @DocumentStatus, GETDATE(), @CreatedByUserID);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);

        ins.Parameters.AddWithValue("@EmployeeID", input.EmployeeID);
        ins.Parameters.AddWithValue("@ExpenseDate", (object?)expenseDate ?? DBNull.Value);
        ins.Parameters.AddWithValue("@LocationID", input.LocationID > 0 ? input.LocationID : DBNull.Value);
        ins.Parameters.AddWithValue("@ExpensePurpose", input.ExpensePurpose);
        ins.Parameters.AddWithValue("@WorkflowStatus", input.WorkflowStatus);
        ins.Parameters.AddWithValue("@VehicleNo", input.VehicleNo);
        ins.Parameters.AddWithValue("@MeterReading", input.MeterReading);
        ins.Parameters.AddWithValue("@DocumentStatus", input.DocumentStatus);
        AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
        return (int)ins.ExecuteScalar()!;
    }

    private void ReplaceExpenseDetails(SqlConnection conn, SqlTransaction tx, int expenseId, List<ExpenseDetailInput> details)
    {
        using (var del = new SqlCommand("DELETE FROM tblExpenseDetail WHERE ExpenseID = @ExpenseID;", conn, tx))
        {
            del.Parameters.AddWithValue("@ExpenseID", expenseId);
            del.ExecuteNonQuery();
        }

        int sort = 0;
        foreach (var line in details.Where(HasDetailContent))
        {
            var docPath = line.OriginalReceiptDocPath;
            var file = Request.Form.Files[$"ReceiptDoc_{sort}"];
            var uploaded = SaveReceiptFile(file, expenseId, sort);
            if (!string.IsNullOrEmpty(uploaded))
                docPath = uploaded;

            using var ins = new SqlCommand(@"
                INSERT INTO tblExpenseDetail
                    (ExpenseID, ExpenseCategoryID, Description, PaymentMethod, TransactionDate,
                     Currency, TransactionAmount, Amount, ApprovalStatus,
                     OriginalReceiptID, OriginalReceiptDocPath, SortOrder,
                     CreatedOn, CreatedByUserID)
                VALUES
                    (@ExpenseID, @ExpenseCategoryID, @Description, @PaymentMethod, @TransactionDate,
                     @Currency, @TransactionAmount, @Amount, @ApprovalStatus,
                     @OriginalReceiptID, @OriginalReceiptDocPath, @SortOrder,
                     GETDATE(), @CreatedByUserID);", conn, tx);

            ins.Parameters.AddWithValue("@ExpenseID", expenseId);
            ins.Parameters.AddWithValue("@ExpenseCategoryID", line.ExpenseCategoryID > 0 ? line.ExpenseCategoryID : DBNull.Value);
            ins.Parameters.AddWithValue("@Description", line.Description?.Trim() ?? "");
            ins.Parameters.AddWithValue("@PaymentMethod", line.PaymentMethod ?? "");
            ins.Parameters.AddWithValue("@TransactionDate", (object?)ParseDate(line.TransactionDate) ?? DBNull.Value);
            ins.Parameters.AddWithValue("@Currency", string.IsNullOrWhiteSpace(line.Currency) ? "PKR" : line.Currency);
            ins.Parameters.AddWithValue("@TransactionAmount", ParseDecimal(line.TransactionAmount));
            ins.Parameters.AddWithValue("@Amount", ParseDecimal(line.Amount));
            ins.Parameters.AddWithValue("@ApprovalStatus", line.ApprovalStatus ?? "Pending");
            ins.Parameters.AddWithValue("@OriginalReceiptID", line.OriginalReceiptID?.Trim() ?? "");
            ins.Parameters.AddWithValue("@OriginalReceiptDocPath", docPath ?? "");
            ins.Parameters.AddWithValue("@SortOrder", sort);
            AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
            ins.ExecuteNonQuery();
            sort++;
        }
    }

    private static bool HasDetailContent(ExpenseDetailInput line) =>
        line.ExpenseCategoryID > 0
        || !string.IsNullOrWhiteSpace(line.Description)
        || !string.IsNullOrWhiteSpace(line.OriginalReceiptID)
        || ParseDecimal(line.Amount) != DBNull.Value;

    private string? SaveReceiptFile(IFormFile? file, int expenseId, int rowIndex)
    {
        if (file == null || file.Length == 0)
            return null;

        var uploads = Path.Combine(_env.WebRootPath, "uploads", "expense-receipts");
        Directory.CreateDirectory(uploads);

        var ext = Path.GetExtension(file.FileName);
        var safeName = $"{expenseId}_{rowIndex}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
        var fullPath = Path.Combine(uploads, safeName);

        using (var fs = System.IO.File.Create(fullPath))
            file.CopyTo(fs);

        return $"/uploads/expense-receipts/{safeName}";
    }

    private void LoadExpenses()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT
                e.ExpenseID,
                emp.EmployeeCode,
                emp.FirstName + ' ' + emp.LastName AS EmployeeName,
                e.ExpenseDate,
                ISNULL(loc.LocationName, '') AS LocationName,
                ISNULL(e.ExpensePurpose, '') AS ExpensePurpose,
                ISNULL(e.WorkflowStatus, '') AS WorkflowStatus,
                ISNULL(e.DocumentStatus, '') AS DocumentStatus,
                ISNULL(agg.TotalAmount, 0) AS TotalAmount,
                ISNULL(agg.LineCount, 0) AS LineCount
            FROM tblExpense e
            INNER JOIN tblEmployee emp ON emp.EmployeeID = e.EmployeeID
            LEFT JOIN tblLocation loc ON loc.LocationID = e.LocationID
            LEFT JOIN (
                SELECT ExpenseID,
                       SUM(ISNULL(Amount, 0)) AS TotalAmount,
                       COUNT(*) AS LineCount
                FROM tblExpenseDetail
                GROUP BY ExpenseID
            ) agg ON agg.ExpenseID = e.ExpenseID
            ORDER BY e.ExpenseID DESC;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Expenses.Add(new ExpenseListItem
            {
                ExpenseID = dr.GetInt32(0),
                EmployeeCode = dr.GetString(1),
                EmployeeName = dr.GetString(2),
                ExpenseDate = dr.IsDBNull(3) ? null : dr.GetDateTime(3),
                LocationName = dr.GetString(4),
                ExpensePurpose = dr.GetString(5),
                WorkflowStatus = dr.GetString(6),
                DocumentStatus = dr.GetString(7),
                TotalAmount = dr.GetDecimal(8),
                LineCount = dr.GetInt32(9)
            });
        }
    }

    private void LoadForEdit(int expenseId)
    {
        using var conn = new SqlConnection(_conn);
        conn.Open();

        using (var cmd = new SqlCommand(@"
            SELECT ExpenseID, EmployeeID, ExpenseDate, LocationID, ExpensePurpose,
                   WorkflowStatus, VehicleNo, MeterReading, DocumentStatus
            FROM tblExpense WHERE ExpenseID = @Id;", conn))
        {
            cmd.Parameters.AddWithValue("@Id", expenseId);
            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                Input = new ExpenseInput
                {
                    ExpenseID = dr.GetInt32(0),
                    EmployeeID = dr.GetInt32(1),
                    ExpenseDate = dr.IsDBNull(2) ? "" : dr.GetDateTime(2).ToString("yyyy-MM-dd"),
                    LocationID = dr.IsDBNull(3) ? 0 : dr.GetInt32(3),
                    ExpensePurpose = dr.IsDBNull(4) ? "" : dr.GetString(4),
                    WorkflowStatus = dr.IsDBNull(5) ? "Draft" : dr.GetString(5),
                    VehicleNo = dr.IsDBNull(6) ? "" : dr.GetString(6),
                    MeterReading = dr.IsDBNull(7) ? "" : dr.GetString(7),
                    DocumentStatus = dr.IsDBNull(8) ? "Pending" : dr.GetString(8)
                };
            }
        }

        using (var cmd = new SqlCommand(@"
            SELECT ExpenseCategoryID, Description, PaymentMethod, TransactionDate, Currency,
                   TransactionAmount, Amount, ApprovalStatus, OriginalReceiptID, OriginalReceiptDocPath
            FROM tblExpenseDetail
            WHERE ExpenseID = @Id
            ORDER BY SortOrder, DetailID;", conn))
        {
            cmd.Parameters.AddWithValue("@Id", expenseId);
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                DetailRecords.Add(new ExpenseDetailInput
                {
                    ExpenseCategoryID = dr.IsDBNull(0) ? 0 : dr.GetInt32(0),
                    Description = dr.IsDBNull(1) ? "" : dr.GetString(1),
                    PaymentMethod = dr.IsDBNull(2) ? "" : dr.GetString(2),
                    TransactionDate = dr.IsDBNull(3) ? "" : dr.GetDateTime(3).ToString("yyyy-MM-dd"),
                    Currency = dr.IsDBNull(4) ? "PKR" : dr.GetString(4),
                    TransactionAmount = dr.IsDBNull(5) ? "" : dr.GetDecimal(5).ToString("0.##"),
                    Amount = dr.IsDBNull(6) ? "" : dr.GetDecimal(6).ToString("0.##"),
                    ApprovalStatus = dr.IsDBNull(7) ? "Pending" : dr.GetString(7),
                    OriginalReceiptID = dr.IsDBNull(8) ? "" : dr.GetString(8),
                    OriginalReceiptDocPath = dr.IsDBNull(9) ? "" : dr.GetString(9)
                });
            }
        }

        if (DetailRecords.Count == 0)
            EnsureDefaultDetailRow();
    }

    private void LoadLookups()
    {
        Employees = LoadEmployeeLookup();
        Locations = LoadLookup("tblLocation", "LocationID", "LocationName");
        ExpenseCategories = LoadLookup("tblExpenseCategory", "ExpenseCategoryID", "ExpenseCategoryName");
    }

    private List<LookupItem> LoadEmployeeLookup()
    {
        var items = new List<LookupItem>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT EmployeeID, EmployeeCode, FirstName, LastName
            FROM tblEmployee
            WHERE Status = 'Active'
            ORDER BY FirstName, LastName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var code = dr.IsDBNull(1) ? "" : dr.GetString(1);
            var name = $"{dr.GetString(2)} {dr.GetString(3)}".Trim();
            items.Add(new LookupItem
            {
                Id = dr.GetInt32(0),
                Name = string.IsNullOrEmpty(code) ? name : $"{code} – {name}"
            });
        }
        return items;
    }

    private List<LookupItem> LoadLookup(string table, string idCol, string nameCol)
    {
        var items = new List<LookupItem>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT {idCol}, {nameCol}
            FROM {table}
            WHERE IsActive = 1
            ORDER BY {nameCol};", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            items.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
        }
        return items;
    }

    private void EnsureDefaultDetailRow()
    {
        if (DetailRecords.Count == 0)
            DetailRecords.Add(new ExpenseDetailInput());
    }

    private void LoadAlert()
    {
        if (TempData["Alert"] is string msg)
            AlertMessage = msg;
        if (TempData["AlertType"] is string type)
            AlertType = type;
    }

    private static List<T> DeserializeList<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<T>();
        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    private static DateTime? ParseDate(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : DateTime.Parse(value);

    private static object ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DBNull.Value;
        return decimal.TryParse(value, out var d) ? d : DBNull.Value;
    }
}
