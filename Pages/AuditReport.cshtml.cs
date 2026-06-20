using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class AuditReportModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    public AuditReportModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    public string PageTitle => "Audit Log Report";

    public List<AuditLogRow> Records { get; set; } = new();
    public List<string> Users { get; set; } = new();
    public List<string> ActionTypes { get; set; } = new();
    public List<string> EntityTypes { get; set; } = new();

    [BindProperty(SupportsGet = true)] public DateTime? DateFrom { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? DateTo { get; set; }
    [BindProperty(SupportsGet = true)] public string FilterUser { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string FilterAction { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string FilterEntity { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string SearchText { get; set; } = "";
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public int PageSize { get; } = 50;
    public int TotalRecords { get; set; }
    public int TotalPages => TotalRecords == 0 ? 1 : (int)Math.Ceiling(TotalRecords / (double)PageSize);

    public IActionResult OnGet()
    {
        if (!_auth.IsAdmin)
            return RedirectToPage("/EmployeeMaster", new { accessDenied = 1 });

        if (!DateFrom.HasValue)
            DateFrom = DateTime.Today.AddDays(-30);
        if (!DateTo.HasValue)
            DateTo = DateTime.Today;

        LoadFilterOptions();
        LoadRecords();
        return Page();
    }

    private void LoadFilterOptions()
    {
        using var conn = new SqlConnection(_conn);
        conn.Open();

        using (var cmd = new SqlCommand(@"
            SELECT DISTINCT Username FROM tblAuditLog
            WHERE Username IS NOT NULL AND Username <> ''
            ORDER BY Username;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
                Users.Add(dr["Username"].ToString()!);
        }

        using (var cmd = new SqlCommand(@"
            SELECT DISTINCT ActionType FROM tblAuditLog ORDER BY ActionType;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
                ActionTypes.Add(dr["ActionType"].ToString()!);
        }

        using (var cmd = new SqlCommand(@"
            SELECT DISTINCT EntityType FROM tblAuditLog
            WHERE EntityType IS NOT NULL AND EntityType <> ''
            ORDER BY EntityType;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
                EntityTypes.Add(dr["EntityType"].ToString()!);
        }
    }

    private void LoadRecords()
    {
        var where = new List<string> { "ActionAt >= @DateFrom", "ActionAt < DATEADD(day, 1, @DateTo)" };
        if (!string.IsNullOrWhiteSpace(FilterUser)) where.Add("Username = @FilterUser");
        if (!string.IsNullOrWhiteSpace(FilterAction)) where.Add("ActionType = @FilterAction");
        if (!string.IsNullOrWhiteSpace(FilterEntity)) where.Add("EntityType = @FilterEntity");
        if (!string.IsNullOrWhiteSpace(SearchText))
            where.Add("(EntityName LIKE @Search OR Details LIKE @Search OR PagePath LIKE @Search OR FormKey LIKE @Search)");

        var whereSql = string.Join(" AND ", where);
        var offset = (Math.Max(1, PageNumber) - 1) * PageSize;

        using var conn = new SqlConnection(_conn);
        conn.Open();

        using (var countCmd = new SqlCommand($"SELECT COUNT(*) FROM tblAuditLog WHERE {whereSql};", conn))
        {
            AddFilterParams(countCmd);
            TotalRecords = Convert.ToInt32(countCmd.ExecuteScalar());
        }

        using var cmd = new SqlCommand($@"
            SELECT AuditLogID, ActionAt, Username, ActionType, EntityType, EntityID, EntityName,
                   FormKey, PagePath, HandlerName, Details, IpAddress, Success
            FROM tblAuditLog
            WHERE {whereSql}
            ORDER BY ActionAt DESC, AuditLogID DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;", conn);

        AddFilterParams(cmd);
        cmd.Parameters.AddWithValue("@Offset", offset);
        cmd.Parameters.AddWithValue("@PageSize", PageSize);

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Records.Add(new AuditLogRow
            {
                Id = Convert.ToInt64(dr["AuditLogID"]),
                ActionAt = Convert.ToDateTime(dr["ActionAt"]),
                Username = dr["Username"]?.ToString() ?? "",
                ActionType = dr["ActionType"].ToString() ?? "",
                EntityType = dr["EntityType"]?.ToString() ?? "",
                EntityId = dr["EntityID"] == DBNull.Value ? null : Convert.ToInt32(dr["EntityID"]),
                EntityName = dr["EntityName"]?.ToString() ?? "",
                FormKey = dr["FormKey"]?.ToString() ?? "",
                PagePath = dr["PagePath"]?.ToString() ?? "",
                HandlerName = dr["HandlerName"]?.ToString() ?? "",
                Details = dr["Details"]?.ToString() ?? "",
                IpAddress = dr["IpAddress"]?.ToString() ?? "",
                Success = Convert.ToBoolean(dr["Success"])
            });
        }
    }

    private void AddFilterParams(SqlCommand cmd)
    {
        cmd.Parameters.AddWithValue("@DateFrom", DateFrom!.Value.Date);
        cmd.Parameters.AddWithValue("@DateTo", DateTo!.Value.Date);
        cmd.Parameters.AddWithValue("@FilterUser", string.IsNullOrWhiteSpace(FilterUser) ? DBNull.Value : FilterUser);
        cmd.Parameters.AddWithValue("@FilterAction", string.IsNullOrWhiteSpace(FilterAction) ? DBNull.Value : FilterAction);
        cmd.Parameters.AddWithValue("@FilterEntity", string.IsNullOrWhiteSpace(FilterEntity) ? DBNull.Value : FilterEntity);
        cmd.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(SearchText) ? DBNull.Value : $"%{SearchText.Trim()}%");
    }
}

public class AuditLogRow
{
    public long Id { get; set; }
    public DateTime ActionAt { get; set; }
    public string Username { get; set; } = "";
    public string ActionType { get; set; } = "";
    public string EntityType { get; set; } = "";
    public int? EntityId { get; set; }
    public string EntityName { get; set; } = "";
    public string FormKey { get; set; } = "";
    public string PagePath { get; set; } = "";
    public string HandlerName { get; set; } = "";
    public string Details { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public bool Success { get; set; }
}
