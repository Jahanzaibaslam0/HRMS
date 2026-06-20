using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class UserRecord
{
    public int    UserID    { get; set; }
    public string UserCode  { get; set; } = "";
    public string Username  { get; set; } = "";
    public string FullName  { get; set; } = "";
    public string Email     { get; set; } = "";
    public bool   IsActive  { get; set; } = true;
    public bool   IsAdmin   { get; set; }
}

public class UserSetupModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;
    private readonly PermissionService _perms;

    public UserSetupModel(IConfiguration config, AuthService auth, PermissionService perms)
    {
        _conn  = config.GetConnectionString("HRMSConnection")!;
        _auth  = auth;
        _perms = perms;
    }

    public string PageTitle => "User Setup";

    public List<UserRecord> Users        { get; set; } = new();
    public UserRecord       Input        { get; set; } = new() { IsActive = true };
    public string           NewPassword  { get; set; } = "";
    public string           AlertMessage { get; set; } = "";
    public string           AlertType    { get; set; } = "success";

    public IActionResult OnGet(int? editId)
    {
        var denied = EnsureCanManage();
        if (denied != null) return denied;

        LoadAlert();
        LoadUsers();

        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);
        else
            Input.UserCode = GenerateNextCode();

        return Page();
    }

    public IActionResult OnPostSave(
        int userId, string userCode, string username, string fullName,
        string email, string newPassword, bool isActive, bool isAdmin)
    {
        var denied = EnsureCanManage();
        if (denied != null) return denied;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName))
        {
            TempData["Alert"]     = "Username and Full Name are required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = userId > 0 ? userId : (int?)null });
        }

        if (userId <= 0 && string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["Alert"]     = "Password is required for new users.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (userId > 0)
            {
                var sql = @"
                    UPDATE tblUser SET
                        UserCode   = @Code,
                        Username   = @Username,
                        FullName   = @FullName,
                        Email      = @Email,
                        IsActive   = @IsActive,
                        IsAdmin    = @IsAdmin,
                        ModifiedOn = GETDATE()
                    WHERE UserID = @Id;";

                if (!string.IsNullOrWhiteSpace(newPassword))
                    sql = sql.Replace("ModifiedOn = GETDATE()", "PasswordHash = @Hash, ModifiedOn = GETDATE()");

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id",       userId);
                cmd.Parameters.AddWithValue("@Code",     string.IsNullOrWhiteSpace(userCode) ? DBNull.Value : (object)userCode.Trim());
                cmd.Parameters.AddWithValue("@Username", username.Trim());
                cmd.Parameters.AddWithValue("@FullName", fullName.Trim());
                cmd.Parameters.AddWithValue("@Email",    string.IsNullOrWhiteSpace(email) ? DBNull.Value : (object)email.Trim());
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                cmd.Parameters.AddWithValue("@IsAdmin",  isAdmin);
                if (!string.IsNullOrWhiteSpace(newPassword))
                    cmd.Parameters.AddWithValue("@Hash", PasswordHelper.HashPassword(newPassword));
                cmd.ExecuteNonQuery();

                TempData["Alert"] = "User updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblUser (UserCode, Username, PasswordHash, FullName, Email, IsActive, IsAdmin)
                    VALUES (@Code, @Username, @Hash, @FullName, @Email, @IsActive, @IsAdmin);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
                cmd.Parameters.AddWithValue("@Code",     string.IsNullOrWhiteSpace(userCode) ? DBNull.Value : (object)userCode.Trim());
                cmd.Parameters.AddWithValue("@Username", username.Trim());
                cmd.Parameters.AddWithValue("@Hash",     PasswordHelper.HashPassword(newPassword));
                cmd.Parameters.AddWithValue("@FullName", fullName.Trim());
                cmd.Parameters.AddWithValue("@Email",    string.IsNullOrWhiteSpace(email) ? DBNull.Value : (object)email.Trim());
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                cmd.Parameters.AddWithValue("@IsAdmin",  isAdmin);
                var newId = Convert.ToInt32(cmd.ExecuteScalar());

                AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                TempData["Alert"] = "User created successfully. Assign rights from Manage Rights.";
                TempData["AlertType"] = "success";
                return RedirectToPage("/UserRightsSetup", new { userId = newId });
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"]     = "Username already exists.";
            TempData["AlertType"] = "error";
        }
        catch (Exception ex)
        {
            TempData["Alert"]     = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage(new { editId = userId > 0 ? userId : (int?)null });
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        var denied = EnsureCanManage();
        if (denied != null) return denied;

        if (_auth.CurrentUserId == deleteId)
        {
            TempData["Alert"]     = "You cannot delete your own account.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            using (var delP = new SqlCommand("DELETE FROM tblUserPermission WHERE UserID = @Id;", conn, tx))
            {
                delP.Parameters.AddWithValue("@Id", deleteId);
                delP.ExecuteNonQuery();
            }
            using (var delU = new SqlCommand(@"
                UPDATE tblUser SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID WHERE UserID = @Id;", conn, tx))
            {
                delU.Parameters.AddWithValue("@Id", deleteId);
                AuditHelper.AddModifiedBy(delU, _auth.CurrentUserId);
                delU.ExecuteNonQuery();
            }

            tx.Commit();
            TempData["Alert"]     = "User deactivated.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"]     = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private IActionResult? EnsureCanManage()
    {
        if (_auth.IsAdmin || _perms.CanWrite("UserSetup")) return null;
        TempData["Alert"]     = "You do not have permission to manage users.";
        TempData["AlertType"] = "error";
        return RedirectToPage("/EmployeeMaster");
    }

    private void LoadUsers()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd  = new SqlCommand(@"
            SELECT UserID, UserCode, Username, FullName, Email, IsActive, IsAdmin
            FROM   tblUser
            ORDER BY IsActive DESC, FullName;", conn);
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

    private void LoadForEdit(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd  = new SqlCommand(@"
            SELECT UserID, UserCode, Username, FullName, Email, IsActive, IsAdmin
            FROM   tblUser WHERE UserID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            Input = new UserRecord
            {
                UserID   = id,
                UserCode = dr["UserCode"] == DBNull.Value ? "" : dr["UserCode"].ToString()!,
                Username = dr["Username"].ToString()!,
                FullName = dr["FullName"].ToString()!,
                Email    = dr["Email"] == DBNull.Value ? "" : dr["Email"].ToString()!,
                IsActive = Convert.ToBoolean(dr["IsActive"]),
                IsAdmin  = Convert.ToBoolean(dr["IsAdmin"])
            };
        }
    }

    private string GenerateNextCode()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd  = new SqlCommand(@"
            SELECT TOP 1 UserCode FROM tblUser
            WHERE UserCode LIKE 'GB-US-%' ORDER BY UserCode DESC;", conn);
        conn.Open();
        var last = cmd.ExecuteScalar()?.ToString();
        if (!string.IsNullOrEmpty(last) && last.Length >= 9 && int.TryParse(last[6..], out int num))
            return $"GB-US-{(num + 1):D5}";
        return "GB-US-00001";
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType    = TempData["AlertType"]?.ToString() ?? "success";
    }
}
