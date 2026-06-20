using Microsoft.Data.SqlClient;

namespace HRMS.Services;

public class MemorandumItem
{
    public int MemorandumID { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int? DepartmentID { get; set; }
    public string DepartmentName { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime ValidTillDate { get; set; }
    public bool IsActive { get; set; }
    public string DocumentPath { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public bool HasDocument => !string.IsNullOrWhiteSpace(DocumentPath);
}

public class MemorandumService
{
    private readonly string _conn;

    public MemorandumService(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public List<MemorandumItem> GetActiveMemorandums()
    {
        var list = new List<MemorandumItem>();

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT m.MemorandumID, m.MemorandumName, m.Description,
                   m.DepartmentID, d.DepartmentName,
                   m.StartDate, m.ValidTillDate, m.IsActive,
                   m.DocumentPath, m.OriginalFileName
            FROM tblMemorandum m
            LEFT JOIN tblDepartment d ON d.DepartmentID = m.DepartmentID
            WHERE m.IsActive = 1
              AND m.StartDate <= CAST(GETDATE() AS DATE)
              AND m.ValidTillDate >= CAST(GETDATE() AS DATE)
            ORDER BY m.StartDate DESC, m.MemorandumID DESC;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            list.Add(ReadItem(dr));

        return list;
    }

    public MemorandumItem? GetById(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT m.MemorandumID, m.MemorandumName, m.Description,
                   m.DepartmentID, d.DepartmentName,
                   m.StartDate, m.ValidTillDate, m.IsActive,
                   m.DocumentPath, m.OriginalFileName
            FROM tblMemorandum m
            LEFT JOIN tblDepartment d ON d.DepartmentID = m.DepartmentID
            WHERE m.MemorandumID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        return dr.Read() ? ReadItem(dr) : null;
    }

    private static MemorandumItem ReadItem(SqlDataReader dr) => new()
    {
        MemorandumID = Convert.ToInt32(dr["MemorandumID"]),
        Name = dr["MemorandumName"].ToString() ?? "",
        Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
        DepartmentID = dr["DepartmentID"] == DBNull.Value ? null : Convert.ToInt32(dr["DepartmentID"]),
        DepartmentName = dr["DepartmentName"] == DBNull.Value ? "All Departments" : dr["DepartmentName"].ToString() ?? "",
        StartDate = Convert.ToDateTime(dr["StartDate"]),
        ValidTillDate = Convert.ToDateTime(dr["ValidTillDate"]),
        IsActive = Convert.ToBoolean(dr["IsActive"]),
        DocumentPath = dr["DocumentPath"] == DBNull.Value ? "" : dr["DocumentPath"].ToString() ?? "",
        OriginalFileName = dr["OriginalFileName"] == DBNull.Value ? "" : dr["OriginalFileName"].ToString() ?? ""
    };
}
