using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class EmployeeReportRow
{
    public string Extension    { get; set; } = "";
    public string CellNo       { get; set; } = "";
    public string Email        { get; set; } = "";
    public string PowerID      { get; set; } = "";
    public string UserID       { get; set; } = "";
    public string Title        { get; set; } = "";
    public string EmployeeCode { get; set; } = "";
    public string PositionCode { get; set; } = "";
    public string Position     { get; set; } = "";
    public string TitleGrade   { get; set; } = "";
    public string Department   { get; set; } = "";
    public string Division     { get; set; } = "";
    public string Region       { get; set; } = "";
    public string Team         { get; set; } = "";
    public string Location     { get; set; } = "";
    public string SalesGroup   { get; set; } = "";
    public string State        { get; set; } = "";
    public string BasedCity    { get; set; } = "";
}

public class EmployeeReportModel : PageModel
{
    private readonly string _conn;

    public EmployeeReportModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public string PageTitle => "Internal Employee Directory";
    public List<EmployeeReportRow> Records { get; set; } = new();
    public string Search { get; set; } = "";

    public void OnGet(string? search)
    {
        Search = search?.Trim() ?? "";
        Records = LoadRecords(Search);
    }

    public IActionResult OnGetDownloadExcel(string? search)
    {
        var records = LoadRecords(search?.Trim() ?? "");

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Internal Employee Directory");
        var headers = new[]
        {
            "Extension", "Cell No", "Email", "Power BI ID", "User ID", "Title",
            "Employee Code", "Position Code", "Position", "Title Grade",
            "Department", "Division", "Region", "Team", "Location",
            "Sales Group", "State", "Based City"
        };

        for (var col = 0; col < headers.Length; col++)
        {
            worksheet.Cell(1, col + 1).Value = headers[col];
            worksheet.Cell(1, col + 1).Style.Font.Bold = true;
        }

        for (var row = 0; row < records.Count; row++)
        {
            var r = records[row];
            var excelRow = row + 2;
            worksheet.Cell(excelRow, 1).Value  = r.Extension;
            worksheet.Cell(excelRow, 2).Value  = r.CellNo;
            worksheet.Cell(excelRow, 3).Value  = r.Email;
            worksheet.Cell(excelRow, 4).Value  = r.PowerID;
            worksheet.Cell(excelRow, 5).Value  = r.UserID;
            worksheet.Cell(excelRow, 6).Value  = r.Title;
            worksheet.Cell(excelRow, 7).Value  = r.EmployeeCode;
            worksheet.Cell(excelRow, 8).Value  = r.PositionCode;
            worksheet.Cell(excelRow, 9).Value  = r.Position;
            worksheet.Cell(excelRow, 10).Value = r.TitleGrade;
            worksheet.Cell(excelRow, 11).Value = r.Department;
            worksheet.Cell(excelRow, 12).Value = r.Division;
            worksheet.Cell(excelRow, 13).Value = r.Region;
            worksheet.Cell(excelRow, 14).Value = r.Team;
            worksheet.Cell(excelRow, 15).Value = r.Location;
            worksheet.Cell(excelRow, 16).Value = r.SalesGroup;
            worksheet.Cell(excelRow, 17).Value = r.State;
            worksheet.Cell(excelRow, 18).Value = r.BasedCity;
        }

        worksheet.Columns().AdjustToContents();
        worksheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"InternalEmployeeDirectory_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    private List<EmployeeReportRow> LoadRecords(string search)
    {
        var records = new List<EmployeeReportRow>();
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var searchParam = hasSearch ? $"%{search.Trim()}%" : null;

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT
                ISNULL(ext.ExtensionName, '')                         AS Extension,
                ISNULL(
                    NULLIF(LTRIM(RTRIM(cCell.ContactValue)), ''),
                    ISNULL(NULLIF(LTRIM(RTRIM(e.OfficialMobile)), ''),
                           ISNULL(NULLIF(LTRIM(RTRIM(e.PersonalMobile)), ''),
                                  ISNULL(e.Phone, ''))))              AS CellNo,
                ISNULL(cEmail.ContactValue, ISNULL(e.OfficialEmail, ISNULL(e.Email, ''))) AS Email,
                ISNULL(cPower.ContactValue, '')                       AS PowerID,
                ISNULL(u.UserCode, ISNULL(u.Username, ''))            AS UserID,
                LTRIM(RTRIM(e.FirstName + ' ' + e.LastName))          AS Title,
                e.EmployeeCode,
                ISNULL(j.JobCode, '')                                 AS PositionCode,
                e.Designation                                         AS Position,
                ISNULL(g.GradeName, '')                               AS TitleGrade,
                d.DepartmentName                                      AS Department,
                ISNULL(div.DivisionName, '')                           AS Division,
                ISNULL(r.RegionName, '')                              AS Region,
                ISNULL(st.SalesTeamName, '')                          AS Team,
                ISNULL(loc.LocationName, '')                           AS Location,
                ISNULL(sg.SalesGroupName, '')                         AS SalesGroup,
                ISNULL(p.ProvinceName, '')                              AS State,
                ISNULL(city.CityName, '')                               AS BasedCity
            FROM tblEmployee e
            INNER JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
            LEFT JOIN tblDivision div ON div.DivisionID = e.DivisionID
            LEFT JOIN tblRegion r ON r.RegionID = e.RegionID
            LEFT JOIN tblSalesTeam st ON st.SalesTeamID = e.SalesTeamID
            LEFT JOIN tblLocation loc ON loc.LocationID = e.LocationID
            LEFT JOIN tblSalesGroup sg ON sg.SalesGroupID = e.SalesGroupID
            LEFT JOIN tblProvince p ON p.ProvinceID = e.ProvinceID
            LEFT JOIN tblCity city ON city.CityID = e.CityID
            LEFT JOIN tblGrade g ON g.GradeID = e.GradeID
            LEFT JOIN tblJob j ON j.JobID = e.JobID
            LEFT JOIN tblUser u ON u.UserID = e.UserID
            LEFT JOIN tblExtension ext ON ext.ExtensionID = e.ExtensionID
            OUTER APPLY (
                SELECT TOP 1 ContactValue
                FROM tblEmployeeContact
                WHERE EmployeeID = e.EmployeeID
                  AND ContactType IN ('OfficialMobile', 'PersonalMobile', 'WhatsApp')
                  AND NULLIF(LTRIM(RTRIM(ContactValue)), '') IS NOT NULL
                ORDER BY
                    CASE
                        WHEN ContactType = 'OfficialMobile' AND IsPrimary = 1 THEN 0
                        WHEN ContactType = 'OfficialMobile' THEN 1
                        WHEN IsPrimary = 1 THEN 2
                        WHEN ContactType = 'PersonalMobile' THEN 3
                        ELSE 4
                    END,
                    ContactID DESC
            ) cCell
            OUTER APPLY (
                SELECT TOP 1 ContactValue
                FROM tblEmployeeContact
                WHERE EmployeeID = e.EmployeeID AND ContactType = 'OfficialEmail'
                  AND NULLIF(LTRIM(RTRIM(ContactValue)), '') IS NOT NULL
                ORDER BY IsPrimary DESC, ContactID DESC
            ) cEmail
            OUTER APPLY (
                SELECT TOP 1 ContactValue
                FROM tblEmployeeContact
                WHERE EmployeeID = e.EmployeeID
                  AND ContactType IN ('PowerBI ID', 'PowerID', 'Power BI ID', 'Power BI')
                  AND NULLIF(LTRIM(RTRIM(ContactValue)), '') IS NOT NULL
                ORDER BY IsPrimary DESC, ContactID DESC
            ) cPower
            WHERE (@Search IS NULL OR
                   e.EmployeeCode LIKE @Search OR
                   e.FirstName LIKE @Search OR
                   e.LastName LIKE @Search OR
                   e.Designation LIKE @Search OR
                   d.DepartmentName LIKE @Search OR
                   ISNULL(div.DivisionName, '') LIKE @Search OR
                   ISNULL(r.RegionName, '') LIKE @Search OR
                   ISNULL(st.SalesTeamName, '') LIKE @Search OR
                   ISNULL(loc.LocationName, '') LIKE @Search OR
                   ISNULL(sg.SalesGroupName, '') LIKE @Search OR
                   ISNULL(p.ProvinceName, '') LIKE @Search OR
                   ISNULL(city.CityName, '') LIKE @Search OR
                   ISNULL(g.GradeName, '') LIKE @Search OR
                   ISNULL(j.JobCode, '') LIKE @Search OR
                   ISNULL(u.UserCode, '') LIKE @Search OR
                   ISNULL(u.Username, '') LIKE @Search OR
                   ISNULL(cCell.ContactValue, '') LIKE @Search OR
                   ISNULL(cEmail.ContactValue, '') LIKE @Search OR
                   ISNULL(cPower.ContactValue, '') LIKE @Search OR
                   ISNULL(ext.ExtensionName, '') LIKE @Search OR
                   ISNULL(ext.ExtensionCode, '') LIKE @Search)
            ORDER BY e.EmployeeCode;", conn);

        cmd.Parameters.AddWithValue("@Search", (object?)searchParam ?? DBNull.Value);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            records.Add(new EmployeeReportRow
            {
                Extension    = dr["Extension"].ToString() ?? "",
                CellNo       = dr["CellNo"].ToString() ?? "",
                Email        = dr["Email"].ToString() ?? "",
                PowerID      = dr["PowerID"].ToString() ?? "",
                UserID       = dr["UserID"].ToString() ?? "",
                Title        = dr["Title"].ToString() ?? "",
                EmployeeCode = dr["EmployeeCode"].ToString() ?? "",
                PositionCode = dr["PositionCode"].ToString() ?? "",
                Position     = dr["Position"].ToString() ?? "",
                TitleGrade   = dr["TitleGrade"].ToString() ?? "",
                Department   = dr["Department"].ToString() ?? "",
                Division     = dr["Division"].ToString() ?? "",
                Region       = dr["Region"].ToString() ?? "",
                Team         = dr["Team"].ToString() ?? "",
                Location     = dr["Location"].ToString() ?? "",
                SalesGroup   = dr["SalesGroup"].ToString() ?? "",
                State        = dr["State"].ToString() ?? "",
                BasedCity    = dr["BasedCity"].ToString() ?? ""
            });
        }

        return records;
    }
}
