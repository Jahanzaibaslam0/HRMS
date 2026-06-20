using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class ProvinceRecord
{
    public int    ProvinceID   { get; set; }
    public string ProvinceCode { get; set; } = "";
    public string ProvinceName { get; set; } = "";
    public string AliasName    { get; set; } = "";
    public bool   IsActive     { get; set; } = true;
}

public class ProvinceSetupModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    public ProvinceSetupModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    public string PageTitle => "Province Setup";
    public ProvinceRecord Input { get; set; } = new() { IsActive = true };
    public List<ProvinceRecord> Records { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);
        else
            Input.ProvinceCode = GenerateNextCode();
        LoadRecords();
    }

    public IActionResult OnPostSave(int provinceID, string provinceCode, string provinceName, string aliasName, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(provinceCode))
        {
            TempData["Alert"] = "Province Code is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = provinceID > 0 ? provinceID : (int?)null });
        }
        if (string.IsNullOrWhiteSpace(provinceName))
        {
            TempData["Alert"] = "Province Name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = provinceID > 0 ? provinceID : (int?)null });
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (provinceID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblProvince SET ProvinceCode = @Code, ProvinceName = @Name, AliasName = @AliasName,
                        IsActive = @IsActive, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                    WHERE ProvinceID = @ID;", conn);
                AddParams(cmd, provinceID, provinceCode, provinceName, aliasName, isActive);
                AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Province updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblProvince (ProvinceCode, ProvinceName, AliasName, IsActive, CreatedOn, CreatedByUserID)
                    VALUES (@Code, @Name, @AliasName, @IsActive, GETDATE(), @CreatedByUserID);", conn);
                AddParams(cmd, 0, provinceCode, provinceName, aliasName, isActive);
                AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Province added successfully.";
            }
            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "A province with this code already exists.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = provinceID > 0 ? provinceID : (int?)null });
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = provinceID > 0 ? provinceID : (int?)null });
        }
        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"UPDATE tblProvince SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID WHERE ProvinceID = @ID;", conn);
            cmd.Parameters.AddWithValue("@ID", deleteId);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            conn.Open();
            cmd.ExecuteNonQuery();
            TempData["Alert"] = "Province removed successfully.";
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
        using var cmd = new SqlCommand(@"SELECT ProvinceID, ProvinceCode, ProvinceName, AliasName, IsActive FROM tblProvince WHERE ProvinceID = @ID;", conn);
        cmd.Parameters.AddWithValue("@ID", id);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read()) Input = ReadRecord(dr);
    }

    private void LoadRecords()
    {
        Records.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"SELECT ProvinceID, ProvinceCode, ProvinceName, AliasName, IsActive FROM tblProvince ORDER BY IsActive DESC, ProvinceCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read()) Records.Add(ReadRecord(dr));
    }

    private static ProvinceRecord ReadRecord(SqlDataReader dr) => new()
    {
        ProvinceID   = Convert.ToInt32(dr["ProvinceID"]),
        ProvinceCode = dr["ProvinceCode"].ToString() ?? "",
        ProvinceName = dr["ProvinceName"].ToString() ?? "",
        AliasName    = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? "",
        IsActive     = Convert.ToBoolean(dr["IsActive"])
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
        using var cmd = new SqlCommand(@"SELECT TOP 1 ProvinceCode FROM tblProvince WHERE ProvinceCode LIKE 'PRV-%' ORDER BY ProvinceCode DESC;", conn);
        conn.Open();
        var last = cmd.ExecuteScalar()?.ToString();
        if (!string.IsNullOrEmpty(last) && last.Length >= 7 && int.TryParse(last[4..], out int num))
            return $"PRV-{(num + 1):D3}";
        return "PRV-001";
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType    = TempData["AlertType"]?.ToString() ?? "success";
    }
}
