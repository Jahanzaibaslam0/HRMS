using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace HRMS.Pages;

// -------------------------------------------------------
// Data models
// -------------------------------------------------------
public class EmployeeViewModel
{
    public int       EmployeeID       { get; set; }
    public string    EmployeeCode     { get; set; } = "";
    public string    FullName         { get; set; } = "";
    public string    DepartmentName   { get; set; } = "";
    public string    LegalEntityName  { get; set; } = "";
    public string    EmploymentType   { get; set; } = "";
    public string    EmploymentStatus { get; set; } = "";
    public string    Designation      { get; set; } = "";
    public string    Phone            { get; set; } = "";
    public string    Email            { get; set; } = "";
    public DateTime? DateOfJoining    { get; set; }
    public decimal   BasicSalary      { get; set; }
    public string    Status           { get; set; } = "Active";
}

public class DepartmentItem
{
    public int    DepartmentID   { get; set; }
    public string DepartmentName { get; set; } = "";
}

public class LookupItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class EmployeeInput
{
    public int    EmployeeID          { get; set; }
    public string EmployeeCode        { get; set; } = "";
    public string FirstName           { get; set; } = "";
    public string LastName            { get; set; } = "";
    public string FathersHusbandsName { get; set; } = "";
    public string DisplayName         { get; set; } = "";
    public string NationalIDNumber    { get; set; } = "";
    public string Gender              { get; set; } = "";
    public int    GenderID            { get; set; }
    public string DateOfBirth         { get; set; } = "";
    public string MaritalStatus       { get; set; } = "";
    public int    DepartmentID        { get; set; }
    public int    DivisionID          { get; set; }
    public int    NationalityID       { get; set; }
    public int    ReligionID          { get; set; }
    public int    LanguageID          { get; set; }
    public int    WorkerCategoryID    { get; set; }
    public int    EmploymentTypeID    { get; set; }
    public int    EmploymentStatusID  { get; set; }
    public int    WorkforceSegmentID  { get; set; }
    public int    LegalEntityID       { get; set; }
    public int    BusinessUnitID      { get; set; }
    public int    SalesTeamID         { get; set; }
    public int    CostCenterID        { get; set; }
    public int    RegionID            { get; set; }
    public int    LocationID          { get; set; }
    public int    JobID               { get; set; }
    public int    WorkerLocationID    { get; set; }
    public int    CityID              { get; set; }
    public int    ProvinceID          { get; set; }
    public int    SalesGroupID        { get; set; }
    public int    GradeID             { get; set; }
    public int    ExtensionID         { get; set; }
    public string Domicile            { get; set; } = "";
    public int    BloodGroupID        { get; set; }
    public int    BenefitEntitlementID{ get; set; }
    public int    UserID              { get; set; }
    public string Designation         { get; set; } = "";
    public string DateOfJoining       { get; set; } = "";
    public string EmploymentStartDate { get; set; } = "";
    public string ProbationPeriodDays { get; set; } = "";
    public string ProbationEndDate    { get; set; } = "";
    public string ConfirmationDate    { get; set; } = "";
    public string BasicSalary         { get; set; } = "";
    public string Status              { get; set; } = "Active";

    public string TotalTenureDisplay =>
        EmployeeMasterModel.FormatTenure(ParseDate(DateOfJoining));

    public string CurrentRoleTenureDisplay =>
        EmployeeMasterModel.FormatTenure(ParseDate(
            !string.IsNullOrWhiteSpace(EmploymentStartDate) ? EmploymentStartDate : DateOfJoining));

    public string AgeDisplay => EmployeeMasterModel.FormatAge(ParseDate(DateOfBirth));

    private static DateTime? ParseDate(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : DateTime.Parse(value);
}

public class EmployeeContactInput
{
    public string ContactType { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string Relationship { get; set; } = "";
    public string ContactValue { get; set; } = "";
    public bool IsPrimary { get; set; }
}

public class EmployeeAddressInput
{
    public string AddressType { get; set; } = "";
    public string AddressLine { get; set; } = "";
    public string City { get; set; } = "";
    public string ProvinceState { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public bool IsPrimary { get; set; }
}

public class EmployeeFamilyMemberInput
{
    public string MemberName { get; set; } = "";
    public string Relationship { get; set; } = "";
    public string Gender { get; set; } = "";
    public string DateOfBirth { get; set; } = "";
    public string ContactNumber { get; set; } = "";
    public bool IsDependent { get; set; }
}

public class EmployeeBankInput
{
    public int BankID { get; set; }
    public string BranchCode { get; set; } = "";
    public string BranchName { get; set; } = "";
    public string AccountTitle { get; set; } = "";
    public string IBAN { get; set; } = "";
    public string SwiftBICCode { get; set; } = "";
    public string AccountType { get; set; } = "";
    public string AccountVerificationStatus { get; set; } = "Pending";
    public bool IsPrimary { get; set; }
}

public class EmployeeEducationInput
{
    public string HighestQualification { get; set; } = "";
    public string DegreeCertificate { get; set; } = "";
    public string Specialization { get; set; } = "";
    public string Institution { get; set; } = "";
    public string YearOfPassing { get; set; } = "";
    public string GradeCGPA { get; set; } = "";
}

public class EmployeeCertificateInput
{
    public string CertificationName { get; set; } = "";
    public string CertificationBody { get; set; } = "";
    public string CertificateNumber { get; set; } = "";
    public string IssueDate { get; set; } = "";
    public string ExpiryDate { get; set; } = "";
    public bool RenewalRequired { get; set; }
    public string CertificateCopyPath { get; set; } = "";
}

public class EmployeeDocumentInput
{
    public int DocumentTypeID { get; set; }
    public string DocumentTypeName { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string IssueDate { get; set; } = "";
    public string ExpiryDate { get; set; } = "";
    public string Remarks { get; set; } = "";
    public string DocumentPath { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string VerificationStatus { get; set; } = "Pending";
}

// -------------------------------------------------------
// Page Model
// -------------------------------------------------------
public class EmployeeMasterModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;
    private readonly IWebHostEnvironment _env;

    public EmployeeMasterModel(IConfiguration config, AuthService auth, IWebHostEnvironment env)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
        _env = env;
    }

    // Bound properties
    public List<EmployeeViewModel> Employees          { get; set; } = new();
    public List<DepartmentItem>    Departments        { get; set; } = new();
    public List<LookupItem>        Genders            { get; set; } = new();
    public List<LookupItem>        Nationalities      { get; set; } = new();
    public List<LookupItem>        Religions          { get; set; } = new();
    public List<LookupItem>        Languages          { get; set; } = new();
    public List<LookupItem>        WorkerCategories   { get; set; } = new();
    public List<LookupItem>        EmploymentTypes    { get; set; } = new();
    public List<LookupItem>        EmploymentStatuses { get; set; } = new();
    public List<LookupItem>        WorkforceSegments  { get; set; } = new();
    public List<LookupItem>        LegalEntities      { get; set; } = new();
    public List<LookupItem>        BusinessUnits      { get; set; } = new();
    public List<LookupItem>        Divisions          { get; set; } = new();
    public List<LookupItem>        SalesTeams         { get; set; } = new();
    public List<LookupItem>        CostCenters        { get; set; } = new();
    public List<LookupItem>        Regions            { get; set; } = new();
    public List<LookupItem>        Locations          { get; set; } = new();
    public List<LookupItem>        Jobs               { get; set; } = new();
    public List<LookupItem>        WorkerLocations    { get; set; } = new();
    public List<LookupItem>        Cities             { get; set; } = new();
    public List<LookupItem>        Provinces          { get; set; } = new();
    public List<LookupItem>        SalesGroups        { get; set; } = new();
    public List<LookupItem>        Grades             { get; set; } = new();
    public List<LookupItem>        Extensions         { get; set; } = new();
    public List<LookupItem>        BloodGroups        { get; set; } = new();
    public List<LookupItem>        BenefitEntitlements{ get; set; } = new();
    public List<LookupItem>        Users              { get; set; } = new();
    public List<LookupItem>        Banks              { get; set; } = new();
    public EmployeeInput           Input        { get; set; } = new();
    public List<EmployeeContactInput> ContactRecords { get; set; } = new();
    public List<EmployeeAddressInput> AddressRecords { get; set; } = new();
    public List<EmployeeFamilyMemberInput> FamilyRecords { get; set; } = new();
    public List<EmployeeBankInput> BankRecords { get; set; } = new();
    public List<EmployeeEducationInput> EducationRecords { get; set; } = new();
    public List<EmployeeCertificateInput> CertificateRecords { get; set; } = new();
    public List<EmployeeDocumentInput> DocumentRecords { get; set; } = new();
    public List<LookupItem> DocumentTypes { get; set; } = new();
    public bool                    EditMode     { get; set; } = false;
    public bool                    ShowForm     { get; set; } = false;
    public string                  AlertMessage { get; set; } = "";
    public string                  AlertType    { get; set; } = "success";

    // -------------------------------------------------------
    // GET – list view OR form view
    // -------------------------------------------------------
    public void OnGet([FromQuery] int? editId, [FromQuery] bool? newEmployee)
    {
        LoadAlert();

        ShowForm = (editId.HasValue && editId > 0) || (newEmployee == true);

        if (ShowForm)
        {
            LoadDepartments();
            LoadLookupLists();
            EnsureDefaultRows();

            if (editId.HasValue && editId > 0)
            {
                LoadForEdit(editId.Value);
                EditMode = true;
            }
        }
        else
        {
            LoadEmployees();
        }
    }

