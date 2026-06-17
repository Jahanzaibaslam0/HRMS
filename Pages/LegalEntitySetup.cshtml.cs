using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class LegalEntityRecord
{
    public int LegalEntityID { get; set; }
    public string LegalEntityCode { get; set; } = "";
    public string LegalEntityName { get; set; } = "";
    public string AliasName { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class LegalEntitySetupModel : PageModel
{
    private readonly string _conn;

    public LegalEntitySetupModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public string PageTitle => "Legal Entity Setup";
    public LegalEntityRecord Input { get; set; } = new();
    public List<LegalEntityRecord> Records { get; set; } = new();
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
        int legalEntityID,
        string legalEntityCode,
        string legalEntityName,
        string aliasName,
        string description,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(legalEntityCode))
        {
            TempData["Alert"] = "Legal Entity Code is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(legalEntityName))
        {
            TempData["Alert"] = "Legal Entity Name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (legalEntityID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblLegalEntity
                    SET LegalEntityCode = @Code,
                        LegalEntityName = @Name,
                        AliasName       = @AliasName,
                        Description     = @Description,
                        IsActive        = @IsActive,
                        ModifiedOn      = GETDATE()
                    WHERE LegalEntityID = @ID;", conn);
                AddParams(cmd, legalEntityID, legalEntityCode, legalEntityName, aliasName, description, isActive);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Legal Entity updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblLegalEntity
                        (LegalEntityCode, LegalEntityName, AliasName, Description, IsActive)
                    VALUES
                        (@Code, @Name, @AliasName, @Description, @IsActive);", conn);
                AddParams(cmd, 0, legalEntityCode, legalEntityName, aliasName, description, isActive);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Legal Entity added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "A Legal Entity with this code already exists.";
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
                UPDATE tblLegalEntity
                SET IsActive = 0, ModifiedOn = GETDATE()
                WHERE LegalEntityID = @ID;", conn);
            cmd.Parameters.AddWithValue("@ID", deleteId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Legal Entity removed successfully.";
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
            SELECT LegalEntityID, LegalEntityCode, LegalEntityName,
                   AliasName, Description, IsActive
            FROM tblLegalEntity
            WHERE LegalEntityID = @ID;", conn);
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
            SELECT LegalEntityID, LegalEntityCode, LegalEntityName,
                   AliasName, Description, IsActive
            FROM tblLegalEntity
            ORDER BY IsActive DESC, LegalEntityCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read()) Records.Add(ReadRecord(dr));
    }

    private static LegalEntityRecord ReadRecord(SqlDataReader dr) => new()
    {
        LegalEntityID   = Convert.ToInt32(dr["LegalEntityID"]),
        LegalEntityCode = dr["LegalEntityCode"].ToString() ?? "",
        LegalEntityName = dr["LegalEntityName"].ToString() ?? "",
        AliasName       = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? "",
        Description     = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
        IsActive        = Convert.ToBoolean(dr["IsActive"])
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
