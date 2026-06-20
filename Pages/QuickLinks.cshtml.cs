using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class QuickLinkItem
{
    public string Title       { get; set; } = "";
    public string Description { get; set; } = "";
    public string Url         { get; set; } = "";
    public string Category    { get; set; } = "";
    public string Icon        { get; set; } = "";
    public bool   External    { get; set; }
}

public class QuickLinksModel : PageModel
{
    private readonly string _conn;

    public QuickLinksModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public string PageTitle => "Quick Links";
    public List<QuickLinkItem> HrProcessLinks { get; set; } = new();
    public List<QuickLinkItem> SoftwareLinks  { get; set; } = new();

    public void OnGet()
    {
        HrProcessLinks = new List<QuickLinkItem>
        {
            new() { Title = "Internal Employee Directory", Description = "Search, view, and export the internal employee directory", Url = "/EmployeeReport",     Category = "HR Process", Icon = "ED" },
            new() { Title = "Expense Process",           Description = "Submit and manage employee expense claims",                Url = "/ExpenseMaster",      Category = "HR Process", Icon = "EP" },
            new() { Title = "Employee Performance",      Description = "Record and review employee performance cycles",            Url = "/PerformanceMaster",  Category = "HR Process", Icon = "PF" },
            new() { Title = "Recruitment Process",       Description = "Manage hiring and recruitment workflows",                  Url = "/RecruitmentMaster",  Category = "HR Process", Icon = "RC" },
            new() { Title = "Employee Training",         Description = "Track employee training and development",                  Url = "/TrainingMaster",     Category = "HR Process", Icon = "TR" },
        };

        LoadSoftwareLinks();
    }

    private void LoadSoftwareLinks()
    {
        SoftwareLinks.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT SoftwareName, SoftwareUrl, Category, Description
            FROM tblSoftwareLink
            WHERE IsActive = 1
            ORDER BY SortOrder, SoftwareName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var name = dr["SoftwareName"].ToString() ?? "";
            SoftwareLinks.Add(new QuickLinkItem
            {
                Title       = name,
                Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
                Url         = dr["SoftwareUrl"].ToString() ?? "",
                Category    = dr["Category"] == DBNull.Value ? "Software" : dr["Category"].ToString() ?? "Software",
                Icon        = name.Length >= 2 ? name[..2].ToUpperInvariant() : name.ToUpperInvariant(),
                External    = true
            });
        }
    }
}
