using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class WorkerCategoryRecord
{
    public int WorkerCategoryID { get; set; }
    public string WorkerCategoryCode { get; set; } = "";
    public string WorkerCategoryName { get; set; } = "";
    public string AliasName { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class WorkerCategorySetupModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    public WorkerCategorySetupModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    public string PageTitle => "Worker Category Setup";
    public WorkerCategoryRecord Input { get; set; } = new();
    public List<WorkerCategoryRecord> Records { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);
        LoadRecords();
    }

    public IActionResult OnPostSave(
        int workerCategoryID,
        string workerCategoryCode,
        string workerCategoryName,
        string aliasName,
        string description,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(workerCategoryCode))
        {
            TempData["Alert"] = "Worker Category Code is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(workerCategoryName))
        {
            TempData["Alert"] = "Worker Category Name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (workerCategoryID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblWorkerCategory
                    SET WorkerCategoryCode = @Code,
                        WorkerCategoryName = @Name,
                        AliasName          = @AliasName,
                        Description        = @Description,
                        IsActive           = @IsActive,
                        ModifiedOn         = GETDATE(),
                        ModifiedByUserID   = @ModifiedByUserID
                    WHERE WorkerCategoryID = @ID;", conn);
                AddParams(cmd, workerCategoryID, workerCategoryCode, workerCategoryName,
                    aliasName, description, isActive);
                AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Worker Category updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblWorkerCategory
                        (WorkerCategoryCode, WorkerCategoryName, AliasName, Description, IsActive, CreatedOn, CreatedByUserID)
                    VALUES
                        (@Code, @Name, @AliasName, @Description, @IsActive, GETDATE(), @CreatedByUserID);", conn);
                AddParams(cmd, 0, workerCategoryCode, workerCategoryName,
                    aliasName, description, isActive);
                AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Worker Category added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "A Worker Category with this code already exists.";
            TempData["AlertType"] = "error";
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
                UPDATE tblWorkerCategory
                SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                WHERE WorkerCategoryID = @ID;", conn);
            cmd.Parameters.AddWithValue("@ID", deleteId);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Worker Category removed successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error removing record: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void LoadForEdit(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT WorkerCategoryID, WorkerCategoryCode, WorkerCategoryName,
                   AliasName, Description, IsActive
            FROM tblWorkerCategory
            WHERE WorkerCategoryID = @ID;", conn);
        cmd.Parameters.AddWithValue("@ID", id);
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
            SELECT WorkerCategoryID, WorkerCategoryCode, WorkerCategoryName,
                   AliasName, Description, IsActive
            FROM tblWorkerCategory
            ORDER BY IsActive DESC, WorkerCategoryCode;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            Records.Add(ReadRecord(dr));
    }

    private static WorkerCategoryRecord ReadRecord(SqlDataReader dr) => new()
    {
        WorkerCategoryID   = Convert.ToInt32(dr["WorkerCategoryID"]),
        WorkerCategoryCode = dr["WorkerCategoryCode"].ToString() ?? "",
        WorkerCategoryName = dr["WorkerCategoryName"].ToString() ?? "",
        AliasName          = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? "",
        Description        = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
        IsActive           = Convert.ToBoolean(dr["IsActive"])
    };

    private static void AddParams(SqlCommand cmd, int id, string code, string name,
        string alias, string description, bool isActive)
    {
        cmd.Parameters.AddWithValue("@ID",          id);
        cmd.Parameters.AddWithValue("@Code",        code.Trim());
        cmd.Parameters.AddWithValue("@Name",        name.Trim());
        cmd.Parameters.AddWithValue("@AliasName",   string.IsNullOrWhiteSpace(alias)       ? DBNull.Value : alias.Trim());
        cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? DBNull.Value : description.Trim());
        cmd.Parameters.AddWithValue("@IsActive",    isActive);
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType    = TempData["AlertType"]?.ToString() ?? "success";
    }
}
