using Microsoft.Data.SqlClient;

namespace HRMS.Services;

public class AuthService
{
    private readonly string _conn;
    private readonly IHttpContextAccessor _http;

    public const string SessionUserId   = "UserId";
    public const string SessionUsername = "Username";
    public const string SessionFullName = "FullName";
    public const string SessionIsAdmin  = "IsAdmin";

    public AuthService(IConfiguration config, IHttpContextAccessor http)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _http = http;
    }

    public int? CurrentUserId
    {
        get
        {
            var ctx = _http.HttpContext;
            if (ctx?.Session.GetInt32(SessionUserId) is int id && id > 0) return id;
            return null;
        }
    }

    public string? CurrentUsername => _http.HttpContext?.Session.GetString(SessionUsername);

    public bool IsAdmin => _http.HttpContext?.Session.GetInt32(SessionIsAdmin) == 1;

    public bool IsLoggedIn => CurrentUserId.HasValue;

    public (bool Success, string Message) Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, "Username and password are required.");

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT UserID, Username, FullName, PasswordHash, IsActive, IsAdmin
            FROM   tblUser
            WHERE  Username = @Username;", conn);
        cmd.Parameters.AddWithValue("@Username", username.Trim());
        conn.Open();

        using var dr = cmd.ExecuteReader();
        if (!dr.Read())
            return (false, "Invalid username or password.");

        if (!Convert.ToBoolean(dr["IsActive"]))
            return (false, "This user account is inactive.");

        var hash = dr["PasswordHash"].ToString() ?? "";
        if (!PasswordHelper.VerifyPassword(password, hash))
            return (false, "Invalid username or password.");

        var ctx = _http.HttpContext;
        if (ctx == null) return (false, "Session unavailable.");

        var userId   = Convert.ToInt32(dr["UserID"]);
        var fullName = dr["FullName"].ToString() ?? username;
        var isAdmin  = Convert.ToBoolean(dr["IsAdmin"]);

        ctx.Session.SetInt32(SessionUserId, userId);
        ctx.Session.SetString(SessionUsername, dr["Username"].ToString()!);
        ctx.Session.SetString(SessionFullName, fullName);
        ctx.Session.SetInt32(SessionIsAdmin, isAdmin ? 1 : 0);

        return (true, "Login successful.");
    }

    public void Logout()
    {
        _http.HttpContext?.Session.Clear();
    }
}
