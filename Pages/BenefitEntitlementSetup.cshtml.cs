using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

// ── Linked benefit (from junction + tblBenefit join) ─────────────────
public class BenefitRecord
{
    public int    DetailID    { get; set; }   // tblBenefitEntitlementDetail PK
    public int    BenefitID   { get; set; }
    public string BenefitCode { get; set; } = "";
    public string BenefitName { get; set; } = "";
    public string BenefitType { get; set; } = "";
    public string Description { get; set; } = "";
}

// ── Page Model ────────────────────────────────────────────────────────
public class BenefitEntitlementSetupModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    public string PageTitle => "Benefit Entitlement Setup";

    // ── Page state ────────────────────────────────────────────────────
    public List<LookupRecord>  Entitlements          { get; set; } = new();
    public LookupRecord        EntitlementInput      { get; set; } = new() { IsActive = true };

    // Benefits already linked to the current entitlement (via junction)
    public List<BenefitRecord> LinkedBenefits        { get; set; } = new();
    // All active benefits NOT yet linked (available to add)
    public List<BenefitItem>   AvailableBenefits     { get; set; } = new();

    public int    ManageEntitlementID   { get; set; }
    public string ManageEntitlementName { get; set; } = "";
    public string AlertMessage          { get; set; } = "";
    public string AlertType             { get; set; } = "success";

    public BenefitEntitlementSetupModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    // ── GET ───────────────────────────────────────────────────────────
    public void OnGet(int? editId, int? manageId)
    {
        LoadAlert();
        LoadEntitlements();

        if (editId.HasValue && editId > 0)
            LoadEntitlementForEdit(editId.Value);

        int targetId = manageId ?? editId ?? 0;
        if (targetId > 0)
        {
            ManageEntitlementID   = targetId;
            ManageEntitlementName = GetEntitlementName(targetId);
            LoadLinkedBenefits(targetId);
            LoadAvailableBenefits(targetId);
        }
    }

    // ── AJAX: returns JSON list of linked benefits for entitlement ────
    public IActionResult OnGetGetBenefits(int entitlementId)
    {
        var list = new List<object>();
        if (entitlementId <= 0) return new JsonResult(list);

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT b.BenefitID, b.BenefitCode, b.BenefitName, b.BenefitType, b.Description
            FROM   tblBenefitEntitlementDetail d
            INNER JOIN tblBenefit b ON b.BenefitID = d.BenefitID
            WHERE  d.BenefitEntitlementID = @Id AND b.IsActive = 1
            ORDER BY b.BenefitName;", conn);
        cmd.Parameters.AddWithValue("@Id", entitlementId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            list.Add(new
            {
                benefitID   = Convert.ToInt32(dr["BenefitID"]),
                benefitCode = dr["BenefitCode"] == DBNull.Value ? "" : dr["BenefitCode"].ToString(),
                benefitName = dr["BenefitName"].ToString(),
                benefitType = dr["BenefitType"] == DBNull.Value ? "" : dr["BenefitType"].ToString(),
                description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString()
            });
        }
        return new JsonResult(list);
    }

    // ── POST: Save Entitlement ────────────────────────────────────────
    public IActionResult OnPostSaveEntitlement(
        int itemId, string itemName, string aliasName, bool isActive, int manageId)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            TempData["Alert"]     = "Benefit Entitlement name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { manageId });
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            int savedId;

            if (itemId > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblBenefitEntitlement SET
                        BenefitEntitlementName = @Name,
                        AliasName              = @Alias,
                        IsActive               = @IsActive,
                        ModifiedOn             = GETDATE(),
                        ModifiedByUserID       = @ModifiedByUserID
                    WHERE BenefitEntitlementID = @Id;", conn);
                cmd.Parameters.AddWithValue("@Id",       itemId);
                cmd.Parameters.AddWithValue("@Name",     itemName.Trim());
                cmd.Parameters.AddWithValue("@Alias",    string.IsNullOrWhiteSpace(aliasName) ? DBNull.Value : (object)aliasName.Trim());
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                savedId = itemId;
                TempData["Alert"] = "Benefit Entitlement updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblBenefitEntitlement (BenefitEntitlementName, AliasName, IsActive, CreatedOn, CreatedByUserID)
                    VALUES (@Name, @Alias, @IsActive, GETDATE(), @CreatedByUserID);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
                cmd.Parameters.AddWithValue("@Name",     itemName.Trim());
                cmd.Parameters.AddWithValue("@Alias",    string.IsNullOrWhiteSpace(aliasName) ? DBNull.Value : (object)aliasName.Trim());
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                savedId = Convert.ToInt32(cmd.ExecuteScalar());
                TempData["Alert"] = "Benefit Entitlement added successfully.";
            }

            TempData["AlertType"] = "success";
            return RedirectToPage(new { manageId = savedId });
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"]     = "Entitlement name already exists.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { manageId });
        }
        catch (Exception ex)
        {
            TempData["Alert"]     = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
            return RedirectToPage(new { manageId });
        }
    }

    // ── POST: Delete Entitlement ──────────────────────────────────────
    public IActionResult OnPostDeleteEntitlement(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var cmd = new SqlCommand(@"
                UPDATE tblBenefitEntitlement SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                WHERE BenefitEntitlementID = @Id;", conn);
            cmd.Parameters.AddWithValue("@Id", deleteId);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            cmd.ExecuteNonQuery();
            TempData["Alert"]     = "Benefit Entitlement removed.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"]     = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }
        return RedirectToPage();
    }

    // ── POST: Link a Benefit to Entitlement (via junction) ───────────
    public IActionResult OnPostLinkBenefit(int benefitEntitlementID, int benefitID)
    {
        if (benefitID <= 0)
        {
            TempData["Alert"]     = "Please select a benefit to add.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { manageId = benefitEntitlementID });
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            // Prevent duplicates
            using var checkCmd = new SqlCommand(@"
                SELECT COUNT(*) FROM tblBenefitEntitlementDetail
                WHERE BenefitEntitlementID = @EntID AND BenefitID = @BenID;", conn);
            checkCmd.Parameters.AddWithValue("@EntID", benefitEntitlementID);
            checkCmd.Parameters.AddWithValue("@BenID", benefitID);
            int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

            if (exists > 0)
            {
                TempData["Alert"]     = "This benefit is already added to this entitlement.";
                TempData["AlertType"] = "error";
                return RedirectToPage(new { manageId = benefitEntitlementID });
            }

            using var cmd = new SqlCommand(@"
                INSERT INTO tblBenefitEntitlementDetail (BenefitEntitlementID, BenefitID)
                VALUES (@EntID, @BenID);", conn);
            cmd.Parameters.AddWithValue("@EntID", benefitEntitlementID);
            cmd.Parameters.AddWithValue("@BenID", benefitID);
            cmd.ExecuteNonQuery();

            TempData["Alert"]     = "Benefit added to entitlement.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"]     = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }
        return RedirectToPage(new { manageId = benefitEntitlementID });
    }

    // ── POST: Unlink a Benefit from Entitlement ───────────────────────
    public IActionResult OnPostUnlinkBenefit(int detailID, int benefitEntitlementID)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var cmd = new SqlCommand(@"
                DELETE FROM tblBenefitEntitlementDetail WHERE DetailID = @Id;", conn);
            cmd.Parameters.AddWithValue("@Id", detailID);
            cmd.ExecuteNonQuery();
            TempData["Alert"]     = "Benefit removed from entitlement.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"]     = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }
        return RedirectToPage(new { manageId = benefitEntitlementID });
    }

    // ── Private helpers ───────────────────────────────────────────────
    private void LoadEntitlements()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT e.BenefitEntitlementID, e.BenefitEntitlementName, e.AliasName, e.IsActive,
                   (SELECT COUNT(*) FROM tblBenefitEntitlementDetail d
                    WHERE d.BenefitEntitlementID = e.BenefitEntitlementID) AS BenefitCount
            FROM   tblBenefitEntitlement e
            ORDER BY e.IsActive DESC, e.BenefitEntitlementName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var rec = new LookupRecord
            {
                Id        = Convert.ToInt32(dr["BenefitEntitlementID"]),
                Name      = dr["BenefitEntitlementName"].ToString() ?? "",
                AliasName = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? "",
                IsActive  = Convert.ToBoolean(dr["IsActive"])
            };
            // Store BenefitCount in AliasName field temporarily for display
            // We'll use a separate approach — add the count to a tag
            Entitlements.Add(rec);
            EntitlementBenefitCounts[rec.Id] = Convert.ToInt32(dr["BenefitCount"]);
        }
    }

    // Count of benefits per entitlement for the list
    public Dictionary<int, int> EntitlementBenefitCounts { get; set; } = new();

    private void LoadEntitlementForEdit(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT BenefitEntitlementID, BenefitEntitlementName, AliasName, IsActive
            FROM   tblBenefitEntitlement WHERE BenefitEntitlementID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            EntitlementInput = new LookupRecord
            {
                Id        = Convert.ToInt32(dr["BenefitEntitlementID"]),
                Name      = dr["BenefitEntitlementName"].ToString() ?? "",
                AliasName = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? "",
                IsActive  = Convert.ToBoolean(dr["IsActive"])
            };
        }
    }

    private void LoadLinkedBenefits(int entitlementId)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT d.DetailID, b.BenefitID, b.BenefitCode, b.BenefitName, b.BenefitType, b.Description
            FROM   tblBenefitEntitlementDetail d
            INNER JOIN tblBenefit b ON b.BenefitID = d.BenefitID
            WHERE  d.BenefitEntitlementID = @Id
            ORDER BY b.BenefitName;", conn);
        cmd.Parameters.AddWithValue("@Id", entitlementId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            LinkedBenefits.Add(new BenefitRecord
            {
                DetailID    = Convert.ToInt32(dr["DetailID"]),
                BenefitID   = Convert.ToInt32(dr["BenefitID"]),
                BenefitCode = dr["BenefitCode"] == DBNull.Value ? "" : dr["BenefitCode"].ToString()!,
                BenefitName = dr["BenefitName"].ToString()!,
                BenefitType = dr["BenefitType"] == DBNull.Value ? "" : dr["BenefitType"].ToString()!,
                Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString()!
            });
        }
    }

    private void LoadAvailableBenefits(int entitlementId)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT BenefitID, BenefitCode, BenefitName, BenefitType
            FROM   tblBenefit
            WHERE  IsActive = 1
              AND  BenefitID NOT IN (
                    SELECT BenefitID FROM tblBenefitEntitlementDetail
                    WHERE BenefitEntitlementID = @Id
              )
            ORDER BY BenefitName;", conn);
        cmd.Parameters.AddWithValue("@Id", entitlementId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            AvailableBenefits.Add(new BenefitItem
            {
                BenefitID   = Convert.ToInt32(dr["BenefitID"]),
                BenefitCode = dr["BenefitCode"] == DBNull.Value ? "" : dr["BenefitCode"].ToString()!,
                BenefitName = dr["BenefitName"].ToString()!,
                BenefitType = dr["BenefitType"] == DBNull.Value ? "" : dr["BenefitType"].ToString()!
            });
        }
    }

    private string GetEntitlementName(int id)
        => Entitlements.FirstOrDefault(e => e.Id == id)?.Name ?? "";

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType    = TempData["AlertType"]?.ToString() ?? "success";
    }
}
