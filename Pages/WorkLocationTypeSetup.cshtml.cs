using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class WorkLocationTypeRecord
{
    public int    WorkLocationTypeID   { get; set; }
    public string WorkLocationTypeCode { get; set; } = "";
    public string WorkLocationTypeName { get; set; } = "";
    public string AliasName            { get; set; } = "";
    public bool   IsActive             { get; set; } = true;
}

public class WorkLocationTypeSetupModel : PageModel
{
    private readonly string _conn;

    public WorkLocationTypeSetupModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public string PageTitle => "Work Location Type Setup";
    public WorkLocationTypeRecord Input { get; set; } = new() { IsActive = true };
    public List<WorkLocationTypeRecord> Records { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);
        else
            Input.WorkLocationTypeCode = GenerateNextCode();
        LoadRecords();
    }

    public IActionResult OnPostSave(
        int workLocationTypeID, string workLocationTypeCode,
        string workLocationTypeName, string aliasName, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(workLocationTypeCode))
        {
            TempData["Alert"] = "Work Location Type Code is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = workLocationTypeID > 0 ? workLocationTypeID : (int?)null });
        }

        if (string.IsNullOrWhiteSpace(workLocationTypeName))
        {
            TempData["Alert"] = "Work Location Type Name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = workLocationTypeID > 0 ? workLocationTypeID : (int?)null });
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (workLocationTypeID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblWorkLocationType SET
                        WorkLocationTypeCode = @Code,
                        WorkLocationTypeName = @Name,
                        AliasName            = @AliasName,
                        IsActive             = @IsActive,
                        ModifiedOn           = GETDATE()
                    WHERE WorkLocationTypeID = @ID;", conn);
                AddParams(cmd, workLocationTypeID, workLocationTypeCode, workLocationTypeName, aliasName, isActive);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Work Location Type updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblWorkLocationType
                        (WorkLocationTypeCode, WorkLocationTypeName, AliasName, IsActive)
                    VALUES
                        (@Code, @Name, @AliasName, @IsActive);", conn);
                AddParams(cmd, 0, workLocationTypeCode, workLocationTypeName, aliasName, isActive);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Work Location Type added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "A Work Location Type with this code already exists.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = workLocationTypeID > 0 ? workLocationTypeID : (int?)null });
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = workLocationTypeID > 0 ? workLocationTypeID : (int?)null });
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                UPDATE tblWorkLocationType SET IsActive = 0, ModifiedOn = GETDATE()
                WHERE WorkLocationTypeID = @ID;", conn);
            cmd.Parameters.AddWithValue("@ID", deleteId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Work Location Type removed successfully.";
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
            SELECT WorkLocationTypeID, WorkLocationTypeCode, WorkLocationTypeName, AliasName, IsActive
            FROM tblWorkLocationType WHERE WorkLocationTypeID = @ID;", conn);
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
            SELECT WorkLocationTypeID, WorkLocationTypeCode, WorkLocationTypeName, AliasName, IsActive
            FROM tblWorkLocationType
            ORDER BY IsActive DESC, WorkLocationTypeCode;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read()) Records.Add(ReadRecord(dr));
    }

    private static WorkLocationTypeRecord ReadRecord(SqlDataReader dr) => new()
    {
        WorkLocationTypeID   = Convert.ToInt32(dr["WorkLocationTypeID"]),
        WorkLocationTypeCode = dr["WorkLocationTypeCode"].ToString() ?? "",
        WorkLocationTypeName = dr["WorkLocationTypeName"].ToString() ?? "",
        AliasName            = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? "",
        IsActive             = Convert.ToBoolean(dr["IsActive"])
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
            SELECT TOP 1 WorkLocationTypeCode FROM tblWorkLocationType
            WHERE WorkLocationTypeCode LIKE 'WLT-%'
            ORDER BY WorkLocationTypeCode DESC;", conn);
        conn.Open();
        var last = cmd.ExecuteScalar()?.ToString();
        if (!string.IsNullOrEmpty(last) && last.Length >= 7 && int.TryParse(last[4..], out int num))
            return $"WLT-{(num + 1):D3}";
        return "WLT-001";
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType    = TempData["AlertType"]?.ToString() ?? "success";
    }
}
