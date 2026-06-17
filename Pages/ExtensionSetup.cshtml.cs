using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class ExtensionRecord
{
    public int    ExtensionID     { get; set; }
    public string ExtensionCode   { get; set; } = "";
    public string ExtensionName   { get; set; } = "";
    public string AliasName       { get; set; } = "";
    public int    DepartmentID    { get; set; }
    public string DepartmentName  { get; set; } = "";
    public int    LocationID      { get; set; }
    public string LocationName    { get; set; } = "";
    public bool   IsActive        { get; set; } = true;
}

public class ExtensionSetupModel : PageModel
{
    private readonly string _conn;

    public ExtensionSetupModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public string PageTitle => "Extension Master Setup";
    public ExtensionRecord Input { get; set; } = new() { IsActive = true };
    public List<ExtensionRecord> Records { get; set; } = new();
    public List<LookupItem> Departments { get; set; } = new();
    public List<LookupItem> Locations { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        LoadDepartments();
        LoadLocations();

        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);
        else
            Input.ExtensionCode = GenerateNextCode();

        LoadRecords();
    }

    public IActionResult OnPostSave(
        int extensionID, string extensionCode, string extensionName, string aliasName,
        int departmentID, int locationID, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(extensionCode))
        {
            TempData["Alert"] = "Extension Code is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = extensionID > 0 ? extensionID : (int?)null });
        }

        if (string.IsNullOrWhiteSpace(extensionName))
        {
            TempData["Alert"] = "Extension Name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = extensionID > 0 ? extensionID : (int?)null });
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (extensionID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblExtension SET
                        ExtensionCode = @Code,
                        ExtensionName = @Name,
                        AliasName       = @AliasName,
                        DepartmentID    = @DepartmentID,
                        LocationID      = @LocationID,
                        IsActive        = @IsActive,
                        ModifiedOn      = GETDATE()
                    WHERE ExtensionID = @ID;", conn);
                AddParams(cmd, extensionID, extensionCode, extensionName, aliasName,
                    departmentID, locationID, isActive);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Extension updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblExtension
                        (ExtensionCode, ExtensionName, AliasName, DepartmentID, LocationID, IsActive)
                    VALUES
                        (@Code, @Name, @AliasName, @DepartmentID, @LocationID, @IsActive);", conn);
                AddParams(cmd, 0, extensionCode, extensionName, aliasName,
                    departmentID, locationID, isActive);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Extension added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "An extension with this code already exists.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = extensionID > 0 ? extensionID : (int?)null });
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = extensionID > 0 ? extensionID : (int?)null });
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                UPDATE tblExtension SET IsActive = 0, ModifiedOn = GETDATE()
                WHERE ExtensionID = @ID;", conn);
            cmd.Parameters.AddWithValue("@ID", deleteId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Extension removed successfully.";
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
            Departments.Add(new LookupItem
            {
                Id   = Convert.ToInt32(dr["DepartmentID"]),
                Name = dr["DepartmentName"].ToString() ?? ""
            });
        }
    }

    private void LoadLocations()
    {
        Locations.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT LocationID, LocationName
            FROM tblLocation
            WHERE IsActive = 1
            ORDER BY LocationName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Locations.Add(new LookupItem
            {
                Id   = Convert.ToInt32(dr["LocationID"]),
                Name = dr["LocationName"].ToString() ?? ""
            });
        }
    }

    private void LoadForEdit(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT ExtensionID, ExtensionCode, ExtensionName, AliasName,
                   DepartmentID, LocationID, IsActive
            FROM tblExtension WHERE ExtensionID = @ID;", conn);
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
            SELECT e.ExtensionID, e.ExtensionCode, e.ExtensionName, e.AliasName,
                   e.DepartmentID, d.DepartmentName,
                   e.LocationID, l.LocationName,
                   e.IsActive
            FROM tblExtension e
            LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
            LEFT JOIN tblLocation l ON l.LocationID = e.LocationID
            ORDER BY e.IsActive DESC, e.ExtensionCode;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            static string Str(object v) => v == DBNull.Value ? "" : v.ToString() ?? "";

            var rec = ReadRecord(dr);
            rec.DepartmentName = Str(dr["DepartmentName"]);
            rec.LocationName   = Str(dr["LocationName"]);
            Records.Add(rec);
        }
    }

    private static ExtensionRecord ReadRecord(SqlDataReader dr)
    {
        static int IntOrZero(object v) => v == DBNull.Value ? 0 : Convert.ToInt32(v);
        return new ExtensionRecord
        {
            ExtensionID   = Convert.ToInt32(dr["ExtensionID"]),
            ExtensionCode = dr["ExtensionCode"].ToString() ?? "",
            ExtensionName = dr["ExtensionName"].ToString() ?? "",
            AliasName     = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? "",
            DepartmentID  = IntOrZero(dr["DepartmentID"]),
            LocationID    = IntOrZero(dr["LocationID"]),
            IsActive      = Convert.ToBoolean(dr["IsActive"])
        };
    }

    private static void AddParams(
        SqlCommand cmd, int id, string code, string name, string alias,
        int departmentID, int locationID, bool isActive)
    {
        static object Fk(int v) => v <= 0 ? DBNull.Value : (object)v;

        if (id > 0) cmd.Parameters.AddWithValue("@ID", id);
        cmd.Parameters.AddWithValue("@Code", code.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("@Name", name.Trim());
        cmd.Parameters.AddWithValue("@AliasName", string.IsNullOrWhiteSpace(alias) ? DBNull.Value : alias.Trim());
        cmd.Parameters.AddWithValue("@DepartmentID", Fk(departmentID));
        cmd.Parameters.AddWithValue("@LocationID", Fk(locationID));
        cmd.Parameters.AddWithValue("@IsActive", isActive);
    }

    private string GenerateNextCode()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT TOP 1 ExtensionCode FROM tblExtension
            WHERE ExtensionCode LIKE 'EXT-%'
            ORDER BY ExtensionCode DESC;", conn);
        conn.Open();
        var last = cmd.ExecuteScalar()?.ToString();
        if (!string.IsNullOrEmpty(last) && last.Length >= 7 && int.TryParse(last[4..], out int num))
            return $"EXT-{(num + 1):D3}";
        return "EXT-001";
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType    = TempData["AlertType"]?.ToString() ?? "success";
    }
}
