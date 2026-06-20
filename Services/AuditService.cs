using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;

namespace HRMS.Services;

public class AuditService
{
    private readonly string _conn;
    private readonly AuthService _auth;
    private readonly IHttpContextAccessor _http;

    private static readonly string[] SensitiveKeys =
    {
        "password", "passwordhash", "__requestverificationtoken", "antiforgery"
    };

    private static readonly string[] IdKeys =
    {
        "deleteId", "itemId", "id", "employeeId", "EmployeeID", "ExpenseID", "expenseId",
        "PerformanceID", "performanceId", "RecruitmentID", "recruitmentId",
        "EmployeeTrainingID", "trainingId", "benefitEntitlementID", "entitlementId",
        "detailID", "UserID", "userId", "manageEntitlementID"
    };

    private static readonly string[] NameKeys =
    {
        "itemName", "employeeName", "EmployeeName", "fullName", "FullName", "username",
        "Username", "SoftwareName", "benefitName", "departmentName", "jobTitle"
    };

    public AuditService(IConfiguration config, AuthService auth, IHttpContextAccessor http)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
        _http = http;
    }

    public void Log(
        string actionType,
        string? entityType = null,
        int? entityId = null,
        string? entityName = null,
        string? formKey = null,
        string? pagePath = null,
        string? handlerName = null,
        string? details = null,
        int? userId = null,
        string? username = null,
        bool success = true)
    {
        try
        {
            var ctx = _http.HttpContext;
            var ip = ctx?.Connection.RemoteIpAddress?.ToString();

            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                INSERT INTO tblAuditLog
                    (ActionAt, UserID, Username, FormKey, PagePath, HandlerName,
                     ActionType, EntityType, EntityID, EntityName, Details, IpAddress, Success)
                VALUES
                    (GETDATE(), @UserID, @Username, @FormKey, @PagePath, @HandlerName,
                     @ActionType, @EntityType, @EntityID, @EntityName, @Details, @IpAddress, @Success);", conn);

            cmd.Parameters.AddWithValue("@UserID", (object?)userId ?? (object?)_auth.CurrentUserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Username", (object?)username ?? (object?)_auth.CurrentUsername ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FormKey", (object?)formKey ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PagePath", (object?)pagePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HandlerName", (object?)handlerName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ActionType", actionType);
            cmd.Parameters.AddWithValue("@EntityType", (object?)entityType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EntityID", (object?)entityId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EntityName", Truncate(entityName, 250) ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Details", Truncate(details, 3800) ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IpAddress", (object?)ip ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Success", success);

            conn.Open();
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // Audit failures must not break business operations
        }
    }

    public void LogLogin(string username, bool success, int? userId = null, string? message = null)
    {
        Log(
            actionType: success ? "Login" : "LoginFailed",
            entityType: "User",
            entityId: userId,
            entityName: username,
            formKey: "Login",
            pagePath: "/Login",
            handlerName: "OnPost",
            details: message,
            userId: userId,
            username: username,
            success: success);
    }

    public void LogLogout()
    {
        Log(
            actionType: "Logout",
            entityType: "User",
            entityId: _auth.CurrentUserId,
            entityName: _auth.CurrentUsername,
            formKey: "Login",
            pagePath: "/Login",
            handlerName: "OnGetLogout",
            userId: _auth.CurrentUserId,
            username: _auth.CurrentUsername);
    }

    public void LogFromPageHandler(PageHandlerExecutedContext context)
    {
        var request = context.HttpContext.Request;
        if (!HttpMethods.IsPost(request.Method)) return;

        var handlerMethod = context.HandlerMethod?.MethodInfo.Name ?? "";
        if (!handlerMethod.StartsWith("OnPost", StringComparison.OrdinalIgnoreCase)) return;
        if (context.Exception != null) return;

        var path = request.Path.Value ?? "";

        if (context.HandlerInstance is HRMS.Pages.LoginModel ||
            path.Equals("/Login", StringComparison.OrdinalIgnoreCase))
        {
            if (context.Result is not Microsoft.AspNetCore.Mvc.RedirectResult) return;
            LogLogin(_auth.CurrentUsername ?? "", true, _auth.CurrentUserId);
            return;
        }

        var formDef = AppForms.FindByPath(path);
        var handlerQuery = request.Query["handler"].ToString();
        var displayHandler = string.IsNullOrEmpty(handlerQuery) ? handlerMethod : $"OnPost{handlerQuery}";
        var form = request.HasFormContentType ? request.Form : null;

        var actionType = InferAction(handlerMethod, handlerQuery, form);
        var entityId = ExtractEntityId(form);
        var entityName = ExtractEntityName(form);
        var entityType = formDef?.Name ?? context.HandlerInstance?.GetType().Name.Replace("Model", "") ?? path.Trim('/');

        Log(
            actionType: actionType,
            entityType: entityType,
            entityId: entityId,
            entityName: entityName,
            formKey: formDef?.Key,
            pagePath: path,
            handlerName: displayHandler,
            details: BuildDetailsJson(form));
    }

    private static string InferAction(string handlerMethod, string handlerQuery, IFormCollection? form)
    {
        var token = (handlerQuery + handlerMethod).ToLowerInvariant();

        if (token.Contains("delete") || token.Contains("unlink")) return "Delete";
        if (token.Contains("upload")) return "Create";

        if (token.Contains("save") || handlerMethod.Equals("OnPost", StringComparison.OrdinalIgnoreCase))
        {
            if (ExtractEntityId(form) is > 0) return "Update";
            return "Create";
        }

        if (token.Contains("link")) return "Create";
        return "Update";
    }

    private static int? ExtractEntityId(IFormCollection? form)
    {
        if (form == null) return null;

        foreach (var key in IdKeys)
        {
            if (!form.TryGetValue(key, out var val)) continue;
            var text = val.ToString();
            if (int.TryParse(text, out var id) && id > 0) return id;
        }

        return null;
    }

    private static string? ExtractEntityName(IFormCollection? form)
    {
        if (form == null) return null;

        foreach (var key in NameKeys)
        {
            if (!form.TryGetValue(key, out var val)) continue;
            var text = val.ToString().Trim();
            if (!string.IsNullOrEmpty(text)) return text;
        }

        return null;
    }

    private static string? BuildDetailsJson(IFormCollection? form)
    {
        if (form == null || form.Count == 0) return null;

        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in form)
        {
            if (IsSensitive(field.Key)) continue;
            if (field.Key.StartsWith("__", StringComparison.Ordinal)) continue;

            var value = field.Value.ToString();
            if (string.IsNullOrWhiteSpace(value)) continue;

            if (field.Key.Contains("file", StringComparison.OrdinalIgnoreCase) ||
                value.Length > 500)
            {
                data[field.Key] = "[omitted]";
                continue;
            }

            data[field.Key] = value.Length > 200 ? value[..200] + "…" : value;
        }

        return data.Count == 0 ? null : JsonSerializer.Serialize(data);
    }

    private static bool IsSensitive(string key)
        => SensitiveKeys.Any(s => key.Contains(s, StringComparison.OrdinalIgnoreCase));

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return value.Length <= max ? value : value[..max];
    }
}
