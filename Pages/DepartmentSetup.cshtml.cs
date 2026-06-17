using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class DepartmentRecord
{
    public int    Id                  { get; set; }
    public int    DivisionID          { get; set; }
    public string DivisionName        { get; set; } = "";
    public int    WingID              { get; set; }
    public string WingName            { get; set; } = "";
    public int    BusinessSegmentID   { get; set; }
    public string BusinessSegmentName { get; set; } = "";
    public int    BusinessUnitID      { get; set; }
    public string BusinessUnitName    { get; set; } = "";
    public string Name                { get; set; } = "";
    public string AliasName           { get; set; } = "";
    public bool   IsActive            { get; set; } = true;
}

public class DepartmentSetupModel : PageModel
{
    private readonly string _conn;

    public DepartmentSetupModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public string PageTitle => "Department Setup";
    public DepartmentRecord Input { get; set; } = new();
    public List<DepartmentRecord> Records { get; set; } = new();
    public List<LookupItem> Divisions        { get; set; } = new();
    public List<LookupItem> Wings            { get; set; } = new();
    public List<LookupItem> BusinessSegments { get; set; } = new();
    public List<LookupItem> BusinessUnits    { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        LoadLookupLists();

        if (editId.HasValue && editId > 0)
        {
            LoadForEdit(editId.Value);
        }

        LoadRecords();
    }

    public IActionResult OnPostSave(
        int itemId, int divisionID, int wingID, int businessSegmentID, int businessUnitID,
        string itemName, string aliasName, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            TempData["Alert"] = "Department is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (itemId > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblDepartment
                    SET DivisionID = @DivisionID,
                        WingID = @WingID,
                        BusinessSegmentID = @BusinessSegmentID,
                        BusinessUnitID = @BusinessUnitID,
                        DepartmentName = @DepartmentName,
                        AliasName = @AliasName,
                        IsActive = @IsActive,
                        ModifiedOn = GETDATE()
                    WHERE DepartmentID = @DepartmentID;", conn);
                AddSaveParameters(cmd, itemId, divisionID, wingID, businessSegmentID, businessUnitID, itemName, aliasName, isActive);
                cmd.ExecuteNonQuery();

                TempData["Alert"] = "Department updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblDepartment
                        (DivisionID, WingID, BusinessSegmentID, BusinessUnitID, DepartmentName, AliasName, IsActive)
                    VALUES
                        (@DivisionID, @WingID, @BusinessSegmentID, @BusinessUnitID, @DepartmentName, @AliasName, @IsActive);", conn);
                AddSaveParameters(cmd, itemId, divisionID, wingID, businessSegmentID, businessUnitID, itemName, aliasName, isActive);
                cmd.ExecuteNonQuery();

                TempData["Alert"] = "Department added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "Department already exists.";
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
                UPDATE tblDepartment
                SET IsActive = 0,
                    ModifiedOn = GETDATE()
                WHERE DepartmentID = @DepartmentID;", conn);
            cmd.Parameters.AddWithValue("@DepartmentID", deleteId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Department removed successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error removing record: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private static void AddSaveParameters(
        SqlCommand cmd, int departmentID, int divisionID, int wingID,
        int businessSegmentID, int businessUnitID,
        string departmentName, string aliasName, bool isActive)
    {
        static object Fk(int id) => id <= 0 ? DBNull.Value : (object)id;

        cmd.Parameters.AddWithValue("@DepartmentID",      departmentID);
        cmd.Parameters.AddWithValue("@DivisionID",        Fk(divisionID));
        cmd.Parameters.AddWithValue("@WingID",            Fk(wingID));
        cmd.Parameters.AddWithValue("@BusinessSegmentID", Fk(businessSegmentID));
        cmd.Parameters.AddWithValue("@BusinessUnitID",    Fk(businessUnitID));
        cmd.Parameters.AddWithValue("@DepartmentName",    departmentName.Trim());
        cmd.Parameters.AddWithValue("@AliasName",       string.IsNullOrWhiteSpace(aliasName) ? DBNull.Value : aliasName.Trim());
        cmd.Parameters.AddWithValue("@IsActive",          isActive);
    }

    private void LoadLookupLists()
    {
        Wings            = LoadLookup("tblWing",             "WingID",             "WingName");
        BusinessSegments = LoadLookup("tblBusinessSegment",  "BusinessSegmentID",  "BusinessSegmentName");
        BusinessUnits    = LoadLookup("tblBusinessUnit",     "BusinessUnitID",     "BusinessUnitName");
        Divisions        = LoadLookup("tblDivision",         "DivisionID",         "DivisionName");
    }

    private List<LookupItem> LoadLookup(string tableName, string idColumn, string nameColumn)
    {
        var items = new List<LookupItem>();

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT {idColumn}, {nameColumn}
            FROM {tableName}
            WHERE IsActive = 1
            ORDER BY {nameColumn};", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            items.Add(new LookupItem
            {
                Id   = Convert.ToInt32(dr[idColumn]),
                Name = dr[nameColumn].ToString() ?? ""
            });
        }

        return items;
    }

