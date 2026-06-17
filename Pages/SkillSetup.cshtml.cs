using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class SkillRecord
{
    public int SkillID { get; set; }
    public string SkillCode { get; set; } = "";
    public string SkillName { get; set; } = "";
    public string FieldType { get; set; } = "";
    public string DefaultTier { get; set; } = "";
    public string Description { get; set; } = "";
    public string ESCOAnchor { get; set; } = "";
    public string RoleCoverage { get; set; } = "";
    public string EmployeeNeed { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class SkillSetupModel : PageModel
{
    private readonly string _conn;

    public static readonly List<string> FieldTypes = new()
    {
        "Technical", "Functional", "Behavioral", "Managerial", "Leadership"
    };

    public static readonly List<string> DefaultTiers = new()
    {
        "1 - Foundation", "2 - Intermediate", "3 - Advanced", "4 - Expert", "5 - Master"
    };

    public static readonly List<string> EmployeeNeeds = new()
    {
        "Mandatory", "Recommended", "Optional"
    };

    public SkillSetupModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public string PageTitle => "Skill Setup";
    public SkillRecord Input { get; set; } = new();
    public List<SkillRecord> Records { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";
    public string NextSkillCode { get; private set; } = "";

    public void OnGet(int? editId)
    {
        LoadAlert();
        NextSkillCode = GenerateNextSkillCode();

        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);

        LoadRecords();
    }

    public IActionResult OnPostSave(
        int skillID,
        string skillCode,
        string skillName,
        string fieldType,
        string defaultTier,
        string description,
        string escoAnchor,
        string roleCoverage,
        string employeeNeed,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            TempData["Alert"] = "Skill Name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (skillID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblSkill
                    SET SkillName    = @SkillName,
                        FieldType    = @FieldType,
                        DefaultTier  = @DefaultTier,
                        Description  = @Description,
                        ESCOAnchor   = @ESCOAnchor,
                        RoleCoverage = @RoleCoverage,
                        EmployeeNeed = @EmployeeNeed,
                        IsActive     = @IsActive,
                        ModifiedOn   = GETDATE()
                    WHERE SkillID = @SkillID;", conn);
                AddParameters(cmd, skillID, skillCode, skillName, fieldType, defaultTier,
                    description, escoAnchor, roleCoverage, employeeNeed, isActive);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Skill updated successfully.";
            }
            else
            {
                // Re-generate code at insert time to avoid race conditions
                string code = GenerateNextSkillCode(conn);

                using var cmd = new SqlCommand(@"
                    INSERT INTO tblSkill
                        (SkillCode, SkillName, FieldType, DefaultTier, Description,
                         ESCOAnchor, RoleCoverage, EmployeeNeed, IsActive)
                    VALUES
                        (@SkillCode, @SkillName, @FieldType, @DefaultTier, @Description,
                         @ESCOAnchor, @RoleCoverage, @EmployeeNeed, @IsActive);", conn);
                AddParameters(cmd, 0, code, skillName, fieldType, defaultTier,
                    description, escoAnchor, roleCoverage, employeeNeed, isActive);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Skill added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "A skill with this code or name already exists.";
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
                UPDATE tblSkill
                SET IsActive = 0, ModifiedOn = GETDATE()
                WHERE SkillID = @SkillID;", conn);
            cmd.Parameters.AddWithValue("@SkillID", deleteId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Skill removed successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error removing skill: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private string GenerateNextSkillCode()
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            return GenerateNextSkillCode(conn);
        }
        catch
        {
            return "GB-SK-00001";
        }
    }

    private static string GenerateNextSkillCode(SqlConnection conn)
    {
        using var cmd = new SqlCommand(@"
            SELECT ISNULL(MAX(CAST(SUBSTRING(SkillCode, 7, 5) AS INT)), 0)
            FROM tblSkill
            WHERE SkillCode LIKE 'GB-SK-%'
              AND LEN(SkillCode) = 12;", conn);
        int next = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
        return $"GB-SK-{next:D5}";
    }

    private void LoadForEdit(int skillID)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT SkillID, SkillCode, SkillName, FieldType, DefaultTier,
                   Description, ESCOAnchor, RoleCoverage, EmployeeNeed, IsActive
            FROM tblSkill
            WHERE SkillID = @SkillID;", conn);
        cmd.Parameters.AddWithValue("@SkillID", skillID);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        if (dr.Read())
            Input = ReadSkill(dr);
    }

    private void LoadRecords()
    {
        Records.Clear();

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT SkillID, SkillCode, SkillName, FieldType, DefaultTier,
                   Description, ESCOAnchor, RoleCoverage, EmployeeNeed, IsActive
            FROM tblSkill
            ORDER BY IsActive DESC, SkillCode;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            Records.Add(ReadSkill(dr));
    }

    private static SkillRecord ReadSkill(SqlDataReader dr) => new()
    {
        SkillID      = Convert.ToInt32(dr["SkillID"]),
        SkillCode    = dr["SkillCode"].ToString() ?? "",
        SkillName    = dr["SkillName"].ToString() ?? "",
        FieldType    = dr["FieldType"]?.ToString() ?? "",
        DefaultTier  = dr["DefaultTier"]?.ToString() ?? "",
        Description  = dr["Description"]?.ToString() ?? "",
        ESCOAnchor   = dr["ESCOAnchor"]?.ToString() ?? "",
        RoleCoverage = dr["RoleCoverage"]?.ToString() ?? "",
        EmployeeNeed = dr["EmployeeNeed"]?.ToString() ?? "",
        IsActive     = Convert.ToBoolean(dr["IsActive"])
    };

    private static void AddParameters(SqlCommand cmd, int skillID, string skillCode,
        string skillName, string fieldType, string defaultTier, string description,
        string escoAnchor, string roleCoverage, string employeeNeed, bool isActive)
    {
        cmd.Parameters.AddWithValue("@SkillID",      skillID);
        cmd.Parameters.AddWithValue("@SkillCode",    skillCode.Trim());
        cmd.Parameters.AddWithValue("@SkillName",    skillName.Trim());
        cmd.Parameters.AddWithValue("@FieldType",    string.IsNullOrWhiteSpace(fieldType)    ? DBNull.Value : fieldType.Trim());
        cmd.Parameters.AddWithValue("@DefaultTier",  string.IsNullOrWhiteSpace(defaultTier)  ? DBNull.Value : defaultTier.Trim());
        cmd.Parameters.AddWithValue("@Description",  string.IsNullOrWhiteSpace(description)  ? DBNull.Value : description.Trim());
        cmd.Parameters.AddWithValue("@ESCOAnchor",   string.IsNullOrWhiteSpace(escoAnchor)   ? DBNull.Value : escoAnchor.Trim());
        cmd.Parameters.AddWithValue("@RoleCoverage", string.IsNullOrWhiteSpace(roleCoverage) ? DBNull.Value : roleCoverage.Trim());
        cmd.Parameters.AddWithValue("@EmployeeNeed", string.IsNullOrWhiteSpace(employeeNeed) ? DBNull.Value : employeeNeed.Trim());
        cmd.Parameters.AddWithValue("@IsActive",     isActive);
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType    = TempData["AlertType"]?.ToString() ?? "success";
    }
}
