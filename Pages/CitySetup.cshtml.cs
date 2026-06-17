using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class CityRecord
{
    public int    CityID   { get; set; }
    public string CityCode { get; set; } = "";
    public string CityName { get; set; } = "";
    public string AliasName { get; set; } = "";
    public bool   IsActive { get; set; } = true;
}

public class CitySetupModel : PageModel
{
    private readonly string _conn;

    public CitySetupModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public string PageTitle => "City Setup";
    public CityRecord Input { get; set; } = new() { IsActive = true };
    public List<CityRecord> Records { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);
        else
            Input.CityCode = GenerateNextCode();
        LoadRecords();
    }

    public IActionResult OnPostSave(int cityID, string cityCode, string cityName, string aliasName, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(cityCode))
        {
            TempData["Alert"] = "City Code is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = cityID > 0 ? cityID : (int?)null });
        }
        if (string.IsNullOrWhiteSpace(cityName))
        {
            TempData["Alert"] = "City Name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = cityID > 0 ? cityID : (int?)null });
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (cityID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblCity SET CityCode = @Code, CityName = @Name, AliasName = @AliasName,
                        IsActive = @IsActive, ModifiedOn = GETDATE()
                    WHERE CityID = @ID;", conn);
                AddParams(cmd, cityID, cityCode, cityName, aliasName, isActive);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "City updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblCity (CityCode, CityName, AliasName, IsActive)
                    VALUES (@Code, @Name, @AliasName, @IsActive);", conn);
                AddParams(cmd, 0, cityCode, cityName, aliasName, isActive);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "City added successfully.";
            }
            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "A city with this code already exists.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = cityID > 0 ? cityID : (int?)null });
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = cityID > 0 ? cityID : (int?)null });
        }
        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"UPDATE tblCity SET IsActive = 0, ModifiedOn = GETDATE() WHERE CityID = @ID;", conn);
            cmd.Parameters.AddWithValue("@ID", deleteId);
            conn.Open();
            cmd.ExecuteNonQuery();
            TempData["Alert"] = "City removed successfully.";
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
        using var cmd = new SqlCommand(@"SELECT CityID, CityCode, CityName, AliasName, IsActive FROM tblCity WHERE CityID = @ID;", conn);
        cmd.Parameters.AddWithValue("@ID", id);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read()) Input = ReadRecord(dr);
    }

    private void LoadRecords()
    {
        Records.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"SELECT CityID, CityCode, CityName, AliasName, IsActive FROM tblCity ORDER BY IsActive DESC, CityCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read()) Records.Add(ReadRecord(dr));
    }

    private static CityRecord ReadRecord(SqlDataReader dr) => new()
    {
        CityID   = Convert.ToInt32(dr["CityID"]),
        CityCode = dr["CityCode"].ToString() ?? "",
        CityName = dr["CityName"].ToString() ?? "",
        AliasName = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? "",
        IsActive = Convert.ToBoolean(dr["IsActive"])
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
        using var cmd = new SqlCommand(@"SELECT TOP 1 CityCode FROM tblCity WHERE CityCode LIKE 'CTY-%' ORDER BY CityCode DESC;", conn);
        conn.Open();
        var last = cmd.ExecuteScalar()?.ToString();
        if (!string.IsNullOrEmpty(last) && last.Length >= 7 && int.TryParse(last[4..], out int num))
            return $"CTY-{(num + 1):D3}";
        return "CTY-001";
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType    = TempData["AlertType"]?.ToString() ?? "success";
    }
}
