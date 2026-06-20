using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class WorkerLocationRecord
{
    public int    WorkerLocationID          { get; set; }
    public int    EmployeeID                { get; set; }
    public string EmployeeCode              { get; set; } = "";
    public string EmployeeName              { get; set; } = "";
    public int    PrimaryLocationID         { get; set; }
    public string PrimaryLocationName       { get; set; } = "";
    public int    SecondaryLocationID       { get; set; }
    public string SecondaryLocationName     { get; set; } = "";
    public int    WorkLocationTypeID        { get; set; }
    public string WorkLocationTypeName      { get; set; } = "";
    public int    WorkArrangementID         { get; set; }
    public string WorkArrangementName       { get; set; } = "";
    public string HybridSchedule            { get; set; } = "";
    public string TerritoryRegionAssignment { get; set; } = "";
    public string ClientSiteAccess          { get; set; } = "";
    public bool   IsActive                  { get; set; } = true;
}

public class WorkerLocationSetupModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    public WorkerLocationSetupModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    public string PageTitle => "Worker Location Setup";

    public WorkerLocationRecord Input   { get; set; } = new() { IsActive = true };
    public List<WorkerLocationRecord> Records { get; set; } = new();
    public List<LookupItem> Employees         { get; set; } = new();
    public List<LookupItem> Locations         { get; set; } = new();
    public List<LookupItem> WorkLocationTypes { get; set; } = new();
    public List<LookupItem> WorkArrangements  { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType    { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        LoadEmployees();
        LoadLocations();
        LoadWorkLocationTypes();
        LoadWorkArrangements();

        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);

        LoadRecords();
    }

    public IActionResult OnPostSave(
        int workerLocationID, int employeeID,
        int primaryLocationID, int secondaryLocationID,
        int workLocationTypeID, int workArrangementID,
        string hybridSchedule, string territoryRegionAssignment, string clientSiteAccess,
        bool isActive)
    {
        if (employeeID <= 0)
        {
            TempData["Alert"] = "Employee is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = workerLocationID > 0 ? workerLocationID : (int?)null });
        }

        if (primaryLocationID <= 0)
        {
            TempData["Alert"] = "Primary Work Location is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = workerLocationID > 0 ? workerLocationID : (int?)null });
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (workerLocationID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblWorkerLocation SET
                        EmployeeID                = @EmployeeID,
                        PrimaryLocationID         = @PrimaryLocationID,
                        SecondaryLocationID       = @SecondaryLocationID,
                        WorkLocationTypeID        = @WorkLocationTypeID,
                        WorkArrangementID         = @WorkArrangementID,
                        HybridSchedule            = @HybridSchedule,
                        TerritoryRegionAssignment = @TerritoryRegionAssignment,
                        ClientSiteAccess          = @ClientSiteAccess,
                        IsActive                  = @IsActive,
                        ModifiedOn                = GETDATE(),
                        ModifiedByUserID          = @ModifiedByUserID
                    WHERE WorkerLocationID = @WorkerLocationID;", conn);
                AddParams(cmd, workerLocationID, employeeID, primaryLocationID, secondaryLocationID,
                    workLocationTypeID, workArrangementID, hybridSchedule,
                    territoryRegionAssignment, clientSiteAccess, isActive);
                AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Worker location updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblWorkerLocation
                        (EmployeeID, PrimaryLocationID, SecondaryLocationID,
                         WorkLocationTypeID, WorkArrangementID,
                         HybridSchedule, TerritoryRegionAssignment, ClientSiteAccess, IsActive, CreatedOn, CreatedByUserID)
                    VALUES
                        (@EmployeeID, @PrimaryLocationID, @SecondaryLocationID,
                         @WorkLocationTypeID, @WorkArrangementID,
                         @HybridSchedule, @TerritoryRegionAssignment, @ClientSiteAccess, @IsActive, GETDATE(), @CreatedByUserID);", conn);
                AddParams(cmd, 0, employeeID, primaryLocationID, secondaryLocationID,
                    workLocationTypeID, workArrangementID, hybridSchedule,
                    territoryRegionAssignment, clientSiteAccess, isActive);
                AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Worker location added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "A worker location record already exists for this employee.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = workerLocationID > 0 ? workerLocationID : (int?)null });
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = workerLocationID > 0 ? workerLocationID : (int?)null });
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                UPDATE tblWorkerLocation SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                WHERE WorkerLocationID = @ID;", conn);
            cmd.Parameters.AddWithValue("@ID", deleteId);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Worker location deactivated successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private static void AddParams(
        SqlCommand cmd, int workerLocationID, int employeeID,
        int primaryLocationID, int secondaryLocationID,
        int workLocationTypeID, int workArrangementID,
        string hybridSchedule, string territoryRegionAssignment, string clientSiteAccess,
        bool isActive)
    {
        static object Fk(int id) => id <= 0 ? DBNull.Value : (object)id;
        static object Str(string? s) => string.IsNullOrWhiteSpace(s) ? DBNull.Value : (object)s.Trim();

        if (workerLocationID > 0)
            cmd.Parameters.AddWithValue("@WorkerLocationID", workerLocationID);

        cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
        cmd.Parameters.AddWithValue("@PrimaryLocationID", primaryLocationID);
        cmd.Parameters.AddWithValue("@SecondaryLocationID", Fk(secondaryLocationID));
        cmd.Parameters.AddWithValue("@WorkLocationTypeID", Fk(workLocationTypeID));
        cmd.Parameters.AddWithValue("@WorkArrangementID", Fk(workArrangementID));
        cmd.Parameters.AddWithValue("@HybridSchedule", Str(hybridSchedule));
        cmd.Parameters.AddWithValue("@TerritoryRegionAssignment", Str(territoryRegionAssignment));
        cmd.Parameters.AddWithValue("@ClientSiteAccess", Str(clientSiteAccess));
        cmd.Parameters.AddWithValue("@IsActive", isActive);
    }

    private void LoadEmployees()
    {
        Employees.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT EmployeeID, EmployeeCode, FirstName + ' ' + LastName AS FullName
            FROM tblEmployee
            WHERE Status = 'Active'
            ORDER BY FirstName, LastName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var code = dr["EmployeeCode"].ToString() ?? "";
            var name = dr["FullName"].ToString() ?? "";
            Employees.Add(new LookupItem
            {
                Id   = Convert.ToInt32(dr["EmployeeID"]),
                Name = $"{code} – {name}"
            });
        }
    }

    private void LoadLocations()
    {
        Locations.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT LocationID, LocationName FROM tblLocation
            WHERE IsActive = 1 ORDER BY LocationName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Locations.Add(new LookupItem
            {
                Id   = Convert.ToInt32(dr["LocationID"]),
                Name = dr["LocationName"].ToString() ?? ""
            });
        }
    }

    private void LoadWorkLocationTypes()
    {
        WorkLocationTypes.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT WorkLocationTypeID, WorkLocationTypeName FROM tblWorkLocationType
            WHERE IsActive = 1 ORDER BY WorkLocationTypeName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            WorkLocationTypes.Add(new LookupItem
            {
                Id   = Convert.ToInt32(dr["WorkLocationTypeID"]),
                Name = dr["WorkLocationTypeName"].ToString() ?? ""
            });
        }
    }

    private void LoadWorkArrangements()
    {
        WorkArrangements.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT WorkArrangementID, WorkArrangementName FROM tblWorkArrangement
            WHERE IsActive = 1 ORDER BY WorkArrangementName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            WorkArrangements.Add(new LookupItem
            {
                Id   = Convert.ToInt32(dr["WorkArrangementID"]),
                Name = dr["WorkArrangementName"].ToString() ?? ""
            });
        }
    }

    private void LoadForEdit(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT WorkerLocationID, EmployeeID, PrimaryLocationID, SecondaryLocationID,
                   WorkLocationTypeID, WorkArrangementID,
                   HybridSchedule, TerritoryRegionAssignment, ClientSiteAccess, IsActive
            FROM tblWorkerLocation WHERE WorkerLocationID = @ID;", conn);
        cmd.Parameters.AddWithValue("@ID", id);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            static int IntOrZero(object v) => v == DBNull.Value ? 0 : Convert.ToInt32(v);
            static string Str(object v) => v == DBNull.Value ? "" : v.ToString() ?? "";

            Input = new WorkerLocationRecord
            {
                WorkerLocationID          = id,
                EmployeeID                = IntOrZero(dr["EmployeeID"]),
                PrimaryLocationID         = IntOrZero(dr["PrimaryLocationID"]),
                SecondaryLocationID       = IntOrZero(dr["SecondaryLocationID"]),
                WorkLocationTypeID        = IntOrZero(dr["WorkLocationTypeID"]),
                WorkArrangementID         = IntOrZero(dr["WorkArrangementID"]),
                HybridSchedule            = Str(dr["HybridSchedule"]),
                TerritoryRegionAssignment = Str(dr["TerritoryRegionAssignment"]),
                ClientSiteAccess          = Str(dr["ClientSiteAccess"]),
                IsActive                  = Convert.ToBoolean(dr["IsActive"])
            };
        }
    }

    private void LoadRecords()
    {
        Records.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT wl.WorkerLocationID, wl.EmployeeID,
                   e.EmployeeCode, e.FirstName + ' ' + e.LastName AS EmployeeName,
                   wl.PrimaryLocationID, pl.LocationName AS PrimaryLocationName,
                   wl.SecondaryLocationID, sl.LocationName AS SecondaryLocationName,
                   wl.WorkLocationTypeID, wlt.WorkLocationTypeName,
                   wl.WorkArrangementID, wa.WorkArrangementName,
                   wl.HybridSchedule, wl.TerritoryRegionAssignment, wl.ClientSiteAccess,
                   wl.IsActive
            FROM tblWorkerLocation wl
            INNER JOIN tblEmployee e ON e.EmployeeID = wl.EmployeeID
            LEFT JOIN tblLocation pl ON pl.LocationID = wl.PrimaryLocationID
            LEFT JOIN tblLocation sl ON sl.LocationID = wl.SecondaryLocationID
            LEFT JOIN tblWorkLocationType wlt ON wlt.WorkLocationTypeID = wl.WorkLocationTypeID
            LEFT JOIN tblWorkArrangement wa ON wa.WorkArrangementID = wl.WorkArrangementID
            ORDER BY wl.IsActive DESC, e.FirstName, e.LastName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            static int IntOrZero(object v) => v == DBNull.Value ? 0 : Convert.ToInt32(v);
            static string Str(object v) => v == DBNull.Value ? "" : v.ToString() ?? "";

            Records.Add(new WorkerLocationRecord
            {
                WorkerLocationID          = Convert.ToInt32(dr["WorkerLocationID"]),
                EmployeeID                = IntOrZero(dr["EmployeeID"]),
                EmployeeCode              = Str(dr["EmployeeCode"]),
                EmployeeName              = Str(dr["EmployeeName"]),
                PrimaryLocationID         = IntOrZero(dr["PrimaryLocationID"]),
                PrimaryLocationName       = Str(dr["PrimaryLocationName"]),
                SecondaryLocationID       = IntOrZero(dr["SecondaryLocationID"]),
                SecondaryLocationName     = Str(dr["SecondaryLocationName"]),
                WorkLocationTypeID        = IntOrZero(dr["WorkLocationTypeID"]),
                WorkLocationTypeName      = Str(dr["WorkLocationTypeName"]),
                WorkArrangementID         = IntOrZero(dr["WorkArrangementID"]),
                WorkArrangementName       = Str(dr["WorkArrangementName"]),
                HybridSchedule            = Str(dr["HybridSchedule"]),
                TerritoryRegionAssignment = Str(dr["TerritoryRegionAssignment"]),
                ClientSiteAccess          = Str(dr["ClientSiteAccess"]),
                IsActive                  = Convert.ToBoolean(dr["IsActive"])
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
