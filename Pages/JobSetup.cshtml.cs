using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class JobRecord
{
    public int    JobID                        { get; set; }
    public string JobTitle                     { get; set; } = "";
    public string JobCode                      { get; set; } = "";
    public int    GradeID                      { get; set; }
    public string GradeName                    { get; set; } = "";
    public string JobLevel                     { get; set; } = "";
    public string PositionNumber               { get; set; } = "";
    public int    ReportsToEmployeeID          { get; set; }
    public string ReportsToName                { get; set; } = "";
    public int    FunctionalManagerEmployeeID  { get; set; }
    public string FunctionalManagerName        { get; set; } = "";
    public int    DottedLineManagerEmployeeID  { get; set; }
    public string DottedLineManagerName        { get; set; } = "";
    public int    BackupApproverEmployeeID     { get; set; }
    public string BackupApproverName           { get; set; } = "";
    public bool   IsActive                     { get; set; } = true;
}

public class JobSetupModel : PageModel
{
    private readonly string _conn;

    public static readonly string[] JobLevels = new[]
    {
        "Entry", "Standard", "Senior", "Lead", "Manager", "Director", "Executive"
    };

    public JobSetupModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public string PageTitle => "Job Setup";

    public JobRecord           Input        { get; set; } = new() { IsActive = true };
    public List<JobRecord>     Records      { get; set; } = new();
    public List<LookupItem>    Grades       { get; set; } = new();
    public List<LookupItem>    Employees    { get; set; } = new();
    public string              AlertMessage { get; set; } = "";
    public string              AlertType    { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        LoadGrades();
        LoadEmployees();

        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);
        else
        {
            Input.JobCode        = GenerateNextJobCode();
            Input.PositionNumber = GenerateNextPositionNumber();
        }

