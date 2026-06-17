using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class SalesGroupRecord
{
    public int    SalesGroupID   { get; set; }
    public string SalesGroupCode { get; set; } = "";
    public string SalesGroupName { get; set; } = "";
    public string AliasName      { get; set; } = "";
    public bool   IsActive       { get; set; } = true;
}

public class SalesGroupSetupModel : PageModel
{
    private readonly string _conn;

    public SalesGroupSetupModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public string PageTitle => "Sales Group Setup";
    public SalesGroupRecord Input { get; set; } = new() { IsActive = true };
    public List<SalesGroupRecord> Records { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);
        else
            Input.SalesGroupCode = GenerateNextCode();
        LoadRecords();
    }

    public IActionResult OnPostSave(int salesGroupID, string salesGroupCode, string salesGroupName, string aliasName, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(salesGroupCode))
        {
            TempData["Alert"] = "Sales Group Code is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = salesGroupID > 0 ? salesGroupID : (int?)null });
        }
        if (string.IsNullOrWhiteSpace(salesGroupName))
        {
            TempData["Alert"] = "Sales Group Name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = salesGroupID > 0 ? salesGroupID : (int?)null });
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (salesGroupID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblSalesGroup SET SalesGroupCode = @Code, SalesGroupName = @Name, AliasName = @AliasName,
                        IsActive = @IsActive, ModifiedOn = GETDATE()
                    WHERE SalesGroupID = @ID;", conn);
                AddParams(cmd, salesGroupID, salesGroupCode, salesGroupName, aliasName, isActive);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Sales Group updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblSalesGroup (SalesGroupCode, SalesGroupName, AliasName, IsActive)
                    VALUES (@Code, @Name, @AliasName, @IsActive);", conn);
                AddParams(cmd, 0, salesGroupCode, salesGroupName, aliasName, isActive);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Sales Group added successfully.";
            }
            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "A sales group with this code already exists.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = salesGroupID > 0 ? salesGroupID : (int?)null });
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = salesGroupID > 0 ? salesGroupID : (int?)null });
        }
        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"UPDATE tblSalesGroup SET IsActive = 0, ModifiedOn = GETDATE() WHERE SalesGroupID = @ID;", conn);
            cmd.Parameters.AddWithValue("@ID", deleteId);
            conn.Open();
            cmd.ExecuteNonQuery();
            TempData["Alert"] = "Sales Group removed successfully.";
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
        using var cmd = new SqlCommand(@"SELECT SalesGroupID, SalesGroupCode, SalesGroupName, AliasName, IsActive FROM tblSalesGroup WHERE SalesGroupID = @ID;", conn);
        cmd.Parameters.AddWithValue("@ID", id);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read()) Input = ReadRecord(dr);
    }

    private void LoadRecords()
    {
        Records.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"SELECT SalesGroupID, SalesGroupCode, SalesGroupName, AliasName, IsActive FROM tblSalesGroup ORDER BY IsActive DESC, SalesGroupCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read()) Records.Add(ReadRecord(dr));
    }

    private static SalesGroupRecord ReadRecord(SqlDataReader dr) => new()
    {
        SalesGroupID   = Convert.ToInt32(dr["SalesGroupID"]),
        SalesGroupCode = dr["SalesGroupCode"].ToString() ?? "",
        SalesGroupName = dr["SalesGroupName"].ToString() ?? "",
        AliasName      = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? "",
        IsActive       = Convert.ToBoolean(dr["IsActive"])
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
        using var cmd = new SqlCommand(@"SELECT TOP 1 SalesGroupCode FROM tblSalesGroup WHERE SalesGroupCode LIKE 'SGP-%' ORDER BY SalesGroupCode DESC;", conn);
        conn.Open();
        var last = cmd.ExecuteScalar()?.ToString();
        if (!string.IsNullOrEmpty(last) && last.Length >= 7 && int.TryParse(last[4..], out int num))
            return $"SGP-{(num + 1):D3}";
        return "SGP-001";
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType    = TempData["AlertType"]?.ToString() ?? "success";
    }
}