    private void LoadForEdit(int departmentID)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT d.DepartmentID, d.DivisionID, v.DivisionName,
                   d.WingID, w.WingName,
                   d.BusinessSegmentID, bs.BusinessSegmentName,
                   d.BusinessUnitID, bu.BusinessUnitName,
                   d.DepartmentName, d.AliasName, d.IsActive
            FROM tblDepartment d
            LEFT JOIN tblDivision v         ON v.DivisionID = d.DivisionID
            LEFT JOIN tblWing w             ON w.WingID = d.WingID
            LEFT JOIN tblBusinessSegment bs ON bs.BusinessSegmentID = d.BusinessSegmentID
            LEFT JOIN tblBusinessUnit bu    ON bu.BusinessUnitID = d.BusinessUnitID
            WHERE d.DepartmentID = @DepartmentID;", conn);
        cmd.Parameters.AddWithValue("@DepartmentID", departmentID);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            Input = ReadDepartment(dr);
        }
    }

    private void LoadRecords()
    {
        Records.Clear();

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT d.DepartmentID, d.DivisionID, v.DivisionName,
                   d.WingID, w.WingName,
                   d.BusinessSegmentID, bs.BusinessSegmentName,
                   d.BusinessUnitID, bu.BusinessUnitName,
                   d.DepartmentName, d.AliasName, d.IsActive
            FROM tblDepartment d
            LEFT JOIN tblDivision v         ON v.DivisionID = d.DivisionID
            LEFT JOIN tblWing w             ON w.WingID = d.WingID
            LEFT JOIN tblBusinessSegment bs ON bs.BusinessSegmentID = d.BusinessSegmentID
            LEFT JOIN tblBusinessUnit bu    ON bu.BusinessUnitID = d.BusinessUnitID
            ORDER BY d.IsActive DESC, v.DivisionName, d.DepartmentName;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Records.Add(ReadDepartment(dr));
        }
    }

    private static DepartmentRecord ReadDepartment(SqlDataReader dr)
    {
        static int IntOrZero(object v) => v == DBNull.Value ? 0 : Convert.ToInt32(v);
        static string StrOrEmpty(object v) => v == DBNull.Value ? "" : v.ToString() ?? "";

        return new DepartmentRecord
        {
            Id                  = Convert.ToInt32(dr["DepartmentID"]),
            DivisionID          = IntOrZero(dr["DivisionID"]),
            DivisionName        = StrOrEmpty(dr["DivisionName"]),
            WingID              = IntOrZero(dr["WingID"]),
            WingName            = StrOrEmpty(dr["WingName"]),
            BusinessSegmentID   = IntOrZero(dr["BusinessSegmentID"]),
            BusinessSegmentName = StrOrEmpty(dr["BusinessSegmentName"]),
            BusinessUnitID      = IntOrZero(dr["BusinessUnitID"]),
            BusinessUnitName    = StrOrEmpty(dr["BusinessUnitName"]),
            Name                = dr["DepartmentName"].ToString() ?? "",
            AliasName           = dr["AliasName"].ToString() ?? "",
            IsActive            = Convert.ToBoolean(dr["IsActive"])
        };
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;

        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType = TempData["AlertType"]?.ToString() ?? "success";
    }
}
