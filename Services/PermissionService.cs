using Microsoft.Data.SqlClient;

namespace HRMS.Services;

public class FormPermission
{
    public string FormKey   { get; set; } = "";
    public string FormName  { get; set; } = "";
    public string Category  { get; set; } = "";
    public bool   CanRead   { get; set; }
    public bool   CanWrite  { get; set; }
    public bool   CanDelete { get; set; }
}

public class PermissionService
{
    private readonly string _conn;
    private readonly AuthService _auth;

    public PermissionService(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    public bool CanRead(string formKey)
        => _auth.IsAdmin || HasRight(formKey, p => p.CanRead);

    public bool CanWrite(string formKey)
        => _auth.IsAdmin || HasRight(formKey, p => p.CanWrite);

    public bool CanDelete(string formKey)
        => _auth.IsAdmin || HasRight(formKey, p => p.CanDelete);

    public bool CanAccessPage(string pagePath)
    {
        if (_auth.IsAdmin) return true;
        var form = AppForms.FindByPath(pagePath);
        if (form == null) return true; // unlisted pages allowed if logged in
        return CanRead(form.Key);
    }

    private bool HasRight(string formKey, Func<FormPermission, bool> selector)
    {
        if (!_auth.CurrentUserId.HasValue) return false;
        var perms = GetUserPermissions(_auth.CurrentUserId.Value);
        var p = perms.FirstOrDefault(x => x.FormKey.Equals(formKey, StringComparison.OrdinalIgnoreCase));
        return p != null && selector(p);
    }

    public List<FormPermission> GetUserPermissions(int userId)
    {
        var map = AppForms.All.ToDictionary(
            f => f.Key,
            f => new FormPermission
            {
                FormKey  = f.Key,
                FormName = f.Name,
                Category = f.Category
            },
            StringComparer.OrdinalIgnoreCase);

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT FormKey, CanRead, CanWrite, CanDelete
            FROM   tblUserPermission
            WHERE  UserID = @UserID;", conn);
        cmd.Parameters.AddWithValue("@UserID", userId);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var key = dr["FormKey"].ToString() ?? "";
            if (!map.TryGetValue(key, out var perm)) continue;
            perm.CanRead   = Convert.ToBoolean(dr["CanRead"]);
            perm.CanWrite  = Convert.ToBoolean(dr["CanWrite"]);
            perm.CanDelete = Convert.ToBoolean(dr["CanDelete"]);
        }

        return AppForms.All
            .Select(f => map[f.Key])
            .OrderBy(p => AppForms.All.First(f => f.Key == p.FormKey).SortOrder)
            .ToList();
    }

    public void SaveUserPermissions(int userId, IEnumerable<FormPermission> permissions)
    {
        using var conn = new SqlConnection(_conn);
        conn.Open();
        using var tx = conn.BeginTransaction();

        using (var del = new SqlCommand("DELETE FROM tblUserPermission WHERE UserID = @UserID;", conn, tx))
        {
            del.Parameters.AddWithValue("@UserID", userId);
            del.ExecuteNonQuery();
        }

        foreach (var p in permissions.Where(x => x.CanRead || x.CanWrite || x.CanDelete))
        {
            using var ins = new SqlCommand(@"
                INSERT INTO tblUserPermission (UserID, FormKey, CanRead, CanWrite, CanDelete)
                VALUES (@UserID, @FormKey, @CanRead, @CanWrite, @CanDelete);", conn, tx);
            ins.Parameters.AddWithValue("@UserID",    userId);
            ins.Parameters.AddWithValue("@FormKey",     p.FormKey);
            ins.Parameters.AddWithValue("@CanRead",    p.CanRead);
            ins.Parameters.AddWithValue("@CanWrite",   p.CanWrite);
            ins.Parameters.AddWithValue("@CanDelete", p.CanDelete);
            ins.ExecuteNonQuery();
        }

        tx.Commit();
    }
}