    // -------------------------------------------------------
    // POST – Save (Insert / Update)
    // -------------------------------------------------------
    public IActionResult OnPost(
        int    EmployeeID,
        bool   EditMode,
        string EmployeeCode, string FirstName, string LastName,
        string FathersHusbandsName, string DisplayName, string NationalIDNumber,
        int    GenderID, string DateOfBirth, string MaritalStatus,
        int    DepartmentID, int DivisionID,
        int    NationalityID, int ReligionID, int LanguageID,
        int    WorkerCategoryID, int EmploymentTypeID, int EmploymentStatusID,
        int    WorkforceSegmentID, int LegalEntityID, int BusinessUnitID,
        int    SalesTeamID, int CostCenterID,
        int    RegionID, int LocationID, int JobID, int WorkerLocationID,
        int    CityID, int ProvinceID, int SalesGroupID, int GradeID, int ExtensionID,
        string Domicile,
        string Designation, string DateOfJoining,
        string EmploymentStartDate, string ProbationPeriodDays,
        string ProbationEndDate, string ConfirmationDate,
        string BasicSalary,
        string Status,
        int    BloodGroupID, int BenefitEntitlementID, int UserID,
        string ContactsJson, string AddressesJson, string FamilyMembersJson)
    {
        // Re-populate page state
        this.EditMode = EditMode;
        Input = new EmployeeInput
        {
            EmployeeID          = EmployeeID,
            EmployeeCode        = EmployeeCode?.Trim() ?? "",
            FirstName           = FirstName?.Trim()    ?? "",
            LastName            = LastName?.Trim()     ?? "",
            FathersHusbandsName = FathersHusbandsName?.Trim() ?? "",
            DisplayName         = DisplayName?.Trim()  ?? "",
            NationalIDNumber    = NationalIDNumber?.Trim() ?? "",
            GenderID            = GenderID,
            Gender              = GetLookupName("tblGender", "GenderID", "GenderName", GenderID),
            DateOfBirth         = DateOfBirth          ?? "",
            MaritalStatus       = MaritalStatus        ?? "",
            DepartmentID        = DepartmentID,
            DivisionID          = DivisionID,
            NationalityID       = NationalityID,
            ReligionID          = ReligionID,
            LanguageID          = LanguageID,
            WorkerCategoryID    = WorkerCategoryID,
            EmploymentTypeID    = EmploymentTypeID,
            EmploymentStatusID  = EmploymentStatusID,
            WorkforceSegmentID  = WorkforceSegmentID,
            LegalEntityID       = LegalEntityID,
            BusinessUnitID      = BusinessUnitID,
            SalesTeamID         = SalesTeamID,
            CostCenterID        = CostCenterID,
            RegionID            = RegionID,
            LocationID          = LocationID,
            JobID               = JobID,
            WorkerLocationID    = WorkerLocationID,
            CityID              = CityID,
            ProvinceID          = ProvinceID,
            SalesGroupID        = SalesGroupID,
            GradeID             = GradeID,
            ExtensionID         = ExtensionID,
            Domicile            = Domicile?.Trim() ?? "",
            BloodGroupID        = BloodGroupID,
            BenefitEntitlementID= BenefitEntitlementID,
            UserID              = UserID,
            Designation         = Designation?.Trim()  ?? "",
            DateOfJoining       = DateOfJoining        ?? "",
            EmploymentStartDate = EmploymentStartDate  ?? "",
            ProbationPeriodDays = ProbationPeriodDays  ?? "",
            ProbationEndDate    = ProbationEndDate     ?? "",
            ConfirmationDate    = ConfirmationDate     ?? "",
            BasicSalary         = BasicSalary          ?? "",
            Status              = Status               ?? "Active"
        };

        ApplyTenureCalculations(Input);

        ContactRecords = DeserializeList<EmployeeContactInput>(ContactsJson);
        AddressRecords = DeserializeList<EmployeeAddressInput>(AddressesJson);
        FamilyRecords = DeserializeList<EmployeeFamilyMemberInput>(FamilyMembersJson);

        var existingEmployeeId = GetEmployeeIdByCode(Input.EmployeeCode);
        if (EmployeeID <= 0 && existingEmployeeId > 0)
        {
            EmployeeID = existingEmployeeId;
            Input.EmployeeID = existingEmployeeId;
        }

        EditMode = EmployeeID > 0;
        this.EditMode = EditMode;

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            int employeeId = SaveEmployeeCore(conn, tx, Input);

            tx.Commit();

            // Redirect with success message in TempData
            TempData["Alert"]     = EditMode ? "Employee updated successfully." : "Employee added successfully.";
            TempData["AlertType"] = "success";
            return RedirectToPage(new { editId = employeeId });
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            AlertMessage = "Duplicate Employee ID. Please use a unique employee ID.";
            AlertType    = "error";
        }
        catch (Exception ex)
        {
            AlertMessage = "Error: " + ex.Message;
            AlertType    = "error";
        }

        LoadDepartments();
        LoadLookupLists();
        LoadEmployees();
        EnsureDefaultRows();
        return Page();
    }

