using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class MemorandumRecord
{
    public int MemorandumID { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int DepartmentID { get; set; }
    public string DepartmentName { get; set; } = "";
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime ValidTillDate { get; set; } = DateTime.Today.AddMonths(1);
    public bool IsActive { get; set; } = true;
    public string DocumentPath { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
}

public class MemorandumSetupModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg" };

    public MemorandumSetupModel(IConfiguration config, AuthService auth, IWebHostEnvironment env)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
        _env = env;
    }

    public string PageTitle => "Memorandum Setup";
    public MemorandumRecord Input { get; set; } = new();
    public List<MemorandumRecord> Records { get; set; } = new();
    public List<DeptLookupItem> Departments { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        LoadDepartments();
        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);
        LoadRecords();
    }

    public async Task<IActionResult> OnPostSaveAsync(
        int memorandumID, string memorandumName, string description,
        int departmentID, DateTime startDate, DateTime validTillDate, bool isActive,
        IFormFile? documentFile)
    {
        if (string.IsNullOrWhiteSpace(memorandumName))
        {
            TempData["Alert"] = "Memorandum name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        if (validTillDate.Date < startDate.Date)
        {
            TempData["Alert"] = "Valid till date cannot be before start date.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            string? docPath = null;
            string? originalName = null;

            if (memorandumID > 0)
            {
                var existing = LoadRecordById(memorandumID);
                docPath = existing?.DocumentPath;
                originalName = existing?.OriginalFileName;
            }

            if (documentFile != null && documentFile.Length > 0)
            {
                var saved = SaveDocumentFile(documentFile, memorandumID > 0 ? memorandumID : 0);
                docPath = saved.path;
                originalName = saved.originalName;
            }

            using var conn = new SqlConnection(_conn);
            conn.Open();

            var deptParam = departmentID > 0 ? (object)departmentID : DBNull.Value;

            if (memorandumID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblMemorandum
                    SET MemorandumName = @Name,
                        Description = @Description,
                        DepartmentID = @DepartmentID,
                        StartDate = @StartDate,
                        ValidTillDate = @ValidTillDate,
                        IsActive = @IsActive,
                        DocumentPath = @DocumentPath,
                        OriginalFileName = @OriginalFileName,
                        ModifiedOn = GETDATE(),
                        ModifiedByUserID = @ModifiedByUserID
                    WHERE MemorandumID = @Id;", conn);
                BindParams(cmd, memorandumID, memorandumName, description, deptParam, startDate, validTillDate, isActive, docPath, originalName);
                AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Memorandum updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblMemorandum
                        (MemorandumName, Description, DepartmentID, StartDate, ValidTillDate, IsActive,
                         DocumentPath, OriginalFileName, CreatedOn, CreatedByUserID)
                    VALUES
                        (@Name, @Description, @DepartmentID, @StartDate, @ValidTillDate, @IsActive,
                         @DocumentPath, @OriginalFileName, GETDATE(), @CreatedByUserID);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
                BindParams(cmd, 0, memorandumName, description, deptParam, startDate, validTillDate, isActive, docPath, originalName, isInsert: true);
                AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                var newId = Convert.ToInt32(cmd.ExecuteScalar());

                if (documentFile != null && documentFile.Length > 0 && newId > 0)
                {
                    var saved = SaveDocumentFile(documentFile, newId);
                    using var upd = new SqlCommand(@"
                        UPDATE tblMemorandum
                        SET DocumentPath = @Path, OriginalFileName = @Orig
                        WHERE MemorandumID = @Id;", conn);
                    upd.Parameters.AddWithValue("@Path", (object?)saved.path ?? DBNull.Value);
                    upd.Parameters.AddWithValue("@Orig", (object?)saved.originalName ?? DBNull.Value);
                    upd.Parameters.AddWithValue("@Id", newId);
                    upd.ExecuteNonQuery();
                }

                TempData["Alert"] = "Memorandum added successfully.";
            }

            TempData["AlertType"] = "success";
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
                UPDATE tblMemorandum
                SET IsActive = 0,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE MemorandumID = @Id;", conn);
            cmd.Parameters.AddWithValue("@Id", deleteId);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Memorandum deactivated successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error removing record: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private static void BindParams(SqlCommand cmd, int id, string name, string description, object deptParam,
        DateTime startDate, DateTime validTillDate, bool isActive, string? docPath, string? originalName, bool isInsert = false)
    {
        if (!isInsert) cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@Name", name.Trim());
        cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? DBNull.Value : description.Trim());
        cmd.Parameters.AddWithValue("@DepartmentID", deptParam);
        cmd.Parameters.AddWithValue("@StartDate", startDate.Date);
        cmd.Parameters.AddWithValue("@ValidTillDate", validTillDate.Date);
        cmd.Parameters.AddWithValue("@IsActive", isActive);
        cmd.Parameters.AddWithValue("@DocumentPath", string.IsNullOrWhiteSpace(docPath) ? DBNull.Value : docPath);
        cmd.Parameters.AddWithValue("@OriginalFileName", string.IsNullOrWhiteSpace(originalName) ? DBNull.Value : originalName);
    }

    private (string? path, string? originalName) SaveDocumentFile(IFormFile file, int memorandumId)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("File type not allowed. Use PDF, Word, Excel, or image files.");

        var uploads = Path.Combine(_env.WebRootPath, "uploads", "memorandums");
        Directory.CreateDirectory(uploads);

        var safeName = $"memo_{memorandumId}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
        var fullPath = Path.Combine(uploads, safeName);

        using (var fs = System.IO.File.Create(fullPath))
            file.CopyTo(fs);

        return ($"/uploads/memorandums/{safeName}", file.FileName);
    }

    private MemorandumRecord? LoadRecordById(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT MemorandumID, MemorandumName, Description,
                   ISNULL(DepartmentID, 0) AS DepartmentID,
                   StartDate, ValidTillDate, IsActive, DocumentPath, OriginalFileName
            FROM tblMemorandum WHERE MemorandumID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (!dr.Read()) return null;
        return new MemorandumRecord
        {
            MemorandumID = Convert.ToInt32(dr["MemorandumID"]),
            Name = dr["MemorandumName"].ToString() ?? "",
            Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
            DepartmentID = Convert.ToInt32(dr["DepartmentID"]),
            StartDate = Convert.ToDateTime(dr["StartDate"]),
            ValidTillDate = Convert.ToDateTime(dr["ValidTillDate"]),
            IsActive = Convert.ToBoolean(dr["IsActive"]),
            DocumentPath = dr["DocumentPath"] == DBNull.Value ? "" : dr["DocumentPath"].ToString() ?? "",
            OriginalFileName = dr["OriginalFileName"] == DBNull.Value ? "" : dr["OriginalFileName"].ToString() ?? ""
        };
    }

    private void LoadDepartments()
    {
        Departments.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT DepartmentID, DepartmentName FROM tblDepartment
            WHERE IsActive = 1 ORDER BY DepartmentName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Departments.Add(new DeptLookupItem
            {
                Id = Convert.ToInt32(dr["DepartmentID"]),
                Name = dr["DepartmentName"].ToString() ?? ""
            });
        }
    }

    private void LoadForEdit(int id)
    {
        Input = LoadRecordById(id) ?? new MemorandumRecord();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT ISNULL(d.DepartmentName, 'All Departments') AS DepartmentName
            FROM tblMemorandum m
            LEFT JOIN tblDepartment d ON d.DepartmentID = m.DepartmentID
            WHERE m.MemorandumID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        conn.Open();
        Input.DepartmentName = cmd.ExecuteScalar()?.ToString() ?? "";
    }

    private void LoadRecords()
    {
        Records.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT m.MemorandumID, m.MemorandumName, m.Description,
                   ISNULL(m.DepartmentID, 0) AS DepartmentID,
                   ISNULL(d.DepartmentName, 'All Departments') AS DepartmentName,
                   m.StartDate, m.ValidTillDate, m.IsActive,
                   m.DocumentPath, m.OriginalFileName
            FROM tblMemorandum m
            LEFT JOIN tblDepartment d ON d.DepartmentID = m.DepartmentID
            ORDER BY m.IsActive DESC, m.StartDate DESC, m.MemorandumID DESC;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Records.Add(new MemorandumRecord
            {
                MemorandumID = Convert.ToInt32(dr["MemorandumID"]),
                Name = dr["MemorandumName"].ToString() ?? "",
                Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
                DepartmentID = Convert.ToInt32(dr["DepartmentID"]),
                DepartmentName = dr["DepartmentName"].ToString() ?? "",
                StartDate = Convert.ToDateTime(dr["StartDate"]),
                ValidTillDate = Convert.ToDateTime(dr["ValidTillDate"]),
                IsActive = Convert.ToBoolean(dr["IsActive"]),
                DocumentPath = dr["DocumentPath"] == DBNull.Value ? "" : dr["DocumentPath"].ToString() ?? "",
                OriginalFileName = dr["OriginalFileName"] == DBNull.Value ? "" : dr["OriginalFileName"].ToString() ?? ""
            });
        }
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType = TempData["AlertType"]?.ToString() ?? "success";
    }
}
