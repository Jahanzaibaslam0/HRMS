using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class WorkArrangementRecord
{
    public int    WorkArrangementID   { get; set; }
    public string WorkArrangementCode { get; set; } = "";
    public string WorkArrangementName { get; set; } = "";
    public string AliasName           { get; set; } = "";
    public bool   IsActive            { get; set; } = true;
}

public class WorkArrangementSetupModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    public WorkArrangementSetupModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    public string PageTitle => "Work Arrangement Setup";
    public WorkArrangementRecord Input { get; set; } = new() { IsActive = true };
    public List<WorkArrangementRecord> Records { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);
        else
            Input.WorkArrangementCode = GenerateNextCode();
        LoadRecords();
    }

    public IActionResult OnPostSave(
        int workArrangementID, string workArrangementCode,
        string workArrangementName, string aliasName, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(workArrangementCode))
        {
            TempData["Alert"] = "Work Arrangement Code is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = workArrangementID > 0 ? workArrangementID : (int?)null });
        }

        if (string.IsNullOrWhiteSpace(workArrangementName))
        {
            TempData["Alert"] = "Work Arrangement Name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = workArrangementID > 0 ? workArrangementID : (int?)null });
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (workArrangementID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblWorkArrangement SET
                        WorkArrangementCode = @Code,
                        WorkArrangementName = @Name,
                        AliasName           = @AliasName,
                        IsActive            = @IsActive,
                        ModifiedOn          = GETDATE(),
                        ModifiedByUserID    = @ModifiedByUserID
                    WHERE WorkArrangementID = @ID;", conn);
                AddParams(cmd, workArrangementID, workArrangementCode, workArrangementName, aliasName, isActive);
                AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Work Arrangement updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblWorkArrangement
                        (WorkArrangementCode, WorkArrangementName, AliasName, IsActive, CreatedOn, CreatedByUserID)
                    VALUES
                        (@Code, @Name, @AliasName, @IsActive, GETDATE(), @CreatedByUserID);", conn);
                AddParams(cmd, 0, workArrangementCode, workArrangementName, aliasName, isActive);
                AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Work Arrangement added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "A Work Arrangement with this code already exists.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = workArrangementID > 0 ? workArrangementID : (int?)null });
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = workArrangementID > 0 ? workArrangementID : (int?)null });
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                UPDATE tblWorkArrangement SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                WHERE WorkArrangementID = @ID;", conn);
            cmd.Parameters.AddWithValue("@ID", deleteId);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Work Arrangement removed successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error removing record: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private void LoadForEdit(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT WorkArrangementID, WorkArrangementCode, WorkArrangementName, AliasName, IsActive
            FROM tblWorkArrangement WHERE WorkArrangementID = @ID;", conn);
        cmd.Parameters.AddWithValue("@ID", id);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        if (dr.Read()) Input = ReadRecord(dr);
    }

    private void LoadRecords()
    {
        Records.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT WorkArrangementID, WorkArrangementCode, WorkArrangementName, AliasName, IsActive
            FROM tblWorkArrangement
            ORDER BY IsActive DESC, WorkArrangementCode;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read()) Records.Add(ReadRecord(dr));
    }

    private static WorkArrangementRecord ReadRecord(SqlDataReader dr) => new()
    {
        WorkArrangementID   = Convert.ToInt32(dr["WorkArrangementID"]),
        WorkArrangementCode = dr["WorkArrangementCode"].ToString() ?? "",
        WorkArrangementName = dr["WorkArrangementName"].ToString() ?? "",
        AliasName           = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? "",
        IsActive            = Convert.ToBoolean(dr["IsActive"])
    };

    private static void AddParams(SqlCommand cmd, int id, string code, string name, string alias, bool isActive)
    {
        if (id > 0) cmd.Parameters.AddWithValue("@ID", id);
        cmd.Parameters.AddWithValue("@Code", code.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("@Name", name.Trim());
        cmd.Parameters.AddWithValue("@AliasName", string.IsNullOrWhiteSpace(alias) ? DBNull.Value : alias.Trim());
        cmd.Parameters.AddWithValue("@IsActive", isActive);
    }

    private string GenerateNextCode()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT TOP 1 WorkArrangementCode FROM tblWorkArrangement
            WHERE WorkArrangementCode LIKE 'WA-%'
            ORDER BY WorkArrangementCode DESC;", conn);
        conn.Open();
        var last = cmd.ExecuteScalar()?.ToString();
        if (!string.IsNullOrEmpty(last) && last.Length >= 6 && int.TryParse(last[3..], out int num))
            return $"WA-{(num + 1):D3}";
        return "WA-001";
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType    = TempData["AlertType"]?.ToString() ?? "success";
    }
}