    public IActionResult OnPostSaveContacts(int EmployeeID, string EmployeeCode, string ContactsJson)
    {
        var employeeId = ResolveEmployeeId(EmployeeID, EmployeeCode);
        if (employeeId <= 0)
        {
            TempData["Alert"] = "Please save or select an employee before saving contact details.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            ReplaceEmployeeContacts(conn, tx, employeeId, DeserializeList<EmployeeContactInput>(ContactsJson));
            tx.Commit();

            TempData["Alert"] = "Employee contact details saved successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error saving contact details: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage(new { editId = employeeId });
    }

    public IActionResult OnPostSaveAddresses(int EmployeeID, string EmployeeCode, string AddressesJson)
    {
        var employeeId = ResolveEmployeeId(EmployeeID, EmployeeCode);
        if (employeeId <= 0)
        {
            TempData["Alert"] = "Please save or select an employee before saving address details.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            ReplaceEmployeeAddresses(conn, tx, employeeId, DeserializeList<EmployeeAddressInput>(AddressesJson));
            tx.Commit();

            TempData["Alert"] = "Employee address details saved successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error saving address details: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage(new { editId = employeeId });
    }

    public IActionResult OnPostSaveFamilyMembers(int EmployeeID, string EmployeeCode, string FamilyMembersJson)
    {
        var employeeId = ResolveEmployeeId(EmployeeID, EmployeeCode);
        if (employeeId <= 0)
        {
            TempData["Alert"] = "Please save or select an employee before saving family member details.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            ReplaceEmployeeFamilyMembers(conn, tx, employeeId, DeserializeList<EmployeeFamilyMemberInput>(FamilyMembersJson));
            tx.Commit();

            TempData["Alert"] = "Employee family member details saved successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error saving family member details: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage(new { editId = employeeId });
    }

    public IActionResult OnPostSaveBanks(int EmployeeID, string EmployeeCode, string BanksJson)
    {
        var employeeId = ResolveEmployeeId(EmployeeID, EmployeeCode);
        if (employeeId <= 0)
        {
            TempData["Alert"] = "Please save or select an employee before saving bank details.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            ReplaceEmployeeBanks(conn, tx, employeeId, DeserializeList<EmployeeBankInput>(BanksJson));
            tx.Commit();

            TempData["Alert"] = "Employee bank details saved successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error saving bank details: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage(new { editId = employeeId });
    }

    public IActionResult OnPostSaveEducation(int EmployeeID, string EmployeeCode, string EducationJson)
    {
        var employeeId = ResolveEmployeeId(EmployeeID, EmployeeCode);
        if (employeeId <= 0)
        {
            TempData["Alert"] = "Please save or select an employee before saving education details.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            ReplaceEmployeeEducation(conn, tx, employeeId, DeserializeList<EmployeeEducationInput>(EducationJson));
            tx.Commit();

            TempData["Alert"] = "Employee education details saved successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error saving education details: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage(new { editId = employeeId });
    }

    public IActionResult OnPostSaveCertificates(int EmployeeID, string EmployeeCode, string CertificatesJson)
    {
        var employeeId = ResolveEmployeeId(EmployeeID, EmployeeCode);
        if (employeeId <= 0)
        {
            TempData["Alert"] = "Please save or select an employee before saving certificate details.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            ReplaceEmployeeCertificates(conn, tx, employeeId, DeserializeList<EmployeeCertificateInput>(CertificatesJson));
            tx.Commit();

            TempData["Alert"] = "Employee certificate details saved successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error saving certificate details: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage(new { editId = employeeId });
    }

    public IActionResult OnPostSaveDocuments(int EmployeeID, string EmployeeCode, string DocumentsJson)
    {
        var employeeId = ResolveEmployeeId(EmployeeID, EmployeeCode);
        if (employeeId <= 0)
        {
            TempData["Alert"] = "Please save or select an employee before saving document details.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var tx = conn.BeginTransaction();

            ReplaceEmployeeDocuments(conn, tx, employeeId, DeserializeList<EmployeeDocumentInput>(DocumentsJson));
            tx.Commit();

            TempData["Alert"] = "Employee documents saved successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error saving document details: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage(new { editId = employeeId });
    }

    private int ResolveEmployeeId(int employeeID, string employeeCode)
    {
        if (employeeID > 0)
            return employeeID;

        return GetEmployeeIdByCode(employeeCode);
    }

    private int GetEmployeeIdByCode(string employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
            return 0;

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(
            "SELECT TOP 1 EmployeeID FROM tblEmployee WHERE EmployeeCode = @EmployeeCode;",
            conn);
        cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode.Trim());
        conn.Open();

        var result = cmd.ExecuteScalar();
        return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }

    // -------------------------------------------------------
    // POST handler – Delete
    // -------------------------------------------------------
    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd  = new SqlCommand("sp_DeleteEmployee", conn)
                             { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@EmployeeID", deleteId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"]     = "Employee deleted successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"]     = "Error deleting record: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------
    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType    = TempData["AlertType"]?.ToString() ?? "success";
    }

    private void LoadEmployees()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd  = new SqlCommand(@"
            SELECT
                e.EmployeeID,
                e.EmployeeCode,
                e.FirstName + ' ' + e.LastName        AS FullName,
                d.DepartmentName,
                e.Designation,
                ISNULL(et.EmploymentTypeName,   '')   AS EmploymentType,
                ISNULL(es.EmploymentStatusName, '')   AS EmploymentStatus,
                ISNULL(le.LegalEntityName,      '')   AS LegalEntityName,
                ISNULL(cPhone.ContactValue, '')       AS Phone,
                ISNULL(cEmail.ContactValue, '')       AS Email,
                e.DateOfJoining,
                e.BasicSalary,
                e.Status
            FROM tblEmployee e
            INNER JOIN tblDepartment d
                ON d.DepartmentID = e.DepartmentID
            LEFT JOIN tblEmploymentType et
                ON et.EmploymentTypeID = e.EmploymentTypeID
            LEFT JOIN tblEmploymentStatus es
                ON es.EmploymentStatusID = e.EmploymentStatusID
            LEFT JOIN tblLegalEntity le
                ON le.LegalEntityID = e.LegalEntityID
            OUTER APPLY (
                SELECT TOP 1 ContactValue
                FROM tblEmployeeContact
                WHERE EmployeeID = e.EmployeeID AND ContactType = 'PersonalMobile'
                ORDER BY IsPrimary DESC, ContactID DESC
            ) cPhone
            OUTER APPLY (
                SELECT TOP 1 ContactValue
                FROM tblEmployeeContact
                WHERE EmployeeID = e.EmployeeID AND ContactType = 'OfficialEmail'
                ORDER BY IsPrimary DESC, ContactID DESC
            ) cEmail
            ORDER BY e.EmployeeID DESC;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            Employees.Add(new EmployeeViewModel
            {
                EmployeeID       = Convert.ToInt32(dr["EmployeeID"]),
                EmployeeCode     = dr["EmployeeCode"].ToString()!,
                FullName         = dr["FullName"].ToString()!,
                DepartmentName   = dr["DepartmentName"].ToString()!,
                LegalEntityName  = dr["LegalEntityName"].ToString()!,
                EmploymentType   = dr["EmploymentType"].ToString()!,
                EmploymentStatus = dr["EmploymentStatus"].ToString()!,
                Designation      = dr["Designation"].ToString()!,
                Phone            = dr["Phone"].ToString()!,
                Email            = dr["Email"].ToString()!,
                DateOfJoining    = dr["DateOfJoining"] == DBNull.Value
                                       ? null : Convert.ToDateTime(dr["DateOfJoining"]),
                BasicSalary      = Convert.ToDecimal(dr["BasicSalary"]),
                Status           = dr["Status"].ToString()!
            });
        }
    }

    private void LoadDepartments()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd  = new SqlCommand("sp_GetDepartments", conn)
                         { CommandType = CommandType.StoredProcedure };
        conn.Open();
        using var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            Departments.Add(new DepartmentItem
            {
                DepartmentID   = Convert.ToInt32(dr["DepartmentID"]),
                DepartmentName = dr["DepartmentName"].ToString()!
            });
        }
    }

    private void LoadLookupLists()
    {
        Genders            = LoadLookup("tblGender",             "GenderID",            "GenderName");
        Nationalities      = LoadLookup("tblNationality",        "NationalityID",        "NationalityName");
        Religions          = LoadLookup("tblReligion",           "ReligionID",           "ReligionName");
        Languages          = LoadLookup("tblLanguage",           "LanguageID",           "LanguageName");
        WorkerCategories   = LoadLookup("tblWorkerCategory",     "WorkerCategoryID",     "WorkerCategoryName");
        EmploymentTypes    = LoadLookup("tblEmploymentType",     "EmploymentTypeID",     "EmploymentTypeName");
        EmploymentStatuses = LoadLookup("tblEmploymentStatus",   "EmploymentStatusID",   "EmploymentStatusName");
        WorkforceSegments  = LoadLookup("tblWorkforceSegment",   "WorkforceSegmentID",   "WorkforceSegmentName");
        LegalEntities      = LoadLookup("tblLegalEntity",        "LegalEntityID",        "LegalEntityName");
        BusinessUnits      = LoadLookup("tblBusinessUnit",       "BusinessUnitID",       "BusinessUnitName");
        Divisions          = LoadLookup("tblDivision",           "DivisionID",           "DivisionName");
        SalesTeams         = LoadLookup("tblSalesTeam",          "SalesTeamID",          "SalesTeamName");
        CostCenters        = LoadLookup("tblCostCenter",         "CostCenterID",         "CostCenterName");
        Regions            = LoadLookup("tblRegion",             "RegionID",             "RegionName");
        Locations          = LoadLookup("tblLocation",           "LocationID",           "LocationName");
        Jobs               = LoadJobLookup();
        WorkerLocations    = LoadWorkerLocationLookup();
        Cities             = LoadLookup("tblCity",               "CityID",               "CityName");
        Provinces          = LoadLookup("tblProvince",           "ProvinceID",           "ProvinceName");
        SalesGroups        = LoadLookup("tblSalesGroup",         "SalesGroupID",         "SalesGroupName");
        Grades             = LoadLookup("tblGrade",                "GradeID",              "GradeName");
        Extensions         = LoadExtensionLookup();
        BloodGroups        = LoadLookup("tblBloodGroup",         "BloodGroupID",         "BloodGroupName");
        BenefitEntitlements= LoadLookup("tblBenefitEntitlement", "BenefitEntitlementID", "BenefitEntitlementName");
        Users              = LoadUserLookup();
        Banks              = LoadBankLookup();
        DocumentTypes      = LoadLookup("tblDocumentType", "DocumentTypeID", "DocumentTypeName");
    }

    private List<LookupItem> LoadUserLookup()
    {
        var items = new List<LookupItem>();

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT UserID, UserCode, Username, FullName
            FROM   tblUser
            WHERE  IsActive = 1
            ORDER BY FullName, Username;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var code     = dr["UserCode"] == DBNull.Value ? "" : dr["UserCode"].ToString();
            var username = dr["Username"].ToString() ?? "";
            var fullName = dr["FullName"].ToString() ?? "";
            var display  = string.IsNullOrWhiteSpace(code)
                ? $"{username} – {fullName}"
                : $"{code} – {fullName} ({username})";

            items.Add(new LookupItem
            {
                Id   = Convert.ToInt32(dr["UserID"]),
                Name = display
            });
        }

        return items;
    }

    private List<LookupItem> LoadJobLookup()
    {
        var items = new List<LookupItem>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT JobID, JobCode, JobTitle
            FROM tblJob WHERE IsActive = 1
            ORDER BY JobCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var code = dr["JobCode"].ToString() ?? "";
            var title = dr["JobTitle"].ToString() ?? "";
            items.Add(new LookupItem
            {
                Id   = Convert.ToInt32(dr["JobID"]),
                Name = $"{code} – {title}"
            });
        }
        return items;
    }

    private List<LookupItem> LoadExtensionLookup()
    {
        var items = new List<LookupItem>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT ExtensionID, ExtensionCode, ExtensionName
            FROM tblExtension
            WHERE IsActive = 1
            ORDER BY ExtensionCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var code = dr["ExtensionCode"].ToString() ?? "";
            var name = dr["ExtensionName"].ToString() ?? "";
            items.Add(new LookupItem
            {
                Id   = Convert.ToInt32(dr["ExtensionID"]),
                Name = $"{code} – {name}"
            });
        }
        return items;
    }

