using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class LookupRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string AliasName { get; set; } = "";
    public bool IsActive { get; set; }
}

public abstract class LookupSetupPageModel : PageModel
{
    private readonly string _conn;

    protected LookupSetupPageModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    protected abstract string TableName { get; }
    protected abstract string IdColumn { get; }
    protected abstract string NameColumn { get; }
    protected virtual string? AliasColumn => null;

    public abstract string PageTitle { get; }
    public abstract string ItemLabel { get; }
    public abstract string PagePath { get; }

    public List<LookupRecord> Records { get; set; } = new();
    public LookupRecord Input { get; set; } = new() { IsActive = true };
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        if (editId.HasValue && editId > 0)
        {
            LoadForEdit(editId.Value);
        }

        LoadRecords();
    }

    public IActionResult OnPostSave(int itemId, string itemName, string aliasName, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            TempData["Alert"] = $"{ItemLabel} is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (itemId > 0)
            {
                using var cmd = new SqlCommand($@"
                    UPDATE {TableName}
                    SET {NameColumn} = @Name,
                        {AliasUpdateSql}
                        IsActive = @IsActive,
                        ModifiedOn = GETDATE()
                    WHERE {IdColumn} = @Id;", conn);
                cmd.Parameters.AddWithValue("@Id", itemId);
                cmd.Parameters.AddWithValue("@Name", itemName.Trim());
                AddAliasParameter(cmd, aliasName);
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                cmd.ExecuteNonQuery();

                TempData["Alert"] = $"{ItemLabel} updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand($@"
                    INSERT INTO {TableName} ({NameColumn}{AliasInsertColumns}, IsActive)
                    VALUES (@Name{AliasInsertValues}, @IsActive);", conn);
                cmd.Parameters.AddWithValue("@Name", itemName.Trim());
                AddAliasParameter(cmd, aliasName);
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                cmd.ExecuteNonQuery();

                TempData["Alert"] = $"{ItemLabel} added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = $"{ItemLabel} already exists.";
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
            conn.Open();
            using var cmd = new SqlCommand($@"
                UPDATE {TableName}
                SET IsActive = 0,
                    ModifiedOn = GETDATE()
                WHERE {IdColumn} = @Id;", conn);
            cmd.Parameters.AddWithValue("@Id", deleteId);
            cmd.ExecuteNonQuery();

            TempData["Alert"] = $"{ItemLabel} removed successfully.";
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
        using var cmd = new SqlCommand($@"
            SELECT {IdColumn}, {NameColumn}{AliasSelectSql}, IsActive
            FROM {TableName}
            WHERE {IdColumn} = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            Input = new LookupRecord
            {
                Id = Convert.ToInt32(dr[IdColumn]),
                Name = dr[NameColumn].ToString() ?? "",
                AliasName = ReadAlias(dr),
                IsActive = Convert.ToBoolean(dr["IsActive"])
            };
        }
    }

    private void LoadRecords()
    {
        Records.Clear();

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT {IdColumn}, {NameColumn}{AliasSelectSql}, IsActive
            FROM {TableName}
            ORDER BY IsActive DESC, {NameColumn};", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Records.Add(new LookupRecord
            {
                Id = Convert.ToInt32(dr[IdColumn]),
                Name = dr[NameColumn].ToString() ?? "",
                AliasName = ReadAlias(dr),
                IsActive = Convert.ToBoolean(dr["IsActive"])
            });
        }
    }

    private string AliasSelectSql => AliasColumn == null ? "" : $", {AliasColumn}";
    private string AliasUpdateSql => AliasColumn == null ? "" : $"{AliasColumn} = @AliasName,";
    private string AliasInsertColumns => AliasColumn == null ? "" : $", {AliasColumn}";
    private string AliasInsertValues => AliasColumn == null ? "" : ", @AliasName";

    private void AddAliasParameter(SqlCommand cmd, string aliasName)
    {
        if (AliasColumn == null) return;

        cmd.Parameters.AddWithValue("@AliasName", string.IsNullOrWhiteSpace(aliasName) ? DBNull.Value : aliasName.Trim());
    }

    private string ReadAlias(SqlDataReader dr)
    {
        return AliasColumn == null || dr[AliasColumn] == DBNull.Value ? "" : dr[AliasColumn]?.ToString() ?? "";
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;

        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType = TempData["AlertType"]?.ToString() ?? "success";
    }
}
