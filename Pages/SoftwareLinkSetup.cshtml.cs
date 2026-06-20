using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class SoftwareLinkRecord
{
    public int    SoftwareLinkID { get; set; }
    public string SoftwareName   { get; set; } = "";
    public string SoftwareUrl    { get; set; } = "";
    public string Category       { get; set; } = "";
    public string Description    { get; set; } = "";
    public int    SortOrder      { get; set; }
    public bool   IsActive       { get; set; } = true;
}

public class SoftwareLinkSetupModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    public SoftwareLinkSetupModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    public string PageTitle => "Software Link Setup";
    public SoftwareLinkRecord Input { get; set; } = new();
    public List<SoftwareLinkRecord> Records { get; set; } = new();
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
        int softwareLinkID, string softwareName, string softwareUrl,
        string category, string description, int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(softwareName))
        {
            TempData["Alert"] = "Software name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(softwareUrl))
        {
            TempData["Alert"] = "Software URL is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (softwareLinkID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblSoftwareLink
                    SET SoftwareName = @SoftwareName,
                        SoftwareUrl  = @SoftwareUrl,
                        Category     = @Category,
                        Description  = @Description,
                        SortOrder    = @SortOrder,
                        IsActive     = @IsActive,
                        ModifiedOn   = GETDATE(),
                        ModifiedByUserID = @ModifiedByUserID
                    WHERE SoftwareLinkID = @SoftwareLinkID;", conn);
                AddParams(cmd, softwareLinkID, softwareName, softwareUrl, category, description, sortOrder, isActive);
                AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Software link updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblSoftwareLink
                        (SoftwareName, SoftwareUrl, Category, Description, SortOrder, IsActive, CreatedOn, CreatedByUserID)
                    VALUES
                        (@SoftwareName, @SoftwareUrl, @Category, @Description, @SortOrder, @IsActive, GETDATE(), @CreatedByUserID);", conn);
                AddParams(cmd, 0, softwareName, softwareUrl, category, description, sortOrder, isActive);
                AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Software link added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "This software link already exists.";
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
                UPDATE tblSoftwareLink
                SET IsActive = 0,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE SoftwareLinkID = @SoftwareLinkID;", conn);
            cmd.Parameters.AddWithValue("@SoftwareLinkID", deleteId);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Software link removed successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error removing record: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private static void AddParams(SqlCommand cmd, int id, string name, string url,
        string category, string description, int sortOrder, bool isActive)
    {
        cmd.Parameters.AddWithValue("@SoftwareLinkID", id);
        cmd.Parameters.AddWithValue("@SoftwareName", name.Trim());
        cmd.Parameters.AddWithValue("@SoftwareUrl", url.Trim());
        cmd.Parameters.AddWithValue("@Category", string.IsNullOrWhiteSpace(category) ? DBNull.Value : category.Trim());
        cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? DBNull.Value : description.Trim());
        cmd.Parameters.AddWithValue("@SortOrder", sortOrder <= 0 ? 1 : sortOrder);
        cmd.Parameters.AddWithValue("@IsActive", isActive);
    }

    private void LoadForEdit(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT SoftwareLinkID, SoftwareName, SoftwareUrl, Category, Description, SortOrder, IsActive
            FROM tblSoftwareLink
            WHERE SoftwareLinkID = @SoftwareLinkID;", conn);
        cmd.Parameters.AddWithValue("@SoftwareLinkID", id);
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
            SELECT SoftwareLinkID, SoftwareName, SoftwareUrl, Category, Description, SortOrder, IsActive
            FROM tblSoftwareLink
            ORDER BY IsActive DESC, SortOrder, SoftwareName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            Records.Add(ReadRecord(dr));
    }

    private static SoftwareLinkRecord ReadRecord(SqlDataReader dr) => new()
    {
        SoftwareLinkID = Convert.ToInt32(dr["SoftwareLinkID"]),
        SoftwareName   = dr["SoftwareName"].ToString() ?? "",
        SoftwareUrl    = dr["SoftwareUrl"].ToString() ?? "",
        Category       = dr["Category"] == DBNull.Value ? "" : dr["Category"].ToString() ?? "",
        Description    = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
        SortOrder      = dr["SortOrder"] == DBNull.Value ? 1 : Convert.ToInt32(dr["SortOrder"]),
        IsActive       = Convert.ToBoolean(dr["IsActive"])
    };

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType = TempData["AlertType"]?.ToString() ?? "success";
    }
}