    private List<LookupItem> LoadWorkerLocationLookup()
    {
        var items = new List<LookupItem>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT wl.WorkerLocationID,
                   e.EmployeeCode,
                   pl.LocationName AS PrimaryLocationName
            FROM tblWorkerLocation wl
            INNER JOIN tblEmployee e ON e.EmployeeID = wl.EmployeeID
            LEFT JOIN tblLocation pl ON pl.LocationID = wl.PrimaryLocationID
            WHERE wl.IsActive = 1
            ORDER BY e.EmployeeCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var code = dr["EmployeeCode"].ToString() ?? "";
            var loc  = dr["PrimaryLocationName"] == DBNull.Value ? "" : dr["PrimaryLocationName"].ToString();
            var name = string.IsNullOrEmpty(loc) ? code : $"{code} – {loc}";
            items.Add(new LookupItem
            {
                Id   = Convert.ToInt32(dr["WorkerLocationID"]),
                Name = name
            });
        }
        return items;
    }

    private List<LookupItem> LoadLookup(string tableName, string idColumn, string nameColumn)
    {
        var items = new List<LookupItem>();

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT {idColumn}, {nameColumn}
            FROM {tableName}
            WHERE IsActive = 1
            ORDER BY {nameColumn};", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            items.Add(new LookupItem
            {
                Id = Convert.ToInt32(dr[idColumn]),
                Name = dr[nameColumn].ToString() ?? ""
            });
        }

        return items;
    }

    private List<LookupItem> LoadBankLookup()
    {
        var items = new List<LookupItem>();

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT BankID,
                   BankName,
                   BranchCode,
                   BranchName
            FROM tblBankMaster
            WHERE IsActive = 1
            ORDER BY BankName, BranchName;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var branchCode = dr["BranchCode"] == DBNull.Value ? "" : dr["BranchCode"].ToString();
            var branchName = dr["BranchName"] == DBNull.Value ? "" : dr["BranchName"].ToString();
            var displayName = dr["BankName"].ToString() ?? "";
            var branchParts = new[] { branchCode, branchName }.Where(v => !string.IsNullOrWhiteSpace(v));
            var branchText = string.Join(" - ", branchParts);

            if (!string.IsNullOrWhiteSpace(branchText))
            {
                displayName = $"{displayName} ({branchText})";
            }

            items.Add(new LookupItem
            {
                Id = Convert.ToInt32(dr["BankID"]),
                Name = displayName
            });
        }

        return items;
    }

    private string GetLookupName(string tableName, string idColumn, string nameColumn, int id)
    {
        if (id <= 0)
            return "";

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT TOP 1 {nameColumn}
            FROM {tableName}
            WHERE {idColumn} = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        conn.Open();

        return cmd.ExecuteScalar()?.ToString() ?? "";
    }

    private int GetGenderIdByName(string genderName)
    {
        if (string.IsNullOrWhiteSpace(genderName))
            return 0;

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT TOP 1 GenderID
            FROM tblGender
            WHERE GenderName = @GenderName;", conn);
        cmd.Parameters.AddWithValue("@GenderName", genderName.Trim());
        conn.Open();

        var result = cmd.ExecuteScalar();
        return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }

    private void LoadForEdit(int employeeID)
    {
        using var conn = new SqlConnection(_conn);
        conn.Open();

        using (var cmd = new SqlCommand(@"
            SELECT EmployeeID, EmployeeCode, FirstName, LastName,
                   FathersHusbandsName, DisplayName, NationalIDNumber,
                   Gender, DateOfBirth,
                   GenderID, MaritalStatus, DepartmentID, DivisionID,
                   NationalityID, ReligionID, LanguageID,
                   WorkerCategoryID, EmploymentTypeID, EmploymentStatusID,
                   WorkforceSegmentID, LegalEntityID, BusinessUnitID,
                   SalesTeamID, CostCenterID,
                   RegionID, LocationID, JobID, WorkerLocationID,
                   CityID, ProvinceID, SalesGroupID, GradeID, ExtensionID, Domicile,
                   BloodGroupID, BenefitEntitlementID, UserID,
                   Designation, DateOfJoining,
                   EmploymentStartDate, ProbationPeriodDays, ProbationEndDate, ConfirmationDate,
                   BasicSalary, Status
            FROM tblEmployee
            WHERE EmployeeID = @EmployeeID;", conn))
        {
            cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                static int IntOrZero(object v) => v == DBNull.Value ? 0 : Convert.ToInt32(v);
                Input = new EmployeeInput
                {
                    EmployeeID          = employeeID,
                    EmployeeCode        = dr["EmployeeCode"].ToString()!,
                    FirstName           = dr["FirstName"].ToString()!,
                    LastName            = dr["LastName"].ToString()!,
                    FathersHusbandsName = dr["FathersHusbandsName"] == DBNull.Value ? "" : dr["FathersHusbandsName"].ToString()!,
                    DisplayName         = dr["DisplayName"] == DBNull.Value ? "" : dr["DisplayName"].ToString()!,
                    NationalIDNumber    = dr["NationalIDNumber"] == DBNull.Value ? "" : dr["NationalIDNumber"].ToString()!,
                    Gender              = dr["Gender"].ToString()!,
                    GenderID            = IntOrZero(dr["GenderID"]) == 0 ? GetGenderIdByName(dr["Gender"].ToString()!) : IntOrZero(dr["GenderID"]),
                    DateOfBirth         = dr["DateOfBirth"] == DBNull.Value ? "" : Convert.ToDateTime(dr["DateOfBirth"]).ToString("yyyy-MM-dd"),
                    MaritalStatus       = dr["MaritalStatus"] == DBNull.Value ? "" : dr["MaritalStatus"].ToString()!,
                    DepartmentID        = IntOrZero(dr["DepartmentID"]),
                    DivisionID          = IntOrZero(dr["DivisionID"]),
                    NationalityID       = IntOrZero(dr["NationalityID"]),
                    ReligionID          = IntOrZero(dr["ReligionID"]),
                    LanguageID          = IntOrZero(dr["LanguageID"]),
                    WorkerCategoryID    = IntOrZero(dr["WorkerCategoryID"]),
                    EmploymentTypeID    = IntOrZero(dr["EmploymentTypeID"]),
                    EmploymentStatusID  = IntOrZero(dr["EmploymentStatusID"]),
                    WorkforceSegmentID  = IntOrZero(dr["WorkforceSegmentID"]),
                    LegalEntityID       = IntOrZero(dr["LegalEntityID"]),
                    BusinessUnitID      = IntOrZero(dr["BusinessUnitID"]),
                    SalesTeamID         = IntOrZero(dr["SalesTeamID"]),
                    CostCenterID        = IntOrZero(dr["CostCenterID"]),
                    RegionID            = IntOrZero(dr["RegionID"]),
                    LocationID          = IntOrZero(dr["LocationID"]),
                    JobID               = IntOrZero(dr["JobID"]),
                    WorkerLocationID    = IntOrZero(dr["WorkerLocationID"]),
                    CityID              = IntOrZero(dr["CityID"]),
                    ProvinceID          = IntOrZero(dr["ProvinceID"]),
                    SalesGroupID        = IntOrZero(dr["SalesGroupID"]),
                    GradeID             = IntOrZero(dr["GradeID"]),
                    ExtensionID         = IntOrZero(dr["ExtensionID"]),
                    Domicile            = dr["Domicile"] == DBNull.Value ? "" : dr["Domicile"].ToString()!,
                    BloodGroupID        = IntOrZero(dr["BloodGroupID"]),
                    BenefitEntitlementID= IntOrZero(dr["BenefitEntitlementID"]),
                    UserID              = IntOrZero(dr["UserID"]),
                    Designation         = dr["Designation"].ToString()!,
                    DateOfJoining       = dr["DateOfJoining"] == DBNull.Value ? "" : Convert.ToDateTime(dr["DateOfJoining"]).ToString("yyyy-MM-dd"),
                    EmploymentStartDate = dr["EmploymentStartDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["EmploymentStartDate"]).ToString("yyyy-MM-dd"),
                    ProbationPeriodDays = dr["ProbationPeriodDays"] == DBNull.Value ? "" : Convert.ToInt32(dr["ProbationPeriodDays"]).ToString(),
                    ProbationEndDate    = dr["ProbationEndDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["ProbationEndDate"]).ToString("yyyy-MM-dd"),
                    ConfirmationDate    = dr["ConfirmationDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["ConfirmationDate"]).ToString("yyyy-MM-dd"),
                    BasicSalary         = dr["BasicSalary"].ToString()!,
                    Status              = dr["Status"].ToString()!
                };
            }
        }

        ContactRecords = LoadEmployeeContacts(conn, employeeID);
        AddressRecords = LoadEmployeeAddresses(conn, employeeID);
        FamilyRecords = LoadEmployeeFamilyMembers(conn, employeeID);
        BankRecords = LoadEmployeeBanks(conn, employeeID);
        EducationRecords = LoadEmployeeEducation(conn, employeeID);
        CertificateRecords = LoadEmployeeCertificates(conn, employeeID);
        DocumentRecords = LoadEmployeeDocuments(conn, employeeID);
        EnsureDefaultRows();
    }

    private int SaveEmployeeCore(SqlConnection conn, SqlTransaction tx, EmployeeInput e)
    {
        static object Fk(int id) => id <= 0 ? DBNull.Value : (object)id;
        static object Str(string? s) => string.IsNullOrWhiteSpace(s) ? DBNull.Value : (object)s.Trim();

        if (e.EmployeeID > 0)
        {
            using var cmd = new SqlCommand(@"
                UPDATE tblEmployee SET
                    EmployeeCode       = @EmployeeCode,
                    FirstName          = @FirstName,
                    LastName           = @LastName,
                    FathersHusbandsName = @FathersHusbandsName,
                    DisplayName        = @DisplayName,
                    NationalIDNumber   = @NationalIDNumber,
                    Gender             = @Gender,
                    GenderID           = @GenderID,
                    DateOfBirth        = @DateOfBirth,
                    MaritalStatus      = @MaritalStatus,
                    DepartmentID       = @DepartmentID,
                    DivisionID         = @DivisionID,
                    NationalityID      = @NationalityID,
                    ReligionID         = @ReligionID,
                    LanguageID         = @LanguageID,
                    WorkerCategoryID   = @WorkerCategoryID,
                    EmploymentTypeID   = @EmploymentTypeID,
                    EmploymentStatusID = @EmploymentStatusID,
                    WorkforceSegmentID = @WorkforceSegmentID,
                    LegalEntityID      = @LegalEntityID,
                    BusinessUnitID     = @BusinessUnitID,
                    SalesTeamID        = @SalesTeamID,
                    CostCenterID       = @CostCenterID,
                    RegionID           = @RegionID,
                    LocationID         = @LocationID,
                    JobID              = @JobID,
                    WorkerLocationID   = @WorkerLocationID,
                    CityID             = @CityID,
                    ProvinceID         = @ProvinceID,
                    SalesGroupID       = @SalesGroupID,
                    GradeID            = @GradeID,
                    ExtensionID        = @ExtensionID,
                    Domicile           = @Domicile,
                    BloodGroupID       = @BloodGroupID,
                    BenefitEntitlementID = @BenefitEntitlementID,
                    UserID             = @UserID,
                    Designation        = @Designation,
                    DateOfJoining      = @DateOfJoining,
                    EmploymentStartDate = @EmploymentStartDate,
                    ProbationPeriodDays = @ProbationPeriodDays,
                    ProbationEndDate   = @ProbationEndDate,
                    ConfirmationDate   = @ConfirmationDate,
                    BasicSalary        = @BasicSalary,
                    Status             = @Status,
                    ModifiedOn         = GETDATE(),
                    ModifiedByUserID   = @ModifiedByUserID
                WHERE EmployeeID = @EmployeeID;", conn, tx);

            AddEmployeeParams(cmd, e, Fk, Str);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            cmd.ExecuteNonQuery();
            return e.EmployeeID;
        }

        using var ins = new SqlCommand(@"
            INSERT INTO tblEmployee
                (EmployeeCode, FirstName, LastName,
                 FathersHusbandsName, DisplayName, NationalIDNumber,
                 Gender, GenderID, DateOfBirth,
                 MaritalStatus, DepartmentID, DivisionID,
                 NationalityID, ReligionID, LanguageID,
                 WorkerCategoryID, EmploymentTypeID, EmploymentStatusID,
                 WorkforceSegmentID, LegalEntityID, BusinessUnitID,
                 SalesTeamID, CostCenterID,
                 RegionID, LocationID, JobID, WorkerLocationID,
                 CityID, ProvinceID, SalesGroupID, GradeID, ExtensionID, Domicile,
                 BloodGroupID, BenefitEntitlementID, UserID,
                 Designation, DateOfJoining,
                 EmploymentStartDate, ProbationPeriodDays, ProbationEndDate, ConfirmationDate,
                 BasicSalary, Status, CreatedOn, CreatedByUserID)
            VALUES
                (@EmployeeCode, @FirstName, @LastName,
                 @FathersHusbandsName, @DisplayName, @NationalIDNumber,
                 @Gender, @GenderID, @DateOfBirth,
                 @MaritalStatus, @DepartmentID, @DivisionID,
                 @NationalityID, @ReligionID, @LanguageID,
                 @WorkerCategoryID, @EmploymentTypeID, @EmploymentStatusID,
                 @WorkforceSegmentID, @LegalEntityID, @BusinessUnitID,
                 @SalesTeamID, @CostCenterID,
                 @RegionID, @LocationID, @JobID, @WorkerLocationID,
                 @CityID, @ProvinceID, @SalesGroupID, @GradeID, @ExtensionID, @Domicile,
                 @BloodGroupID, @BenefitEntitlementID, @UserID,
                 @Designation, @DateOfJoining,
                 @EmploymentStartDate, @ProbationPeriodDays, @ProbationEndDate, @ConfirmationDate,
                 @BasicSalary, @Status, GETDATE(), @CreatedByUserID);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);

        AddEmployeeParams(ins, e, Fk, Str);
        AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
        return Convert.ToInt32(ins.ExecuteScalar());
    }

    private static void AddEmployeeParams(SqlCommand cmd, EmployeeInput e,
        Func<int,object> Fk, Func<string?,object> Str)
    {
        cmd.Parameters.AddWithValue("@EmployeeID",          e.EmployeeID);
        cmd.Parameters.AddWithValue("@EmployeeCode",        e.EmployeeCode.Trim());
        cmd.Parameters.AddWithValue("@FirstName",           e.FirstName.Trim());
        cmd.Parameters.AddWithValue("@LastName",            e.LastName.Trim());
        cmd.Parameters.AddWithValue("@FathersHusbandsName", Str(e.FathersHusbandsName));
        cmd.Parameters.AddWithValue("@DisplayName",         Str(e.DisplayName));
        cmd.Parameters.AddWithValue("@NationalIDNumber",    Str(e.NationalIDNumber));
        cmd.Parameters.AddWithValue("@Gender",              Str(e.Gender));
        cmd.Parameters.AddWithValue("@GenderID",            Fk(e.GenderID));
        cmd.Parameters.AddWithValue("@DateOfBirth",         string.IsNullOrWhiteSpace(e.DateOfBirth) ? DBNull.Value : DateTime.Parse(e.DateOfBirth));
        cmd.Parameters.AddWithValue("@MaritalStatus",       Str(e.MaritalStatus));
        cmd.Parameters.AddWithValue("@DepartmentID",        e.DepartmentID);
        cmd.Parameters.AddWithValue("@DivisionID",          Fk(e.DivisionID));
        cmd.Parameters.AddWithValue("@NationalityID",       Fk(e.NationalityID));
        cmd.Parameters.AddWithValue("@ReligionID",          Fk(e.ReligionID));
        cmd.Parameters.AddWithValue("@LanguageID",          Fk(e.LanguageID));
        cmd.Parameters.AddWithValue("@WorkerCategoryID",    Fk(e.WorkerCategoryID));
        cmd.Parameters.AddWithValue("@EmploymentTypeID",    Fk(e.EmploymentTypeID));
        cmd.Parameters.AddWithValue("@EmploymentStatusID",  Fk(e.EmploymentStatusID));
        cmd.Parameters.AddWithValue("@WorkforceSegmentID",  Fk(e.WorkforceSegmentID));
        cmd.Parameters.AddWithValue("@LegalEntityID",       Fk(e.LegalEntityID));
        cmd.Parameters.AddWithValue("@BusinessUnitID",      Fk(e.BusinessUnitID));
        cmd.Parameters.AddWithValue("@SalesTeamID",         Fk(e.SalesTeamID));
        cmd.Parameters.AddWithValue("@CostCenterID",        Fk(e.CostCenterID));
        cmd.Parameters.AddWithValue("@RegionID",            Fk(e.RegionID));
        cmd.Parameters.AddWithValue("@LocationID",          Fk(e.LocationID));
        cmd.Parameters.AddWithValue("@JobID",               Fk(e.JobID));
        cmd.Parameters.AddWithValue("@WorkerLocationID",    Fk(e.WorkerLocationID));
        cmd.Parameters.AddWithValue("@CityID",              Fk(e.CityID));
        cmd.Parameters.AddWithValue("@ProvinceID",        Fk(e.ProvinceID));
        cmd.Parameters.AddWithValue("@SalesGroupID",      Fk(e.SalesGroupID));
        cmd.Parameters.AddWithValue("@GradeID",           Fk(e.GradeID));
        cmd.Parameters.AddWithValue("@ExtensionID",       Fk(e.ExtensionID));
        cmd.Parameters.AddWithValue("@Domicile",            Str(e.Domicile));
        cmd.Parameters.AddWithValue("@BloodGroupID",        Fk(e.BloodGroupID));
        cmd.Parameters.AddWithValue("@BenefitEntitlementID",Fk(e.BenefitEntitlementID));
        cmd.Parameters.AddWithValue("@UserID",              Fk(e.UserID));
        cmd.Parameters.AddWithValue("@Designation",         e.Designation.Trim());
        cmd.Parameters.AddWithValue("@DateOfJoining",       DateTime.Parse(e.DateOfJoining));
        cmd.Parameters.AddWithValue("@EmploymentStartDate", ParseDateParam(e.EmploymentStartDate));
        cmd.Parameters.AddWithValue("@ProbationPeriodDays", ParseIntParam(e.ProbationPeriodDays));
        cmd.Parameters.AddWithValue("@ProbationEndDate",    ParseDateParam(e.ProbationEndDate));
        cmd.Parameters.AddWithValue("@ConfirmationDate",    ParseDateParam(e.ConfirmationDate));
        cmd.Parameters.AddWithValue("@BasicSalary",         decimal.Parse(e.BasicSalary));
        cmd.Parameters.AddWithValue("@Status",              e.Status);
    }

    private void ReplaceEmployeeContacts(SqlConnection conn, SqlTransaction tx, int employeeID, List<EmployeeContactInput> contacts)
    {
        using (var delCmd = new SqlCommand("DELETE FROM tblEmployeeContact WHERE EmployeeID = @EmployeeID;", conn, tx))
        {
            delCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            delCmd.ExecuteNonQuery();
        }

        int sortOrder = 0;
        foreach (var contact in contacts.Where(c => !string.IsNullOrWhiteSpace(c.ContactType) && (!string.IsNullOrWhiteSpace(c.ContactValue) || !string.IsNullOrWhiteSpace(c.ContactName))))
        {
            sortOrder++;
            using var insCmd = new SqlCommand(@"
                INSERT INTO tblEmployeeContact
                    (EmployeeID, ContactType, ContactName, Relationship, ContactValue, IsPrimary, SortOrder, CreatedOn, CreatedByUserID)
                VALUES
                    (@EmployeeID, @ContactType, @ContactName, @Relationship, @ContactValue, @IsPrimary, @SortOrder, GETDATE(), @CreatedByUserID);", conn, tx);

            insCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            insCmd.Parameters.AddWithValue("@ContactType", contact.ContactType.Trim());
            insCmd.Parameters.AddWithValue("@ContactName", string.IsNullOrWhiteSpace(contact.ContactName) ? DBNull.Value : contact.ContactName.Trim());
            insCmd.Parameters.AddWithValue("@Relationship", string.IsNullOrWhiteSpace(contact.Relationship) ? DBNull.Value : contact.Relationship.Trim());
            insCmd.Parameters.AddWithValue("@ContactValue", string.IsNullOrWhiteSpace(contact.ContactValue) ? DBNull.Value : contact.ContactValue.Trim());
            insCmd.Parameters.AddWithValue("@IsPrimary", contact.IsPrimary);
            insCmd.Parameters.AddWithValue("@SortOrder", sortOrder);
            AuditHelper.AddCreatedBy(insCmd, _auth.CurrentUserId);
            insCmd.ExecuteNonQuery();
        }
    }

    private void ReplaceEmployeeAddresses(SqlConnection conn, SqlTransaction tx, int employeeID, List<EmployeeAddressInput> addresses)
    {
        using (var delCmd = new SqlCommand("DELETE FROM tblEmployeeAddress WHERE EmployeeID = @EmployeeID;", conn, tx))
        {
            delCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            delCmd.ExecuteNonQuery();
        }

        int sortOrder = 0;
        foreach (var address in addresses.Where(a => !string.IsNullOrWhiteSpace(a.AddressLine)))
        {
            sortOrder++;
            using var insCmd = new SqlCommand(@"
                INSERT INTO tblEmployeeAddress
                    (EmployeeID, AddressType, AddressLine, City, ProvinceState, PostalCode, IsPrimary, SortOrder, CreatedOn, CreatedByUserID)
                VALUES
                    (@EmployeeID, @AddressType, @AddressLine, @City, @ProvinceState, @PostalCode, @IsPrimary, @SortOrder, GETDATE(), @CreatedByUserID);", conn, tx);

            insCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            insCmd.Parameters.AddWithValue("@AddressType", string.IsNullOrWhiteSpace(address.AddressType) ? "Other" : address.AddressType.Trim());
            insCmd.Parameters.AddWithValue("@AddressLine", address.AddressLine.Trim());
            insCmd.Parameters.AddWithValue("@City", string.IsNullOrWhiteSpace(address.City) ? DBNull.Value : address.City.Trim());
            insCmd.Parameters.AddWithValue("@ProvinceState", string.IsNullOrWhiteSpace(address.ProvinceState) ? DBNull.Value : address.ProvinceState.Trim());
            insCmd.Parameters.AddWithValue("@PostalCode", string.IsNullOrWhiteSpace(address.PostalCode) ? DBNull.Value : address.PostalCode.Trim());
            insCmd.Parameters.AddWithValue("@IsPrimary", address.IsPrimary);
            insCmd.Parameters.AddWithValue("@SortOrder", sortOrder);
            AuditHelper.AddCreatedBy(insCmd, _auth.CurrentUserId);
            insCmd.ExecuteNonQuery();
        }
    }

    private void ReplaceEmployeeFamilyMembers(SqlConnection conn, SqlTransaction tx, int employeeID, List<EmployeeFamilyMemberInput> members)
    {
        using (var delCmd = new SqlCommand("DELETE FROM tblEmployeeFamilyMember WHERE EmployeeID = @EmployeeID;", conn, tx))
        {
            delCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            delCmd.ExecuteNonQuery();
        }

        int sortOrder = 0;
        foreach (var member in members.Where(m => !string.IsNullOrWhiteSpace(m.MemberName)))
        {
            sortOrder++;
            using var insCmd = new SqlCommand(@"
                INSERT INTO tblEmployeeFamilyMember
                    (EmployeeID, MemberName, Relationship, Gender, DateOfBirth, ContactNumber, IsDependent, SortOrder, CreatedOn, CreatedByUserID)
                VALUES
                    (@EmployeeID, @MemberName, @Relationship, @Gender, @DateOfBirth, @ContactNumber, @IsDependent, @SortOrder, GETDATE(), @CreatedByUserID);", conn, tx);

            insCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            insCmd.Parameters.AddWithValue("@MemberName", member.MemberName.Trim());
            insCmd.Parameters.AddWithValue("@Relationship", string.IsNullOrWhiteSpace(member.Relationship) ? DBNull.Value : member.Relationship.Trim());
            insCmd.Parameters.AddWithValue("@Gender", string.IsNullOrWhiteSpace(member.Gender) ? DBNull.Value : member.Gender.Trim());
            insCmd.Parameters.AddWithValue("@DateOfBirth", string.IsNullOrWhiteSpace(member.DateOfBirth) ? DBNull.Value : DateTime.Parse(member.DateOfBirth));
            insCmd.Parameters.AddWithValue("@ContactNumber", string.IsNullOrWhiteSpace(member.ContactNumber) ? DBNull.Value : member.ContactNumber.Trim());
            insCmd.Parameters.AddWithValue("@IsDependent", member.IsDependent);
            insCmd.Parameters.AddWithValue("@SortOrder", sortOrder);
            AuditHelper.AddCreatedBy(insCmd, _auth.CurrentUserId);
            insCmd.ExecuteNonQuery();
        }
    }

    private void ReplaceEmployeeBanks(SqlConnection conn, SqlTransaction tx, int employeeID, List<EmployeeBankInput> banks)
    {
        using (var delCmd = new SqlCommand("DELETE FROM tblEmployeeBank WHERE EmployeeID = @EmployeeID;", conn, tx))
        {
            delCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            delCmd.ExecuteNonQuery();
        }

        int sortOrder = 0;
        foreach (var bank in banks.Where(b => b.BankID > 0))
        {
            sortOrder++;
            using var insCmd = new SqlCommand(@"
                INSERT INTO tblEmployeeBank
                    (EmployeeID, BankID, BranchCode,BranchName,AccountTitle, IBAN, SwiftBICCode, AccountType, AccountVerificationStatus, IsPrimary, SortOrder, CreatedOn, CreatedByUserID)
                VALUES
                    (@EmployeeID, @BankID,@BranchCode,@BranchName, @AccountTitle, @IBAN, @SwiftBICCode, @AccountType, @AccountVerificationStatus, @IsPrimary, @SortOrder, GETDATE(), @CreatedByUserID);", conn, tx);

            insCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            insCmd.Parameters.AddWithValue("@BankID", bank.BankID);
            insCmd.Parameters.AddWithValue("@AccountTitle", string.IsNullOrWhiteSpace(bank.AccountTitle) ? DBNull.Value : bank.AccountTitle.Trim());
            insCmd.Parameters.AddWithValue("@IBAN", string.IsNullOrWhiteSpace(bank.IBAN) ? DBNull.Value : bank.IBAN.Trim());
            insCmd.Parameters.AddWithValue("@BranchCode", string.IsNullOrWhiteSpace(bank.BranchCode) ? DBNull.Value : bank.BranchCode.Trim());
            insCmd.Parameters.AddWithValue("@BranchName", string.IsNullOrWhiteSpace(bank.BranchName) ? DBNull.Value : bank.BranchName.Trim());
            insCmd.Parameters.AddWithValue("@SwiftBICCode", string.IsNullOrWhiteSpace(bank.SwiftBICCode) ? DBNull.Value : bank.SwiftBICCode.Trim());
            insCmd.Parameters.AddWithValue("@AccountType", string.IsNullOrWhiteSpace(bank.AccountType) ? DBNull.Value : bank.AccountType.Trim());
            insCmd.Parameters.AddWithValue("@AccountVerificationStatus", string.IsNullOrWhiteSpace(bank.AccountVerificationStatus) ? "Pending" : bank.AccountVerificationStatus.Trim());
            insCmd.Parameters.AddWithValue("@IsPrimary", bank.IsPrimary);
            insCmd.Parameters.AddWithValue("@SortOrder", sortOrder);
            AuditHelper.AddCreatedBy(insCmd, _auth.CurrentUserId);
            insCmd.ExecuteNonQuery();
        }
    }

    private void ReplaceEmployeeEducation(SqlConnection conn, SqlTransaction tx, int employeeID, List<EmployeeEducationInput> records)
    {
        using (var delCmd = new SqlCommand("DELETE FROM tblEmployeeEducation WHERE EmployeeID = @EmployeeID;", conn, tx))
        {
            delCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            delCmd.ExecuteNonQuery();
        }

        int sortOrder = 0;
        foreach (var edu in records.Where(e =>
            !string.IsNullOrWhiteSpace(e.HighestQualification)
            || !string.IsNullOrWhiteSpace(e.DegreeCertificate)
            || !string.IsNullOrWhiteSpace(e.Institution)))
        {
            sortOrder++;
            using var insCmd = new SqlCommand(@"
                INSERT INTO tblEmployeeEducation
                    (EmployeeID, HighestQualification, DegreeCertificate, Specialization, Institution,
                     YearOfPassing, GradeCGPA, SortOrder, CreatedOn, CreatedByUserID)
                VALUES
                    (@EmployeeID, @HighestQualification, @DegreeCertificate, @Specialization, @Institution,
                     @YearOfPassing, @GradeCGPA, @SortOrder, GETDATE(), @CreatedByUserID);", conn, tx);

            insCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            insCmd.Parameters.AddWithValue("@HighestQualification", string.IsNullOrWhiteSpace(edu.HighestQualification) ? DBNull.Value : edu.HighestQualification.Trim());
            insCmd.Parameters.AddWithValue("@DegreeCertificate", string.IsNullOrWhiteSpace(edu.DegreeCertificate) ? DBNull.Value : edu.DegreeCertificate.Trim());
            insCmd.Parameters.AddWithValue("@Specialization", string.IsNullOrWhiteSpace(edu.Specialization) ? DBNull.Value : edu.Specialization.Trim());
            insCmd.Parameters.AddWithValue("@Institution", string.IsNullOrWhiteSpace(edu.Institution) ? DBNull.Value : edu.Institution.Trim());
            insCmd.Parameters.AddWithValue("@YearOfPassing", ParseIntParam(edu.YearOfPassing));
            insCmd.Parameters.AddWithValue("@GradeCGPA", string.IsNullOrWhiteSpace(edu.GradeCGPA) ? DBNull.Value : edu.GradeCGPA.Trim());
            insCmd.Parameters.AddWithValue("@SortOrder", sortOrder);
            AuditHelper.AddCreatedBy(insCmd, _auth.CurrentUserId);
            insCmd.ExecuteNonQuery();
        }
    }

    private void ReplaceEmployeeCertificates(SqlConnection conn, SqlTransaction tx, int employeeID, List<EmployeeCertificateInput> records)
    {
        using (var delCmd = new SqlCommand("DELETE FROM tblEmployeeCertificate WHERE EmployeeID = @EmployeeID;", conn, tx))
        {
            delCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            delCmd.ExecuteNonQuery();
        }

        int sortOrder = 0;
        for (int i = 0; i < records.Count; i++)
        {
            var cert = records[i];
            if (string.IsNullOrWhiteSpace(cert.CertificationName)
                && string.IsNullOrWhiteSpace(cert.CertificateNumber)
                && string.IsNullOrWhiteSpace(cert.CertificationBody))
                continue;

            var docPath = cert.CertificateCopyPath;
            var file = Request.Form.Files[$"CertCopy_{i}"];
            var uploaded = SaveCertificateFile(file, employeeID, i);
            if (!string.IsNullOrEmpty(uploaded))
                docPath = uploaded;

            sortOrder++;
            using var insCmd = new SqlCommand(@"
                INSERT INTO tblEmployeeCertificate
                    (EmployeeID, CertificationName, CertificationBody, CertificateNumber,
                     IssueDate, ExpiryDate, RenewalRequired, CertificateCopyPath,
                     SortOrder, CreatedOn, CreatedByUserID)
                VALUES
                    (@EmployeeID, @CertificationName, @CertificationBody, @CertificateNumber,
                     @IssueDate, @ExpiryDate, @RenewalRequired, @CertificateCopyPath,
                     @SortOrder, GETDATE(), @CreatedByUserID);", conn, tx);

            insCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            insCmd.Parameters.AddWithValue("@CertificationName", string.IsNullOrWhiteSpace(cert.CertificationName) ? DBNull.Value : cert.CertificationName.Trim());
            insCmd.Parameters.AddWithValue("@CertificationBody", string.IsNullOrWhiteSpace(cert.CertificationBody) ? DBNull.Value : cert.CertificationBody.Trim());
            insCmd.Parameters.AddWithValue("@CertificateNumber", string.IsNullOrWhiteSpace(cert.CertificateNumber) ? DBNull.Value : cert.CertificateNumber.Trim());
            insCmd.Parameters.AddWithValue("@IssueDate", ParseDateParam(cert.IssueDate));
            insCmd.Parameters.AddWithValue("@ExpiryDate", ParseDateParam(cert.ExpiryDate));
            insCmd.Parameters.AddWithValue("@RenewalRequired", cert.RenewalRequired);
            insCmd.Parameters.AddWithValue("@CertificateCopyPath", string.IsNullOrWhiteSpace(docPath) ? DBNull.Value : docPath);
            insCmd.Parameters.AddWithValue("@SortOrder", sortOrder);
            AuditHelper.AddCreatedBy(insCmd, _auth.CurrentUserId);
            insCmd.ExecuteNonQuery();
        }
    }

    private string? SaveCertificateFile(IFormFile? file, int employeeId, int rowIndex)
    {
        if (file == null || file.Length == 0)
            return null;

        var uploads = Path.Combine(_env.WebRootPath, "uploads", "employee-certificates");
        Directory.CreateDirectory(uploads);

        var ext = Path.GetExtension(file.FileName);
        var safeName = $"{employeeId}_{rowIndex}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
        var fullPath = Path.Combine(uploads, safeName);

        using (var fs = System.IO.File.Create(fullPath))
            file.CopyTo(fs);

        return $"/uploads/employee-certificates/{safeName}";
    }

    private List<EmployeeContactInput> LoadEmployeeContacts(SqlConnection conn, int employeeID)
    {
        var contacts = new List<EmployeeContactInput>();
        using var cmd = new SqlCommand(@"
            SELECT ContactType, ContactName, Relationship, ContactValue, IsPrimary
            FROM tblEmployeeContact
            WHERE EmployeeID = @EmployeeID
            ORDER BY SortOrder, ContactID;", conn);
        cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            contacts.Add(new EmployeeContactInput
            {
                ContactType = dr["ContactType"].ToString() ?? "",
                ContactName = dr["ContactName"] == DBNull.Value ? "" : dr["ContactName"].ToString() ?? "",
                Relationship = dr["Relationship"] == DBNull.Value ? "" : dr["Relationship"].ToString() ?? "",
                ContactValue = dr["ContactValue"] == DBNull.Value ? "" : dr["ContactValue"].ToString() ?? "",
                IsPrimary = dr["IsPrimary"] != DBNull.Value && Convert.ToBoolean(dr["IsPrimary"])
            });
        }
        return contacts;
    }

    private List<EmployeeAddressInput> LoadEmployeeAddresses(SqlConnection conn, int employeeID)
    {
        var addresses = new List<EmployeeAddressInput>();
        using var cmd = new SqlCommand(@"
            SELECT AddressType, AddressLine, City, ProvinceState, PostalCode, IsPrimary
            FROM tblEmployeeAddress
            WHERE EmployeeID = @EmployeeID
            ORDER BY SortOrder, AddressID;", conn);
        cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            addresses.Add(new EmployeeAddressInput
            {
                AddressType = dr["AddressType"].ToString() ?? "",
                AddressLine = dr["AddressLine"] == DBNull.Value ? "" : dr["AddressLine"].ToString() ?? "",
                City = dr["City"] == DBNull.Value ? "" : dr["City"].ToString() ?? "",
                ProvinceState = dr["ProvinceState"] == DBNull.Value ? "" : dr["ProvinceState"].ToString() ?? "",
                PostalCode = dr["PostalCode"] == DBNull.Value ? "" : dr["PostalCode"].ToString() ?? "",
                IsPrimary = dr["IsPrimary"] != DBNull.Value && Convert.ToBoolean(dr["IsPrimary"])
            });
        }
        return addresses;
    }

    private List<EmployeeFamilyMemberInput> LoadEmployeeFamilyMembers(SqlConnection conn, int employeeID)
    {
        var members = new List<EmployeeFamilyMemberInput>();
        using var cmd = new SqlCommand(@"
            SELECT MemberName, Relationship, Gender, DateOfBirth, ContactNumber, IsDependent
            FROM tblEmployeeFamilyMember
            WHERE EmployeeID = @EmployeeID
            ORDER BY SortOrder, FamilyMemberID;", conn);
        cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            members.Add(new EmployeeFamilyMemberInput
            {
                MemberName = dr["MemberName"] == DBNull.Value ? "" : dr["MemberName"].ToString() ?? "",
                Relationship = dr["Relationship"] == DBNull.Value ? "" : dr["Relationship"].ToString() ?? "",
                Gender = dr["Gender"] == DBNull.Value ? "" : dr["Gender"].ToString() ?? "",
                DateOfBirth = dr["DateOfBirth"] == DBNull.Value ? "" : Convert.ToDateTime(dr["DateOfBirth"]).ToString("yyyy-MM-dd"),
                ContactNumber = dr["ContactNumber"] == DBNull.Value ? "" : dr["ContactNumber"].ToString() ?? "",
                IsDependent = dr["IsDependent"] != DBNull.Value && Convert.ToBoolean(dr["IsDependent"])
            });
        }
        return members;
    }

    private List<EmployeeBankInput> LoadEmployeeBanks(SqlConnection conn, int employeeID)
    {
        var banks = new List<EmployeeBankInput>();
        using var cmd = new SqlCommand(@"
            SELECT BankID,BranchCode,BranchName, AccountTitle, IBAN, SwiftBICCode, AccountType, AccountVerificationStatus, IsPrimary
            FROM tblEmployeeBank
            WHERE EmployeeID = @EmployeeID
            ORDER BY SortOrder, EmployeeBankID;", conn);
        cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            banks.Add(new EmployeeBankInput
            {
                BankID = dr["BankID"] == DBNull.Value ? 0 : Convert.ToInt32(dr["BankID"]),
                BranchCode = dr["BranchCode"] == DBNull.Value ? "" : dr["BranchCode"].ToString() ?? "",
                BranchName = dr["BranchName"] == DBNull.Value ? "" : dr["BranchName"].ToString() ?? "",
                AccountTitle = dr["AccountTitle"] == DBNull.Value ? "" : dr["AccountTitle"].ToString() ?? "",
                IBAN = dr["IBAN"] == DBNull.Value ? "" : dr["IBAN"].ToString() ?? "",
                SwiftBICCode = dr["SwiftBICCode"] == DBNull.Value ? "" : dr["SwiftBICCode"].ToString() ?? "",
                AccountType = dr["AccountType"] == DBNull.Value ? "" : dr["AccountType"].ToString() ?? "",
                AccountVerificationStatus = dr["AccountVerificationStatus"] == DBNull.Value ? "Pending" : dr["AccountVerificationStatus"].ToString() ?? "Pending",
                IsPrimary = dr["IsPrimary"] != DBNull.Value && Convert.ToBoolean(dr["IsPrimary"])
            });
        }
        return banks;
    }

    private List<EmployeeEducationInput> LoadEmployeeEducation(SqlConnection conn, int employeeID)
    {
        var records = new List<EmployeeEducationInput>();
        using var cmd = new SqlCommand(@"
            SELECT HighestQualification, DegreeCertificate, Specialization, Institution,
                   YearOfPassing, GradeCGPA
            FROM tblEmployeeEducation
            WHERE EmployeeID = @EmployeeID
            ORDER BY SortOrder, EducationID;", conn);
        cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            records.Add(new EmployeeEducationInput
            {
                HighestQualification = dr["HighestQualification"] == DBNull.Value ? "" : dr["HighestQualification"].ToString() ?? "",
                DegreeCertificate = dr["DegreeCertificate"] == DBNull.Value ? "" : dr["DegreeCertificate"].ToString() ?? "",
                Specialization = dr["Specialization"] == DBNull.Value ? "" : dr["Specialization"].ToString() ?? "",
                Institution = dr["Institution"] == DBNull.Value ? "" : dr["Institution"].ToString() ?? "",
                YearOfPassing = dr["YearOfPassing"] == DBNull.Value ? "" : Convert.ToInt32(dr["YearOfPassing"]).ToString(),
                GradeCGPA = dr["GradeCGPA"] == DBNull.Value ? "" : dr["GradeCGPA"].ToString() ?? ""
            });
        }
        return records;
    }

    private List<EmployeeCertificateInput> LoadEmployeeCertificates(SqlConnection conn, int employeeID)
    {
        var records = new List<EmployeeCertificateInput>();
        using var cmd = new SqlCommand(@"
            SELECT CertificationName, CertificationBody, CertificateNumber,
                   IssueDate, ExpiryDate, RenewalRequired, CertificateCopyPath
            FROM tblEmployeeCertificate
            WHERE EmployeeID = @EmployeeID
            ORDER BY SortOrder, CertificateID;", conn);
        cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            records.Add(new EmployeeCertificateInput
            {
                CertificationName = dr["CertificationName"] == DBNull.Value ? "" : dr["CertificationName"].ToString() ?? "",
                CertificationBody = dr["CertificationBody"] == DBNull.Value ? "" : dr["CertificationBody"].ToString() ?? "",
                CertificateNumber = dr["CertificateNumber"] == DBNull.Value ? "" : dr["CertificateNumber"].ToString() ?? "",
                IssueDate = dr["IssueDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["IssueDate"]).ToString("yyyy-MM-dd"),
                ExpiryDate = dr["ExpiryDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["ExpiryDate"]).ToString("yyyy-MM-dd"),
                RenewalRequired = dr["RenewalRequired"] != DBNull.Value && Convert.ToBoolean(dr["RenewalRequired"]),
                CertificateCopyPath = dr["CertificateCopyPath"] == DBNull.Value ? "" : dr["CertificateCopyPath"].ToString() ?? ""
            });
        }
        return records;
    }

    private void ReplaceEmployeeDocuments(SqlConnection conn, SqlTransaction tx, int employeeID, List<EmployeeDocumentInput> records)
    {
        using (var delCmd = new SqlCommand("DELETE FROM tblEmployeeDocument WHERE EmployeeID = @EmployeeID;", conn, tx))
        {
            delCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            delCmd.ExecuteNonQuery();
        }

        int sortOrder = 0;
        for (int i = 0; i < records.Count; i++)
        {
            var doc = records[i];
            if (doc.DocumentTypeID <= 0
                && string.IsNullOrWhiteSpace(doc.DocumentNumber)
                && string.IsNullOrWhiteSpace(doc.DocumentPath)
                && string.IsNullOrWhiteSpace(doc.Remarks))
                continue;

            var docPath = doc.DocumentPath;
            var originalName = doc.OriginalFileName;
            var file = Request.Form.Files[$"DocFile_{i}"];
            var uploaded = SaveDocumentFile(file, employeeID, i);
            if (!string.IsNullOrEmpty(uploaded.path))
            {
                docPath = uploaded.path;
                originalName = uploaded.originalName;
            }

            var status = string.IsNullOrWhiteSpace(doc.VerificationStatus) ? "Pending" : doc.VerificationStatus.Trim();
            var verifiedOn = status == "Verified" ? (object)DateTime.Now : DBNull.Value;
            var verifiedBy = status == "Verified" ? (object)(_auth.CurrentUserId ?? 0) : DBNull.Value;
            if (status == "Verified" && (_auth.CurrentUserId ?? 0) <= 0)
                verifiedBy = DBNull.Value;

            sortOrder++;
            using var insCmd = new SqlCommand(@"
                INSERT INTO tblEmployeeDocument
                    (EmployeeID, DocumentTypeID, DocumentNumber, IssueDate, ExpiryDate, Remarks,
                     DocumentPath, OriginalFileName, VerificationStatus, VerifiedOn, VerifiedByUserID,
                     SortOrder, CreatedOn, CreatedByUserID)
                VALUES
                    (@EmployeeID, @DocumentTypeID, @DocumentNumber, @IssueDate, @ExpiryDate, @Remarks,
                     @DocumentPath, @OriginalFileName, @VerificationStatus, @VerifiedOn, @VerifiedByUserID,
                     @SortOrder, GETDATE(), @CreatedByUserID);", conn, tx);

            insCmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            insCmd.Parameters.AddWithValue("@DocumentTypeID", doc.DocumentTypeID <= 0 ? DBNull.Value : doc.DocumentTypeID);
            insCmd.Parameters.AddWithValue("@DocumentNumber", string.IsNullOrWhiteSpace(doc.DocumentNumber) ? DBNull.Value : doc.DocumentNumber.Trim());
            insCmd.Parameters.AddWithValue("@IssueDate", ParseDateParam(doc.IssueDate));
            insCmd.Parameters.AddWithValue("@ExpiryDate", ParseDateParam(doc.ExpiryDate));
            insCmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(doc.Remarks) ? DBNull.Value : doc.Remarks.Trim());
            insCmd.Parameters.AddWithValue("@DocumentPath", string.IsNullOrWhiteSpace(docPath) ? DBNull.Value : docPath);
            insCmd.Parameters.AddWithValue("@OriginalFileName", string.IsNullOrWhiteSpace(originalName) ? DBNull.Value : originalName);
            insCmd.Parameters.AddWithValue("@VerificationStatus", status);
            insCmd.Parameters.AddWithValue("@VerifiedOn", verifiedOn);
            insCmd.Parameters.AddWithValue("@VerifiedByUserID", verifiedBy);
            insCmd.Parameters.AddWithValue("@SortOrder", sortOrder);
            AuditHelper.AddCreatedBy(insCmd, _auth.CurrentUserId);
            insCmd.ExecuteNonQuery();
        }
    }

    private (string? path, string? originalName) SaveDocumentFile(IFormFile? file, int employeeId, int rowIndex)
    {
        if (file == null || file.Length == 0)
            return (null, null);

        var uploads = Path.Combine(_env.WebRootPath, "uploads", "employee-documents");
        Directory.CreateDirectory(uploads);

        var ext = Path.GetExtension(file.FileName);
        var safeName = $"{employeeId}_{rowIndex}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
        var fullPath = Path.Combine(uploads, safeName);

        using (var fs = System.IO.File.Create(fullPath))
            file.CopyTo(fs);

        return ($"/uploads/employee-documents/{safeName}", file.FileName);
    }

    private List<EmployeeDocumentInput> LoadEmployeeDocuments(SqlConnection conn, int employeeID)
    {
        var records = new List<EmployeeDocumentInput>();
        using var cmd = new SqlCommand(@"
            SELECT d.DocumentTypeID, dt.DocumentTypeName, d.DocumentNumber,
                   d.IssueDate, d.ExpiryDate, d.Remarks,
                   d.DocumentPath, d.OriginalFileName, d.VerificationStatus
            FROM tblEmployeeDocument d
            LEFT JOIN tblDocumentType dt ON dt.DocumentTypeID = d.DocumentTypeID
            WHERE d.EmployeeID = @EmployeeID
            ORDER BY d.SortOrder, d.EmployeeDocumentID;", conn);
        cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            records.Add(new EmployeeDocumentInput
            {
                DocumentTypeID = dr["DocumentTypeID"] == DBNull.Value ? 0 : Convert.ToInt32(dr["DocumentTypeID"]),
                DocumentTypeName = dr["DocumentTypeName"] == DBNull.Value ? "" : dr["DocumentTypeName"].ToString() ?? "",
                DocumentNumber = dr["DocumentNumber"] == DBNull.Value ? "" : dr["DocumentNumber"].ToString() ?? "",
                IssueDate = dr["IssueDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["IssueDate"]).ToString("yyyy-MM-dd"),
                ExpiryDate = dr["ExpiryDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["ExpiryDate"]).ToString("yyyy-MM-dd"),
                Remarks = dr["Remarks"] == DBNull.Value ? "" : dr["Remarks"].ToString() ?? "",
                DocumentPath = dr["DocumentPath"] == DBNull.Value ? "" : dr["DocumentPath"].ToString() ?? "",
                OriginalFileName = dr["OriginalFileName"] == DBNull.Value ? "" : dr["OriginalFileName"].ToString() ?? "",
                VerificationStatus = dr["VerificationStatus"] == DBNull.Value ? "Pending" : dr["VerificationStatus"].ToString() ?? "Pending"
            });
        }
        return records;
    }

    private List<T> DeserializeList<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<T>();

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    private void EnsureDefaultRows()
    {
        if (ContactRecords.Count == 0)
        {
            ContactRecords.Add(new EmployeeContactInput { ContactType = "OfficialEmail", IsPrimary = true });
        }

        if (AddressRecords.Count == 0)
        {
            AddressRecords.Add(new EmployeeAddressInput { AddressType = "Current", IsPrimary = true });
        }
    }

    private static object ParseDateParam(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : (object)DateTime.Parse(value);

    private static object ParseIntParam(string? value) =>
        string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out int n) || n < 0
            ? DBNull.Value : (object)n;

    private static void ApplyTenureCalculations(EmployeeInput e)
    {
        var startText = !string.IsNullOrWhiteSpace(e.EmploymentStartDate)
            ? e.EmploymentStartDate
            : e.DateOfJoining;

        if (string.IsNullOrWhiteSpace(startText) ||
            !int.TryParse(e.ProbationPeriodDays, out int days) || days <= 0)
            return;

        e.ProbationEndDate = DateTime.Parse(startText).AddDays(days).ToString("yyyy-MM-dd");
    }

    public static string FormatAge(DateTime? dateOfBirth)
    {
        if (!dateOfBirth.HasValue) return "";
        var today = DateTime.Today;
        var dob = dateOfBirth.Value.Date;
        if (dob > today) return "";

        var age = today.Year - dob.Year;
        if (dob > today.AddYears(-age)) age--;
        return age >= 0 ? $"{age} yr{(age == 1 ? "" : "s")}" : "";
    }

    public static string FormatTenure(DateTime? from)
    {
        if (!from.HasValue) return "";
        var start = from.Value.Date;
        var end = DateTime.Today;
        if (start > end) return "";

        var months = (end.Year - start.Year) * 12 + end.Month - start.Month;
        if (end.Day < start.Day) months--;
        if (months < 0) return "";

        var years = months / 12;
        months %= 12;
        var parts = new List<string>();
        if (years > 0) parts.Add($"{years} yr{(years == 1 ? "" : "s")}");
        if (months > 0) parts.Add($"{months} mo{(months == 1 ? "" : "s")}");
        if (parts.Count == 0)
        {
            var days = (end - start).Days;
            parts.Add($"{days} day{(days == 1 ? "" : "s")}");
        }
        return string.Join(", ", parts);
    }
}
