using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class TrainingListItem
{
    public int EmployeeTrainingID { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public string TrainingName { get; set; } = "";
    public string TrainingCode { get; set; } = "";
    public string MandatoryTrainingStatus { get; set; } = "";
    public string TrainingDepartment { get; set; } = "";
    public DateTime? LastTrainingDate { get; set; }
    public DateTime? NextTrainingDue { get; set; }
    public decimal? TrainingHoursYTD { get; set; }
    public decimal? TrainingHoursRequiredAnnual { get; set; }
}

public class TrainingInput
{
    public int EmployeeTrainingID { get; set; }
    public int EmployeeID { get; set; }
    public string MandatoryTrainingStatus { get; set; } = "";
    public string SafetyTrainingValidTill { get; set; } = "";
    public string GMPTrainingValidTill { get; set; } = "";
    public string TrainingHoursYTD { get; set; } = "";
    public string TrainingHoursRequiredAnnual { get; set; } = "";
    public string LastTrainingDate { get; set; } = "";
    public string NextTrainingDue { get; set; } = "";
    public string TrainingName { get; set; } = "";
    public string TrainingCode { get; set; } = "";
    public string TrainingDepartment { get; set; } = "All";
}

public class TrainingMasterModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    public static readonly string[] MandatoryStatuses =
    {
        "Completed", "In Progress", "Not Started", "Overdue", "Exempt", "Not Applicable"
    };

    public TrainingMasterModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    public List<TrainingListItem> Records { get; set; } = new();
    public List<LookupItem> Employees { get; set; } = new();
    public List<LookupItem> Departments { get; set; } = new();
    public TrainingInput Input { get; set; } = new();
    public IReadOnlyList<string> MandatoryStatusOptions => MandatoryStatuses;
    public bool EditMode { get; set; }
    public bool ShowForm { get; set; }
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet([FromQuery] int? editId, [FromQuery] bool? newRecord)
    {
        LoadAlert();
        ShowForm = (editId.HasValue && editId > 0) || newRecord == true;

        if (ShowForm)
        {
            LoadEmployees();
            LoadDepartments();
            if (editId.HasValue && editId > 0)
            {
                LoadForEdit(editId.Value);
                EditMode = true;
            }
        }
        else
        {
            LoadRecords();
        }
    }

    public IActionResult OnPost(
        int EmployeeTrainingID,
        bool EditMode,
        int EmployeeID,
        string MandatoryTrainingStatus,
        string SafetyTrainingValidTill,
        string GMPTrainingValidTill,
        string TrainingHoursYTD,
        string TrainingHoursRequiredAnnual,
        string LastTrainingDate,
        string NextTrainingDue,
        string TrainingName,
        string TrainingCode,
        string TrainingDepartment)
    {
        this.EditMode = EditMode;
        Input = new TrainingInput
        {
            EmployeeTrainingID = EmployeeTrainingID,
            EmployeeID = EmployeeID,
            MandatoryTrainingStatus = MandatoryTrainingStatus ?? "",
            SafetyTrainingValidTill = SafetyTrainingValidTill ?? "",
            GMPTrainingValidTill = GMPTrainingValidTill ?? "",
            TrainingHoursYTD = TrainingHoursYTD ?? "",
            TrainingHoursRequiredAnnual = TrainingHoursRequiredAnnual ?? "",
            LastTrainingDate = LastTrainingDate ?? "",
            NextTrainingDue = NextTrainingDue ?? "",
            TrainingName = TrainingName?.Trim() ?? "",
            TrainingCode = TrainingCode?.Trim() ?? "",
            TrainingDepartment = string.IsNullOrWhiteSpace(TrainingDepartment) ? "All" : TrainingDepartment
        };

        if (EmployeeID <= 0)
        {
            AlertMessage = "Employee is required.";
            AlertType = "error";
            LoadEmployees();
            LoadDepartments();
            ShowForm = true;
            return Page();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            SaveRecord(conn, Input);

            TempData["Alert"] = EditMode ? "Training record updated successfully." : "Training record added successfully.";
            TempData["AlertType"] = "success";
            return RedirectToPage(new { editId = Input.EmployeeTrainingID });
        }
        catch (Exception ex)
        {
            AlertMessage = "Error: " + ex.Message;
            AlertType = "error";
            LoadEmployees();
            LoadDepartments();
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
            using var cmd = new SqlCommand("DELETE FROM tblEmployeeTraining WHERE EmployeeTrainingID = @Id;", conn);
            cmd.Parameters.AddWithValue("@Id", deleteId);
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Training record deleted successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error deleting record: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private void SaveRecord(SqlConnection conn, TrainingInput input)
    {
        if (input.EmployeeTrainingID > 0)
        {
            using var cmd = new SqlCommand(@"
                UPDATE tblEmployeeTraining
                SET EmployeeID = @EmployeeID,
                    MandatoryTrainingStatus = @MandatoryTrainingStatus,
                    SafetyTrainingValidTill = @SafetyTrainingValidTill,
                    GMPTrainingValidTill = @GMPTrainingValidTill,
                    TrainingHoursYTD = @TrainingHoursYTD,
                    TrainingHoursRequiredAnnual = @TrainingHoursRequiredAnnual,
                    LastTrainingDate = @LastTrainingDate,
                    NextTrainingDue = @NextTrainingDue,
                    TrainingName = @TrainingName,
                    TrainingCode = @TrainingCode,
                    TrainingDepartment = @TrainingDepartment,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE EmployeeTrainingID = @EmployeeTrainingID;", conn);

            BindParams(cmd, input);
            cmd.Parameters.AddWithValue("@EmployeeTrainingID", input.EmployeeTrainingID);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            cmd.ExecuteNonQuery();
            return;
        }

        using var ins = new SqlCommand(@"
            INSERT INTO tblEmployeeTraining
                (EmployeeID, MandatoryTrainingStatus, SafetyTrainingValidTill, GMPTrainingValidTill,
                 TrainingHoursYTD, TrainingHoursRequiredAnnual, LastTrainingDate, NextTrainingDue,
                 TrainingName, TrainingCode, TrainingDepartment, CreatedOn, CreatedByUserID)
            VALUES
                (@EmployeeID, @MandatoryTrainingStatus, @SafetyTrainingValidTill, @GMPTrainingValidTill,
                 @TrainingHoursYTD, @TrainingHoursRequiredAnnual, @LastTrainingDate, @NextTrainingDue,
                 @TrainingName, @TrainingCode, @TrainingDepartment, GETDATE(), @CreatedByUserID);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);

        BindParams(ins, input);
        AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
        input.EmployeeTrainingID = (int)ins.ExecuteScalar()!;
    }

    private static void BindParams(SqlCommand cmd, TrainingInput input)
    {
        cmd.Parameters.AddWithValue("@EmployeeID", input.EmployeeID);
        cmd.Parameters.AddWithValue("@MandatoryTrainingStatus", string.IsNullOrWhiteSpace(input.MandatoryTrainingStatus) ? DBNull.Value : input.MandatoryTrainingStatus);
        cmd.Parameters.AddWithValue("@SafetyTrainingValidTill", ParseDate(input.SafetyTrainingValidTill));
        cmd.Parameters.AddWithValue("@GMPTrainingValidTill", ParseDate(input.GMPTrainingValidTill));
        cmd.Parameters.AddWithValue("@TrainingHoursYTD", ParseDecimal(input.TrainingHoursYTD));
        cmd.Parameters.AddWithValue("@TrainingHoursRequiredAnnual", ParseDecimal(input.TrainingHoursRequiredAnnual));
        cmd.Parameters.AddWithValue("@LastTrainingDate", ParseDate(input.LastTrainingDate));
        cmd.Parameters.AddWithValue("@NextTrainingDue", ParseDate(input.NextTrainingDue));
        cmd.Parameters.AddWithValue("@TrainingName", string.IsNullOrWhiteSpace(input.TrainingName) ? DBNull.Value : input.TrainingName);
        cmd.Parameters.AddWithValue("@TrainingCode", string.IsNullOrWhiteSpace(input.TrainingCode) ? DBNull.Value : input.TrainingCode);
        cmd.Parameters.AddWithValue("@TrainingDepartment", input.TrainingDepartment);
    }

    private void LoadRecords()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT
                t.EmployeeTrainingID,
                e.EmployeeCode,
                e.FirstName + ' ' + e.LastName AS EmployeeName,
                ISNULL(t.TrainingName, '') AS TrainingName,
                ISNULL(t.TrainingCode, '') AS TrainingCode,
                ISNULL(t.MandatoryTrainingStatus, '') AS MandatoryTrainingStatus,
                ISNULL(t.TrainingDepartment, 'All') AS TrainingDepartment,
                t.LastTrainingDate,
                t.NextTrainingDue,
                t.TrainingHoursYTD,
                t.TrainingHoursRequiredAnnual
            FROM tblEmployeeTraining t
            INNER JOIN tblEmployee e ON e.EmployeeID = t.EmployeeID
            ORDER BY t.EmployeeTrainingID DESC;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Records.Add(new TrainingListItem
            {
                EmployeeTrainingID = dr.GetInt32(0),
                EmployeeCode = dr.GetString(1),
                EmployeeName = dr.GetString(2),
                TrainingName = dr.GetString(3),
                TrainingCode = dr.GetString(4),
                MandatoryTrainingStatus = dr.GetString(5),
                TrainingDepartment = dr.GetString(6),
                LastTrainingDate = dr.IsDBNull(7) ? null : dr.GetDateTime(7),
                NextTrainingDue = dr.IsDBNull(8) ? null : dr.GetDateTime(8),
                TrainingHoursYTD = dr.IsDBNull(9) ? null : dr.GetDecimal(9),
                TrainingHoursRequiredAnnual = dr.IsDBNull(10) ? null : dr.GetDecimal(10)
            });
        }
    }

    private void LoadForEdit(int trainingId)
    {
        using var conn = new SqlConnection(_conn);
        conn.Open();
        using var cmd = new SqlCommand(@"
            SELECT EmployeeTrainingID, EmployeeID, MandatoryTrainingStatus, SafetyTrainingValidTill,
                   GMPTrainingValidTill, TrainingHoursYTD, TrainingHoursRequiredAnnual,
                   LastTrainingDate, NextTrainingDue, TrainingName, TrainingCode, TrainingDepartment
            FROM tblEmployeeTraining
            WHERE EmployeeTrainingID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", trainingId);

        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            Input = new TrainingInput
            {
                EmployeeTrainingID = dr.GetInt32(0),
                EmployeeID = dr.GetInt32(1),
                MandatoryTrainingStatus = dr.IsDBNull(2) ? "" : dr.GetString(2),
                SafetyTrainingValidTill = dr.IsDBNull(3) ? "" : dr.GetDateTime(3).ToString("yyyy-MM-dd"),
                GMPTrainingValidTill = dr.IsDBNull(4) ? "" : dr.GetDateTime(4).ToString("yyyy-MM-dd"),
                TrainingHoursYTD = dr.IsDBNull(5) ? "" : dr.GetDecimal(5).ToString("0.##"),
                TrainingHoursRequiredAnnual = dr.IsDBNull(6) ? "" : dr.GetDecimal(6).ToString("0.##"),
                LastTrainingDate = dr.IsDBNull(7) ? "" : dr.GetDateTime(7).ToString("yyyy-MM-dd"),
                NextTrainingDue = dr.IsDBNull(8) ? "" : dr.GetDateTime(8).ToString("yyyy-MM-dd"),
                TrainingName = dr.IsDBNull(9) ? "" : dr.GetString(9),
                TrainingCode = dr.IsDBNull(10) ? "" : dr.GetString(10),
                TrainingDepartment = dr.IsDBNull(11) ? "All" : dr.GetString(11)
            };
        }
    }

    private void LoadEmployees()
    {
        Employees = new List<LookupItem>();
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
            Employees.Add(new LookupItem
            {
                Id = dr.GetInt32(0),
                Name = string.IsNullOrEmpty(code) ? name : $"{code} – {name}"
            });
        }
    }

    private void LoadDepartments()
    {
        Departments = new List<LookupItem> { new() { Id = 0, Name = "All" } };
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT DepartmentID, DepartmentName
            FROM tblDepartment
            WHERE IsActive = 1
            ORDER BY DepartmentName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Departments.Add(new LookupItem
            {
                Id = dr.GetInt32(0),
                Name = dr.GetString(1)
            });
        }
    }

    private void LoadAlert()
    {
        if (TempData["Alert"] is string msg) AlertMessage = msg;
        if (TempData["AlertType"] is string type) AlertType = type;
    }

    private static object ParseDate(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : (object)DateTime.Parse(value);

    private static object ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
        return decimal.TryParse(value, out var d) ? d : DBNull.Value;
    }
}