        LoadRecords();
    }

    public IActionResult OnPostSave(
        int jobID, string jobTitle, string jobCode, int gradeID, string jobLevel,
        string positionNumber,
        int reportsToEmployeeID, int functionalManagerEmployeeID,
        int dottedLineManagerEmployeeID, int backupApproverEmployeeID,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(jobTitle))
        {
            TempData["Alert"] = "Job Title is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = jobID > 0 ? jobID : (int?)null });
        }

        if (string.IsNullOrWhiteSpace(jobCode))
        {
            TempData["Alert"] = "Job Code is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = jobID > 0 ? jobID : (int?)null });
        }

        if (gradeID <= 0)
        {
            TempData["Alert"] = "Job Grade is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = jobID > 0 ? jobID : (int?)null });
        }

        if (string.IsNullOrWhiteSpace(jobLevel))
        {
            TempData["Alert"] = "Job Level is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = jobID > 0 ? jobID : (int?)null });
        }

        if (string.IsNullOrWhiteSpace(positionNumber))
        {
            TempData["Alert"] = "Position Number is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = jobID > 0 ? jobID : (int?)null });
        }

        if (reportsToEmployeeID <= 0)
        {
            TempData["Alert"] = "Reports To (Supervisor) is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = jobID > 0 ? jobID : (int?)null });
        }

        if (backupApproverEmployeeID <= 0)
        {
            TempData["Alert"] = "Backup Approver is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = jobID > 0 ? jobID : (int?)null });
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (jobID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblJob SET
                        JobTitle                    = @JobTitle,
                        JobCode                     = @JobCode,
                        GradeID                     = @GradeID,
                        JobLevel                    = @JobLevel,
                        PositionNumber              = @PositionNumber,
                        ReportsToEmployeeID         = @ReportsToEmployeeID,
                        FunctionalManagerEmployeeID = @FunctionalManagerEmployeeID,
                        DottedLineManagerEmployeeID = @DottedLineManagerEmployeeID,
                        BackupApproverEmployeeID    = @BackupApproverEmployeeID,
                        IsActive                    = @IsActive,
                        ModifiedOn                  = GETDATE()
                    WHERE JobID = @JobID;", conn);
                AddParams(cmd, jobID, jobTitle, jobCode, gradeID, jobLevel, positionNumber,
                    reportsToEmployeeID, functionalManagerEmployeeID,
                    dottedLineManagerEmployeeID, backupApproverEmployeeID, isActive);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Job updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblJob
                        (JobTitle, JobCode, GradeID, JobLevel, PositionNumber,
                         ReportsToEmployeeID, FunctionalManagerEmployeeID,
                         DottedLineManagerEmployeeID, BackupApproverEmployeeID, IsActive)
                    VALUES
                        (@JobTitle, @JobCode, @GradeID, @JobLevel, @PositionNumber,
                         @ReportsToEmployeeID, @FunctionalManagerEmployeeID,
                         @DottedLineManagerEmployeeID, @BackupApproverEmployeeID, @IsActive);", conn);
                AddParams(cmd, 0, jobTitle, jobCode, gradeID, jobLevel, positionNumber,
                    reportsToEmployeeID, functionalManagerEmployeeID,
                    dottedLineManagerEmployeeID, backupApproverEmployeeID, isActive);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Job added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"]     = "Job Code or Position Number already exists.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = jobID > 0 ? jobID : (int?)null });
        }
        catch (Exception ex)
        {
            TempData["Alert"]     = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = jobID > 0 ? jobID : (int?)null });
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                UPDATE tblJob SET IsActive = 0, ModifiedOn = GETDATE()
                WHERE JobID = @JobID;", conn);
            cmd.Parameters.AddWithValue("@JobID", deleteId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"]     = "Job deactivated successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"]     = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private static void AddParams(
        SqlCommand cmd, int jobID, string jobTitle, string jobCode, int gradeID,
        string jobLevel, string positionNumber,
        int reportsToEmployeeID, int functionalManagerEmployeeID,
        int dottedLineManagerEmployeeID, int backupApproverEmployeeID, bool isActive)
    {
        static object Fk(int id) => id <= 0 ? DBNull.Value : (object)id;

        if (jobID > 0)
            cmd.Parameters.AddWithValue("@JobID", jobID);

        cmd.Parameters.AddWithValue("@JobTitle",     jobTitle.Trim());
        cmd.Parameters.AddWithValue("@JobCode",      jobCode.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("@GradeID",       gradeID);
        cmd.Parameters.AddWithValue("@JobLevel",     jobLevel.Trim());
        cmd.Parameters.AddWithValue("@PositionNumber", positionNumber.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("@ReportsToEmployeeID",         reportsToEmployeeID);
        cmd.Parameters.AddWithValue("@FunctionalManagerEmployeeID", Fk(functionalManagerEmployeeID));
        cmd.Parameters.AddWithValue("@DottedLineManagerEmployeeID", Fk(dottedLineManagerEmployeeID));
        cmd.Parameters.AddWithValue("@BackupApproverEmployeeID",    backupApproverEmployeeID);
        cmd.Parameters.AddWithValue("@IsActive", isActive);
    }

    private void LoadGrades()
    {
        Grades.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT GradeID, GradeName FROM tblGrade
            WHERE IsActive = 1 ORDER BY GradeName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Grades.Add(new LookupItem
            {
                Id   = Convert.ToInt32(dr["GradeID"]),
                Name = dr["GradeName"].ToString() ?? ""
            });
        }
    }

    private void LoadEmployees()
    {
        Employees.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT EmployeeID, EmployeeCode,
                   FirstName + ' ' + LastName AS FullName
            FROM   tblEmployee
            WHERE  Status = 'Active'
            ORDER BY FirstName, LastName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var code = dr["EmployeeCode"].ToString() ?? "";
            var name = dr["FullName"].ToString() ?? "";
            Employees.Add(new LookupItem
            {
                Id   = Convert.ToInt32(dr["EmployeeID"]),
                Name = $"{code} – {name}"
            });
        }
    }

    private void LoadForEdit(int jobId)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT JobID, JobTitle, JobCode, GradeID, JobLevel, PositionNumber,
                   ReportsToEmployeeID, FunctionalManagerEmployeeID,
                   DottedLineManagerEmployeeID, BackupApproverEmployeeID, IsActive
            FROM tblJob WHERE JobID = @JobID;", conn);
        cmd.Parameters.AddWithValue("@JobID", jobId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            static int IntOrZero(object v) => v == DBNull.Value ? 0 : Convert.ToInt32(v);
            Input = new JobRecord
            {
                JobID                       = jobId,
                JobTitle                    = dr["JobTitle"].ToString()!,
                JobCode                     = dr["JobCode"].ToString()!,
                GradeID                     = IntOrZero(dr["GradeID"]),
                JobLevel                    = dr["JobLevel"].ToString()!,
                PositionNumber              = dr["PositionNumber"].ToString()!,
                ReportsToEmployeeID         = IntOrZero(dr["ReportsToEmployeeID"]),
                FunctionalManagerEmployeeID = IntOrZero(dr["FunctionalManagerEmployeeID"]),
                DottedLineManagerEmployeeID = IntOrZero(dr["DottedLineManagerEmployeeID"]),
                BackupApproverEmployeeID    = IntOrZero(dr["BackupApproverEmployeeID"]),
                IsActive                    = Convert.ToBoolean(dr["IsActive"])
            };
        }
    }

    private void LoadRecords()
    {
        Records.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT j.JobID, j.JobTitle, j.JobCode, j.GradeID, g.GradeName,
                   j.JobLevel, j.PositionNumber,
                   j.ReportsToEmployeeID,
                   rt.FirstName + ' ' + rt.LastName AS ReportsToName,
                   j.FunctionalManagerEmployeeID,
                   fm.FirstName + ' ' + fm.LastName AS FunctionalManagerName,
                   j.DottedLineManagerEmployeeID,
                   dm.FirstName + ' ' + dm.LastName AS DottedLineManagerName,
                   j.BackupApproverEmployeeID,
                   ba.FirstName + ' ' + ba.LastName AS BackupApproverName,
                   j.IsActive
            FROM tblJob j
            LEFT JOIN tblGrade g ON g.GradeID = j.GradeID
            LEFT JOIN tblEmployee rt ON rt.EmployeeID = j.ReportsToEmployeeID
            LEFT JOIN tblEmployee fm ON fm.EmployeeID = j.FunctionalManagerEmployeeID
            LEFT JOIN tblEmployee dm ON dm.EmployeeID = j.DottedLineManagerEmployeeID
            LEFT JOIN tblEmployee ba ON ba.EmployeeID = j.BackupApproverEmployeeID
            ORDER BY j.IsActive DESC, j.JobTitle;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            static int IntOrZero(object v) => v == DBNull.Value ? 0 : Convert.ToInt32(v);
            static string Str(object v) => v == DBNull.Value ? "" : v.ToString() ?? "";

            Records.Add(new JobRecord
            {
                JobID                       = Convert.ToInt32(dr["JobID"]),
                JobTitle                    = dr["JobTitle"].ToString()!,
                JobCode                     = dr["JobCode"].ToString()!,
                GradeID                     = IntOrZero(dr["GradeID"]),
                GradeName                   = Str(dr["GradeName"]),
                JobLevel                    = dr["JobLevel"].ToString()!,
                PositionNumber              = dr["PositionNumber"].ToString()!,
                ReportsToEmployeeID         = IntOrZero(dr["ReportsToEmployeeID"]),
                ReportsToName               = Str(dr["ReportsToName"]),
                FunctionalManagerEmployeeID = IntOrZero(dr["FunctionalManagerEmployeeID"]),
                FunctionalManagerName       = Str(dr["FunctionalManagerName"]),
                DottedLineManagerEmployeeID = IntOrZero(dr["DottedLineManagerEmployeeID"]),
                DottedLineManagerName       = Str(dr["DottedLineManagerName"]),
                BackupApproverEmployeeID    = IntOrZero(dr["BackupApproverEmployeeID"]),
                BackupApproverName          = Str(dr["BackupApproverName"]),
                IsActive                    = Convert.ToBoolean(dr["IsActive"])
            });
        }
    }

    private string GenerateNextJobCode()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT TOP 1 JobCode FROM tblJob
            WHERE JobCode LIKE 'JC-GEN-%'
            ORDER BY JobCode DESC;", conn);
        conn.Open();
        var last = cmd.ExecuteScalar()?.ToString();
        if (!string.IsNullOrEmpty(last) && last.Length >= 10)
        {
            var numPart = last[7..];
            if (int.TryParse(numPart, out int num))
                return $"JC-GEN-{(num + 1):D3}";
        }
        return "JC-GEN-001";
    }

    private string GenerateNextPositionNumber()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT TOP 1 PositionNumber FROM tblJob
            WHERE PositionNumber LIKE 'POS-%'
            ORDER BY PositionNumber DESC;", conn);
        conn.Open();
        var last = cmd.ExecuteScalar()?.ToString();
        if (!string.IsNullOrEmpty(last) && last.Length >= 10)
        {
            var numPart = last[4..];
            if (int.TryParse(numPart, out int num))
                return $"POS-{(num + 1):D6}";
        }
        return "POS-000001";
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType    = TempData["AlertType"]?.ToString() ?? "success";
    }
}
