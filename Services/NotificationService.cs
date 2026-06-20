using Microsoft.Data.SqlClient;

namespace HRMS.Services;

public class NotificationItem
{
    public int NotificationID { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int? DepartmentID { get; set; }
    public string DepartmentName { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime ValidTillDate { get; set; }
    public bool IsActive { get; set; }
}

public class NotificationService
{
    private readonly string _conn;

    public NotificationService(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public List<NotificationItem> GetActiveNotifications()
    {
        var list = new List<NotificationItem>();

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT n.NotificationID, n.NotificationName, n.Description,
                   n.DepartmentID, d.DepartmentName,
                   n.StartDate, n.ValidTillDate, n.IsActive
            FROM tblNotification n
            LEFT JOIN tblDepartment d ON d.DepartmentID = n.DepartmentID
            WHERE n.IsActive = 1
              AND n.StartDate <= CAST(GETDATE() AS DATE)
              AND n.ValidTillDate >= CAST(GETDATE() AS DATE)
            ORDER BY n.StartDate DESC, n.NotificationID DESC;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            list.Add(ReadItem(dr));

        return list;
    }

    public NotificationItem? GetById(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT n.NotificationID, n.NotificationName, n.Description,
                   n.DepartmentID, d.DepartmentName,
                   n.StartDate, n.ValidTillDate, n.IsActive
            FROM tblNotification n
            LEFT JOIN tblDepartment d ON d.DepartmentID = n.DepartmentID
            WHERE n.NotificationID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        return dr.Read() ? ReadItem(dr) : null;
    }

    private static NotificationItem ReadItem(SqlDataReader dr) => new()
    {
        NotificationID = Convert.ToInt32(dr["NotificationID"]),
        Name = dr["NotificationName"].ToString() ?? "",
        Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
        DepartmentID = dr["DepartmentID"] == DBNull.Value ? null : Convert.ToInt32(dr["DepartmentID"]),
        DepartmentName = dr["DepartmentName"] == DBNull.Value ? "All Departments" : dr["DepartmentName"].ToString() ?? "",
        StartDate = Convert.ToDateTime(dr["StartDate"]),
        ValidTillDate = Convert.ToDateTime(dr["ValidTillDate"]),
        IsActive = Convert.ToBoolean(dr["IsActive"])
    };
}
