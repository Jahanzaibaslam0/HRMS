using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class SalesTeamRecord
{
    public int SalesTeamID { get; set; }
    public string SalesTeamCode { get; set; } = "";
    public string SalesTeamName { get; set; } = "";
    public int DivisionID { get; set; }
    public string DivisionName { get; set; } = "";
    public string AliasName { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class SalesTeamSetupModel : PageModel
{
    private readonly string _conn;

    public SalesTeamSetupModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public string PageTitle => "Sales Team Setup";
    public SalesTeamRecord Input { get; set; } = new();
    public List<SalesTeamRecord> Records { get; set; } = new();
    public List<LookupItem> Divisions { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        LoadDivisions();
        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);
        LoadRecords();
    }

    public IActionResult OnPostSave(
        int salesTeamID,
        string salesTeamCode,
        string salesTeamName,
        int divisionID,
        string aliasName,
        string description,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(salesTeamCode))
        {
            TempData["Alert"] = "Sales Team Code is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(salesTeamName))
        {
            TempData["Alert"] = "Sales Team Name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (salesTeamID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblSalesTeam
                    SET SalesTeamCode = @Code,
                        SalesTeamName = @Name,
                        DivisionID    = @DivisionID,
                        AliasName     = @AliasName,
                        Description   = @Description,
                        IsActive      = @IsActive,
                        ModifiedOn    = GETDATE()
                    WHERE SalesTeamID = @ID;", conn);
                AddParams(cmd, salesTeamID, salesTeamCode, salesTeamName, divisionID, aliasName, description, isActive);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Sales Team updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblSalesTeam
                        (SalesTeamCode, SalesTeamName, DivisionID, AliasName, Description, IsActive)
                    VALUES
                        (@Code, @Name, @DivisionID, @AliasName, @Description, @IsActive);", conn);
                AddParams(cmd, 0, salesTeamCode, salesTeamName, divisionID, aliasName, description, isActive);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Sales Team added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "A Sales Team with this code already exists.";
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
                UPDATE tblSalesTeam
                SET IsActive = 0, ModifiedOn = GETDATE()
                WHERE SalesTeamID = @ID;", conn);
            cmd.Parameters.AddWithValue("@ID", deleteId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Sales Team removed successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error removing record: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private void LoadDivisions()
    {
        Divisions.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT DivisionID, DivisionName
            FROM tblDivision
            WHERE IsActive = 1
            ORDER BY DivisionName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            Divisions.Add(new LookupItem
            {
                Id   = Convert.ToInt32(dr["DivisionID"]),
                Name = dr["DivisionName"].ToString() ?? ""
            });
    }

    private void LoadForEdit(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT s.SalesTeamID, s.SalesTeamCode, s.SalesTeamName,
                   ISNULL(s.DivisionID, 0) AS DivisionID,
                   ISNULL(d.DivisionName, '') AS DivisionName,
                   s.AliasName, s.Description, s.IsActive
            FROM tblSalesTeam s
            LEFT JOIN tblDivision d ON d.DivisionID = s.DivisionID
            WHERE s.SalesTeamID = @ID;", conn);
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
            SELECT s.SalesTeamID, s.SalesTeamCode, s.SalesTeamName,
                   ISNULL(s.DivisionID, 0) AS DivisionID,
                   ISNULL(d.DivisionName, '') AS DivisionName,
                   s.AliasName, s.Description, s.IsActive
            FROM tblSalesTeam s
            LEFT JOIN tblDivision d ON d.DivisionID = s.DivisionID
            ORDER BY s.IsActive DESC, s.SalesTeamCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read()) Records.Add(ReadRecord(dr));
    }

    private static SalesTeamRecord ReadRecord(SqlDataReader dr) => new()
    {
        SalesTeamID   = Convert.ToInt32(dr["SalesTeamID"]),
        SalesTeamCode = dr["SalesTeamCode"].ToString() ?? "",
        SalesTeamName = dr["SalesTeamName"].ToString() ?? "",
        DivisionID    = Convert.ToInt32(dr["DivisionID"]),
        DivisionName  = dr["DivisionName"].ToString() ?? "",
        AliasName     = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? "",
        Description   = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
        IsActive      = Convert.ToBoolean(dr["IsActive"])
    };

    private static void AddParams(SqlCommand cmd, int id, string code, string name,
        int divisionID, string alias, string description, bool isActive)
    {
        cmd.Parameters.AddWithValue("@ID",          id);
        cmd.Parameters.AddWithValue("@Code",        code.Trim());
        cmd.Parameters.AddWithValue("@Name",        name.Trim());
        cmd.Parameters.AddWithValue("@DivisionID",  divisionID <= 0 ? DBNull.Value : divisionID);
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
