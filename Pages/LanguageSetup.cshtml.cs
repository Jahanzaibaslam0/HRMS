using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class LanguageRecord
{
    public int LanguageID { get; set; }
    public string LanguageCode { get; set; } = "";
    public string LanguageName { get; set; } = "";
    public string NativeName { get; set; } = "";
    public string Region { get; set; } = "";
    public string Source { get; set; } = "";
    public bool IsPriority { get; set; }
    public bool IsActive { get; set; } = true;
}

public class LanguageSetupModel : PageModel
{
    private readonly string _conn;

    public LanguageSetupModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public LanguageRecord Input { get; set; } = new();
    public List<LanguageRecord> Languages { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        if (editId.HasValue && editId > 0)
        {
            LoadForEdit(editId.Value);
        }

        LoadLanguages();
    }

    public IActionResult OnPostSave(
        int languageID,
        string languageCode,
        string languageName,
        string nativeName,
        string region,
        string source,
        bool isPriority,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            TempData["Alert"] = "Language Code is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(languageName))
        {
            TempData["Alert"] = "Language Name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (languageID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblLanguage
                    SET LanguageCode = @LanguageCode,
                        LanguageName = @LanguageName,
                        NativeName = @NativeName,
                        Region = @Region,
                        Source = @Source,
                        IsPriority = @IsPriority,
                        IsActive = @IsActive,
                        ModifiedOn = GETDATE()
                    WHERE LanguageID = @LanguageID;", conn);
                AddSaveParameters(cmd, languageID, languageCode, languageName, nativeName, region, source, isPriority, isActive);
                cmd.ExecuteNonQuery();

                TempData["Alert"] = "Language record updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblLanguage
                        (LanguageCode, LanguageName, NativeName, Region, Source, IsPriority, IsActive)
                    VALUES
                        (@LanguageCode, @LanguageName, @NativeName, @Region, @Source, @IsPriority, @IsActive);", conn);
                AddSaveParameters(cmd, languageID, languageCode, languageName, nativeName, region, source, isPriority, isActive);
                cmd.ExecuteNonQuery();

                TempData["Alert"] = "Language record added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "This language code already exists.";
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
                UPDATE tblLanguage
                SET IsActive = 0,
                    ModifiedOn = GETDATE()
                WHERE LanguageID = @LanguageID;", conn);
            cmd.Parameters.AddWithValue("@LanguageID", deleteId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Language record removed successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error removing record: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    public IActionResult OnGetDownloadExcel()
    {
        var records = LoadLanguageRecords();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Languages");
        var headers = new[]
        {
            "LanguageID",
            "Code",
            "Name",
            "Native Name",
            "Region",
            "Source",
            "Priority Flag",
            "Status"
        };

        for (var col = 0; col < headers.Length; col++)
        {
            worksheet.Cell(1, col + 1).Value = headers[col];
            worksheet.Cell(1, col + 1).Style.Font.Bold = true;
        }

        for (var row = 0; row < records.Count; row++)
        {
            var language = records[row];
            var excelRow = row + 2;
            worksheet.Cell(excelRow, 1).Value = language.LanguageID;
            worksheet.Cell(excelRow, 2).Value = language.LanguageCode;
            worksheet.Cell(excelRow, 3).Value = language.LanguageName;
            worksheet.Cell(excelRow, 4).Value = language.NativeName;
            worksheet.Cell(excelRow, 5).Value = language.Region;
            worksheet.Cell(excelRow, 6).Value = language.Source;
            worksheet.Cell(excelRow, 7).Value = language.IsPriority ? "Yes" : "No";
            worksheet.Cell(excelRow, 8).Value = language.IsActive ? "Active" : "Inactive";
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"LanguageSetup_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    public IActionResult OnPostUploadExcel(IFormFile languageFile)
    {
        if (languageFile == null || languageFile.Length == 0)
        {
            TempData["Alert"] = "Please select an Excel file to upload.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var workbook = new XLWorkbook(languageFile.OpenReadStream());
            var worksheet = workbook.Worksheets.First();
            var rows = worksheet.RowsUsed().Skip(1).ToList();

            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            var processed = 0;
            foreach (var row in rows)
            {
                var code = row.Cell(2).GetString().Trim();
                var name = row.Cell(3).GetString().Trim();
                if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                {
                    throw new InvalidOperationException($"Row {row.RowNumber()} must have both Code and Name.");
                }

                var record = new LanguageRecord
                {
                    LanguageID = TryReadInt(row.Cell(1).GetString()),
                    LanguageCode = code,
                    LanguageName = name,
                    NativeName = row.Cell(4).GetString().Trim(),
                    Region = row.Cell(5).GetString().Trim(),
                    Source = row.Cell(6).GetString().Trim(),
                    IsPriority = ReadBool(row.Cell(7).GetString(), false),
                    IsActive = ReadBool(row.Cell(8).GetString(), true)
                };

                UpsertLanguage(conn, tx, record);
                processed++;
            }

            tx.Commit();

            TempData["Alert"] = $"{processed} language record(s) uploaded successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error uploading language data: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private static void AddSaveParameters(
        SqlCommand cmd,
        int languageID,
        string languageCode,
        string languageName,
        string nativeName,
        string region,
        string source,
        bool isPriority,
        bool isActive)
    {
        cmd.Parameters.AddWithValue("@LanguageID", languageID);
        cmd.Parameters.AddWithValue("@LanguageCode", languageCode.Trim());
        cmd.Parameters.AddWithValue("@LanguageName", languageName.Trim());
        cmd.Parameters.AddWithValue("@NativeName", string.IsNullOrWhiteSpace(nativeName) ? DBNull.Value : nativeName.Trim());
        cmd.Parameters.AddWithValue("@Region", string.IsNullOrWhiteSpace(region) ? DBNull.Value : region.Trim());
        cmd.Parameters.AddWithValue("@Source", string.IsNullOrWhiteSpace(source) ? DBNull.Value : source.Trim());
        cmd.Parameters.AddWithValue("@IsPriority", isPriority);
        cmd.Parameters.AddWithValue("@IsActive", isActive);
    }

    private void LoadForEdit(int languageID)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT LanguageID, LanguageCode, LanguageName, NativeName, Region, Source, IsPriority, IsActive
            FROM tblLanguage
            WHERE LanguageID = @LanguageID;", conn);
        cmd.Parameters.AddWithValue("@LanguageID", languageID);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            Input = ReadLanguage(dr);
        }
    }

    private void LoadLanguages()
    {
        Languages = LoadLanguageRecords();
    }

    private List<LanguageRecord> LoadLanguageRecords()
    {
        var languages = new List<LanguageRecord>();

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT LanguageID, LanguageCode, LanguageName, NativeName, Region, Source, IsPriority, IsActive
            FROM tblLanguage
            ORDER BY IsActive DESC, IsPriority DESC, LanguageName;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            languages.Add(ReadLanguage(dr));
        }

        return languages;
    }

    private static int TryReadInt(string value)
    {
        return int.TryParse(value, out var number) ? number : 0;
    }

    private static bool ReadBool(string value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized is "true" or "yes" or "y" or "1" or "active" or "priority";
    }

    private static void UpsertLanguage(SqlConnection conn, SqlTransaction tx, LanguageRecord record)
    {
        var existingId = record.LanguageID > 0
            ? GetLanguageIdById(conn, tx, record.LanguageID)
            : GetLanguageIdByCode(conn, tx, record.LanguageCode);

        if (existingId > 0)
        {
            using var updateCmd = new SqlCommand(@"
                UPDATE tblLanguage
                SET LanguageCode = @LanguageCode,
                    LanguageName = @LanguageName,
                    NativeName = @NativeName,
                    Region = @Region,
                    Source = @Source,
                    IsPriority = @IsPriority,
                    IsActive = @IsActive,
                    ModifiedOn = GETDATE()
                WHERE LanguageID = @LanguageID;", conn, tx);
            AddSaveParameters(updateCmd, existingId, record.LanguageCode, record.LanguageName, record.NativeName, record.Region, record.Source, record.IsPriority, record.IsActive);
            updateCmd.ExecuteNonQuery();
            return;
        }

        using var insertCmd = new SqlCommand(@"
            INSERT INTO tblLanguage
                (LanguageCode, LanguageName, NativeName, Region, Source, IsPriority, IsActive)
            VALUES
                (@LanguageCode, @LanguageName, @NativeName, @Region, @Source, @IsPriority, @IsActive);", conn, tx);
        AddSaveParameters(insertCmd, 0, record.LanguageCode, record.LanguageName, record.NativeName, record.Region, record.Source, record.IsPriority, record.IsActive);
        insertCmd.ExecuteNonQuery();
    }

    private static int GetLanguageIdById(SqlConnection conn, SqlTransaction tx, int languageID)
    {
        using var cmd = new SqlCommand("SELECT TOP 1 LanguageID FROM tblLanguage WHERE LanguageID = @LanguageID;", conn, tx);
        cmd.Parameters.AddWithValue("@LanguageID", languageID);
        var result = cmd.ExecuteScalar();
        return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }

    private static int GetLanguageIdByCode(SqlConnection conn, SqlTransaction tx, string languageCode)
    {
        using var cmd = new SqlCommand("SELECT TOP 1 LanguageID FROM tblLanguage WHERE LanguageCode = @LanguageCode;", conn, tx);
        cmd.Parameters.AddWithValue("@LanguageCode", languageCode.Trim());
        var result = cmd.ExecuteScalar();
        return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }

    private static LanguageRecord ReadLanguage(SqlDataReader dr)
    {
        return new LanguageRecord
        {
            LanguageID = Convert.ToInt32(dr["LanguageID"]),
            LanguageCode = dr["LanguageCode"].ToString() ?? "",
            LanguageName = dr["LanguageName"].ToString() ?? "",
            NativeName = dr["NativeName"].ToString() ?? "",
            Region = dr["Region"].ToString() ?? "",
            Source = dr["Source"].ToString() ?? "",
            IsPriority = Convert.ToBoolean(dr["IsPriority"]),
            IsActive = Convert.ToBoolean(dr["IsActive"])
        };
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;

        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType = TempData["AlertType"]?.ToString() ?? "success";
    }
}
