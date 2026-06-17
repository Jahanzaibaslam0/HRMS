using System.Text.Json;
using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class UserRightsSetupModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;
    private readonly PermissionService _perms;

    public UserRightsSetupModel(IConfiguration config, AuthService auth, PermissionService perms)
    {
        _conn  = config.GetConnectionString("HRMSConnection")!;
        _auth  = auth;
        _perms = perms;
    }

    public string PageTitle => "User Rights Setup";

    public List<UserRecord> Users           { get; set; } = new();
    public List<FormPermission> Permissions   { get; set; } = new();
    public int    SelectedUserId            { get; set; }
    public string SelectedUserName          { get; set; } = "";
    public string SelectedFullName          { get; set; } = "";
    public bool   SelectedIsAdmin           { get; set; }
    public string AlertMessage              { get; set; } = "";
    public string AlertType                 { get; set; } = "success";

    public IActionResult OnGet(int? userId)
    {
        var denied = EnsureCanManage();
        if (denied != null) return denied;

        LoadAlert();
        LoadUsers();

        if (userId.HasValue && userId > 0)
        {
            SelectedUserId = userId.Value;
            var user = Users.FirstOrDefault(u => u.UserID == userId.Value);
            if (user != null)
            {
                SelectedUserName = user.Username;
                SelectedFullName = user.FullName;
                SelectedIsAdmin  = user.IsAdmin;
            }
            Permissions = _perms.GetUserPermissions(userId.Value);
        }

        return Page();
    }

    public IActionResult OnPostSave(int userId, string permissionsJson)
    {
        var denied = EnsureCanManage();
        if (denied != null) return denied;

        if (userId <= 0)
        {
            TempData["Alert"]     = "Please select a user.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            var list = DeserializePermissions(permissionsJson);
            _perms.SaveUserPermissions(userId, list);
            TempData["Alert"]     = "User rights saved successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"]     = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage(new { userId });
    }

    private List<FormPermission> DeserializePermissions(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();

        var rows = JsonSerializer.Deserialize<List<PermissionRow>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

        var map = AppForms.All.ToDictionary(f => f.Key, f => f, StringComparer.OrdinalIgnoreCase);

        return rows
            .Where(r => map.ContainsKey(r.FormKey))
            .Select(r => new FormPermission
            {
                FormKey   = r.FormKey,
                FormName  = map[r.FormKey].Name,
                Category  = map[r.FormKey].Category,
                CanRead   = r.CanRead,
                CanWrite  = r.CanWrite,
                CanDelete = r.CanDelete
            })
            .ToList();
    }

    private class PermissionRow
    {
        public string FormKey   { get; set; } = "";
        public bool   CanRead   { get; set; }
        public bool   CanWrite  { get; set; }
        public bool   CanDelete { get; set; }
    }

    private IActionResult? EnsureCanManage()
    {
        if (_auth.IsAdmin || _perms.CanWrite("UserRightsSetup")) return null;
        TempData["Alert"]     = "You do not have permission to manage user rights.";
        TempData["AlertType"] = "error";
        return RedirectToPage("/EmployeeMaster");
    }

    private void LoadUsers()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd  = new SqlCommand(@"
            SELECT UserID, UserCode, Username, FullName, Email, IsActive, IsAdmin
            FROM   tblUser
            WHERE  IsActive = 1
            ORDER BY FullName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Users.Add(new UserRecord
            {
                UserID   = Convert.ToInt32(dr["UserID"]),
                UserCode = dr["UserCode"] == DBNull.Value ? "" : dr["UserCode"].ToString()!,
                Username = dr["Username"].ToString()!,
                FullName = dr["FullName"].ToString()!,
                Email    = dr["Email"] == DBNull.Value ? "" : dr["Email"].ToString()!,
                IsActive = Convert.ToBoolean(dr["IsActive"]),
                IsAdmin  = Convert.ToBoolean(dr["IsAdmin"])
            });
        }
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType    = TempData["AlertType"]?.ToString() ?? "success";
    }
}
