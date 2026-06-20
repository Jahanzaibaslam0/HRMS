using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

// ── Standalone benefit record ─────────────────────────────────────────
public class BenefitItem
{
    public int    BenefitID   { get; set; }
    public string BenefitCode { get; set; } = "";
    public string BenefitName { get; set; } = "";
    public string BenefitType { get; set; } = "";
    public string Description { get; set; } = "";
    public bool   IsActive    { get; set; } = true;
}

// ── Page Model ────────────────────────────────────────────────────────
public class BenefitSetupModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    public static readonly string[] BenefitTypes = new[]
    {
        "Medical", "Leave", "Allowance", "Insurance", "Vehicle",
        "Housing", "Education", "Pension", "Bonus", "Fuel", "Other"
    };

    public string PageTitle => "Benefit Setup";

    public List<BenefitItem> Records      { get; set; } = new();
    public BenefitItem       Input        { get; set; } = new() { IsActive = true };
    public string            AlertMessage { get; set; } = "";
    public string            AlertType    { get; set; } = "success";

    public BenefitSetupModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    // ── GET ───────────────────────────────────────────────────────────
    public void OnGet(int? editId)
    {
        LoadAlert();
        LoadRecords();

        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);
        else
            Input.BenefitCode = GenerateNextCode();
    }

    // ── POST: Save ────────────────────────────────────────────────────
    public IActionResult OnPostSave(
        int benefitId, string benefitCode, string benefitName,
        string benefitType, string description, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(benefitName))
        {
            TempData["Alert"]     = "Benefit name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (benefitId > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblBenefit SET
                        BenefitCode  = @Code,
                        BenefitName  = @Name,
                        BenefitType  = @Type,
                        Description  = @Desc,
                        IsActive     = @IsActive,
                        ModifiedOn   = GETDATE(),
                        ModifiedByUserID = @ModifiedByUserID
                    WHERE BenefitID  = @Id;", conn);
                cmd.Parameters.AddWithValue("@Id",       benefitId);
                cmd.Parameters.AddWithValue("@Code",     string.IsNullOrWhiteSpace(benefitCode) ? DBNull.Value : (object)benefitCode.Trim());
                cmd.Parameters.AddWithValue("@Name",     benefitName.Trim());
                cmd.Parameters.AddWithValue("@Type",     string.IsNullOrWhiteSpace(benefitType) ? DBNull.Value : (object)benefitType.Trim());
                cmd.Parameters.AddWithValue("@Desc",     string.IsNullOrWhiteSpace(description) ? DBNull.Value : (object)description.Trim());
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Benefit updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblBenefit (BenefitCode, BenefitName, BenefitType, Description, IsActive, CreatedOn, CreatedByUserID)
                    VALUES (@Code, @Name, @Type, @Desc, @IsActive, GETDATE(), @CreatedByUserID);", conn);
                cmd.Parameters.AddWithValue("@Code",     string.IsNullOrWhiteSpace(benefitCode) ? DBNull.Value : (object)benefitCode.Trim());
                cmd.Parameters.AddWithValue("@Name",     benefitName.Trim());
                cmd.Parameters.AddWithValue("@Type",     string.IsNullOrWhiteSpace(benefitType) ? DBNull.Value : (object)benefitType.Trim());
                cmd.Parameters.AddWithValue("@Desc",     string.IsNullOrWhiteSpace(description) ? DBNull.Value : (object)description.Trim());
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Benefit added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"]     = "Benefit name already exists.";
            TempData["AlertType"] = "error";
        }
        catch (Exception ex)
        {
            TempData["Alert"]     = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    // ── POST: Delete ──────────────────────────────────────────────────
    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var cmd = new SqlCommand(@"
                UPDATE tblBenefit SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                WHERE BenefitID = @Id;", conn);
            cmd.Parameters.AddWithValue("@Id", deleteId);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            cmd.ExecuteNonQuery();
            TempData["Alert"]     = "Benefit deactivated.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"]     = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }
        return RedirectToPage();
    }

    // ── Private helpers ───────────────────────────────────────────────
    private void LoadRecords()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd  = new SqlCommand(@"
            SELECT BenefitID, BenefitCode, BenefitName, BenefitType, Description, IsActive
            FROM   tblBenefit
            ORDER BY IsActive DESC, BenefitName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Records.Add(new BenefitItem
            {
                BenefitID   = Convert.ToInt32(dr["BenefitID"]),
                BenefitCode = dr["BenefitCode"] == DBNull.Value ? "" : dr["BenefitCode"].ToString()!,
                BenefitName = dr["BenefitName"].ToString()!,
                BenefitType = dr["BenefitType"] == DBNull.Value ? "" : dr["BenefitType"].ToString()!,
                Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString()!,
                IsActive    = Convert.ToBoolean(dr["IsActive"])
            });
        }
    }

    private void LoadForEdit(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd  = new SqlCommand(@"
            SELECT BenefitID, BenefitCode, BenefitName, BenefitType, Description, IsActive
            FROM   tblBenefit WHERE BenefitID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            Input = new BenefitItem
            {
                BenefitID   = id,
                BenefitCode = dr["BenefitCode"] == DBNull.Value ? "" : dr["BenefitCode"].ToString()!,
                BenefitName = dr["BenefitName"].ToString()!,
                BenefitType = dr["BenefitType"] == DBNull.Value ? "" : dr["BenefitType"].ToString()!,
                Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString()!,
                IsActive    = Convert.ToBoolean(dr["IsActive"])
            };
        }
    }

    private string GenerateNextCode()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd  = new SqlCommand(@"
            SELECT TOP 1 BenefitCode FROM tblBenefit
            WHERE BenefitCode LIKE 'GB-BN-%'
            ORDER BY BenefitCode DESC;", conn);
        conn.Open();
        var last = cmd.ExecuteScalar()?.ToString();
        if (!string.IsNullOrEmpty(last) && last.Length >= 9)
        {
            if (int.TryParse(last[6..], out int num))
                return $"GB-BN-{(num + 1):D5}";
        }
        return "GB-BN-00001";
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType    = TempData["AlertType"]?.ToString() ?? "success";
    }
}
