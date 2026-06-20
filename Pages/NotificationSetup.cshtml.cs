using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class NotificationRecord
{
    public int NotificationID { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int DepartmentID { get; set; }
    public string DepartmentName { get; set; } = "";
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime ValidTillDate { get; set; } = DateTime.Today.AddMonths(1);
    public bool IsActive { get; set; } = true;
}

public class DeptLookupItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class NotificationSetupModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    public NotificationSetupModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    public string PageTitle => "Notification Setup";
    public NotificationRecord Input { get; set; } = new();
    public List<NotificationRecord> Records { get; set; } = new();
    public List<DeptLookupItem> Departments { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        LoadDepartments();
        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);
        LoadRecords();
    }

    public IActionResult OnPostSave(
        int notificationID, string notificationName, string description,
        int departmentID, DateTime startDate, DateTime validTillDate, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(notificationName))
        {
            TempData["Alert"] = "Notification name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        if (validTillDate.Date < startDate.Date)
        {
            TempData["Alert"] = "Valid till date cannot be before start date.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            var deptParam = departmentID > 0 ? (object)departmentID : DBNull.Value;

            if (notificationID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblNotification
                    SET NotificationName = @Name,
                        Description = @Description,
                        DepartmentID = @DepartmentID,
                        StartDate = @StartDate,
                        ValidTillDate = @ValidTillDate,
                        IsActive = @IsActive,
                        ModifiedOn = GETDATE(),
                        ModifiedByUserID = @ModifiedByUserID
                    WHERE NotificationID = @Id;", conn);
                cmd.Parameters.AddWithValue("@Id", notificationID);
                cmd.Parameters.AddWithValue("@Name", notificationName.Trim());
                cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? DBNull.Value : description.Trim());
                cmd.Parameters.AddWithValue("@DepartmentID", deptParam);
                cmd.Parameters.AddWithValue("@StartDate", startDate.Date);
                cmd.Parameters.AddWithValue("@ValidTillDate", validTillDate.Date);
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Notification updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblNotification
                        (NotificationName, Description, DepartmentID, StartDate, ValidTillDate, IsActive, CreatedOn, CreatedByUserID)
                    VALUES
                        (@Name, @Description, @DepartmentID, @StartDate, @ValidTillDate, @IsActive, GETDATE(), @CreatedByUserID);", conn);
                cmd.Parameters.AddWithValue("@Name", notificationName.Trim());
                cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? DBNull.Value : description.Trim());
                cmd.Parameters.AddWithValue("@DepartmentID", deptParam);
                cmd.Parameters.AddWithValue("@StartDate", startDate.Date);
                cmd.Parameters.AddWithValue("@ValidTillDate", validTillDate.Date);
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Notification added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                UPDATE tblNotification
                SET IsActive = 0,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE NotificationID = @Id;", conn);
            cmd.Parameters.AddWithValue("@Id", deleteId);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Notification deactivated successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error removing record: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private void LoadDepartments()
    {
        Departments.Clear();
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
            Departments.Add(new DeptLookupItem
            {
                Id = Convert.ToInt32(dr["DepartmentID"]),
                Name = dr["DepartmentName"].ToString() ?? ""
            });
        }
    }

    private void LoadForEdit(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT n.NotificationID, n.NotificationName, n.Description,
                   ISNULL(n.DepartmentID, 0) AS DepartmentID,
                   d.DepartmentName, n.StartDate, n.ValidTillDate, n.IsActive
            FROM tblNotification n
            LEFT JOIN tblDepartment d ON d.DepartmentID = n.DepartmentID
            WHERE n.NotificationID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read())
            Input = ReadRecord(dr);
    }

    private void LoadRecords()
    {
        Records.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT n.NotificationID, n.NotificationName, n.Description,
                   ISNULL(n.DepartmentID, 0) AS DepartmentID,
                   ISNULL(d.DepartmentName, 'All Departments') AS DepartmentName,
                   n.StartDate, n.ValidTillDate, n.IsActive
            FROM tblNotification n
            LEFT JOIN tblDepartment d ON d.DepartmentID = n.DepartmentID
            ORDER BY n.IsActive DESC, n.StartDate DESC, n.NotificationID DESC;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            Records.Add(ReadRecord(dr));
    }

    private static NotificationRecord ReadRecord(SqlDataReader dr) => new()
    {
        NotificationID = Convert.ToInt32(dr["NotificationID"]),
        Name = dr["NotificationName"].ToString() ?? "",
        Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
        DepartmentID = Convert.ToInt32(dr["DepartmentID"]),
        DepartmentName = dr["DepartmentName"].ToString() ?? "",
        StartDate = Convert.ToDateTime(dr["StartDate"]),
        ValidTillDate = Convert.ToDateTime(dr["ValidTillDate"]),
        IsActive = Convert.ToBoolean(dr["IsActive"])
    };

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType = TempData["AlertType"]?.ToString() ?? "success";
    }
}
