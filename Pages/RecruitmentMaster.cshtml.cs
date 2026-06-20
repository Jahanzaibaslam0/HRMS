using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class RecruitmentListItem
{
    public int RecruitmentID { get; set; }
    public string JobRequisitionNumber { get; set; } = "";
    public string CandidateName { get; set; } = "";
    public string PositionTitle { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string InterviewStatus { get; set; } = "";
    public DateTime? JoiningDate { get; set; }
    public string OnboardingStatus { get; set; } = "";
    public string RecruitmentSource { get; set; } = "";
}

public class RecruitmentInput
{
    public int RecruitmentID { get; set; }
    public string JobRequisitionNumber { get; set; } = "";
    public string RecruitmentSource { get; set; } = "";
    public string PositionTitle { get; set; } = "";
    public int DepartmentID { get; set; }
    public int HiringManagerEmployeeID { get; set; }
    public string CandidateName { get; set; } = "";
    public string PersonalEmail { get; set; } = "";
    public string PersonalPhone { get; set; } = "";
    public string ApplicationDate { get; set; } = "";
    public string InterviewDate { get; set; } = "";
    public string InterviewStatus { get; set; } = "";
    public string SelectionDate { get; set; } = "";
    public string OfferLetterNumber { get; set; } = "";
    public string OfferedSalary { get; set; } = "";
    public string OfferDate { get; set; } = "";
    public string OfferAcceptedDate { get; set; } = "";
    public string BackgroundVerificationStatus { get; set; } = "";
    public string ReferenceCheckStatus { get; set; } = "";
    public string OnboardingStatus { get; set; } = "";
    public string JoiningDate { get; set; } = "";
    public bool InductionCompleted { get; set; }
    public string InductionDate { get; set; } = "";
    public bool DocumentsSubmitted { get; set; }
    public bool SystemAccessProvided { get; set; }
    public string OfficialEmailCreated { get; set; } = "";
    public bool EquipmentIssued { get; set; }
    public string AssetDetails { get; set; } = "";
    public bool TrainingScheduleAssigned { get; set; }
    public int BuddyMentorEmployeeID { get; set; }
    public string ProbationPeriod { get; set; } = "";
    public string ProbationReviewSchedule { get; set; } = "";
    public string ConfirmationStatus { get; set; } = "";
}

public class RecruitmentAuditInfo
{
    public string CreatedBy { get; set; } = "";
    public string CreatedDate { get; set; } = "";
    public string UpdatedBy { get; set; } = "";
    public string UpdatedDate { get; set; } = "";
}

public class RecruitmentMasterModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    public static readonly string[] RecruitmentSources =
    {
        "Internal Referral", "Job Portal", "LinkedIn", "Recruitment Agency",
        "Walk-in", "Campus Hiring", "Social Media", "Other"
    };

    public static readonly string[] InterviewStatuses =
        { "Selected", "Rejected", "On Hold" };

    public static readonly string[] VerificationStatuses =
        { "Pending", "In Progress", "Cleared", "Failed", "Not Required" };

    public static readonly string[] OnboardingStatuses =
        { "Not Started", "In Progress", "Completed", "On Hold" };

    public static readonly string[] ConfirmationStatuses =
        { "Pending", "Confirmed", "Extended", "Terminated" };

    public static readonly string[] ProbationPeriods =
        { "1 month", "2 months", "3 months", "6 months", "12 months", "Other" };

    public RecruitmentMasterModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    public List<RecruitmentListItem> Records { get; set; } = new();
    public List<LookupItem> Departments { get; set; } = new();
    public List<LookupItem> Employees { get; set; } = new();
    [BindProperty]
    public RecruitmentInput Input { get; set; } = new();
    public RecruitmentAuditInfo Audit { get; set; } = new();
    public IReadOnlyList<string> RecruitmentSourceOptions => RecruitmentSources;
    public IReadOnlyList<string> InterviewStatusOptions => InterviewStatuses;
    public IReadOnlyList<string> VerificationStatusOptions => VerificationStatuses;
    public IReadOnlyList<string> OnboardingStatusOptions => OnboardingStatuses;
    public IReadOnlyList<string> ConfirmationStatusOptions => ConfirmationStatuses;
    public IReadOnlyList<string> ProbationPeriodOptions => ProbationPeriods;
    public bool EditMode { get; set; }
    public bool ShowForm { get; set; }
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet([FromQuery] int? editId, [FromQuery] bool? newRecord)
    {
        LoadAlert();
        ShowForm = (editId.HasValue && editId > 0) || newRecord == true;

        if (ShowForm)
        {
            LoadLookups();
            if (editId.HasValue && editId > 0)
            {
                LoadForEdit(editId.Value);
                EditMode = true;
            }
        }
        else
        {
            LoadRecords();
        }
    }

    public IActionResult OnPost(bool EditMode)
    {
        this.EditMode = EditMode;

        if (string.IsNullOrWhiteSpace(Input.CandidateName))
        {
            AlertMessage = "Candidate Name is required.";
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            return Page();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            SaveRecord(conn, Input);

            TempData["Alert"] = EditMode ? "Recruitment record updated successfully." : "Recruitment record added successfully.";
            TempData["AlertType"] = "success";
            return RedirectToPage(new { editId = Input.RecruitmentID });
        }
        catch (Exception ex)
        {
            AlertMessage = "Error: " + ex.Message;
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            return Page();
        }
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var cmd = new SqlCommand("DELETE FROM tblRecruitment WHERE RecruitmentID = @Id;", conn);
            cmd.Parameters.AddWithValue("@Id", deleteId);
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Recruitment record deleted successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error deleting record: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private void SaveRecord(SqlConnection conn, RecruitmentInput input)
    {
        if (input.RecruitmentID > 0)
        {
            using var cmd = new SqlCommand(BuildUpdateSql(), conn);
            BindParams(cmd, input);
            cmd.Parameters.AddWithValue("@RecruitmentID", input.RecruitmentID);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            cmd.ExecuteNonQuery();
            return;
        }

        using var ins = new SqlCommand(BuildInsertSql(), conn);
        BindParams(ins, input);
        AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
        input.RecruitmentID = (int)ins.ExecuteScalar()!;
    }

    private static string BuildInsertSql() => @"
        INSERT INTO tblRecruitment (
            JobRequisitionNumber, RecruitmentSource, PositionTitle, DepartmentID, HiringManagerEmployeeID,
            CandidateName, PersonalEmail, PersonalPhone, ApplicationDate,
            InterviewDate, InterviewStatus, SelectionDate,
            OfferLetterNumber, OfferedSalary, OfferDate, OfferAcceptedDate,
            BackgroundVerificationStatus, ReferenceCheckStatus,
            OnboardingStatus, JoiningDate, InductionCompleted, InductionDate, DocumentsSubmitted,
            SystemAccessProvided, OfficialEmailCreated, EquipmentIssued, AssetDetails,
            TrainingScheduleAssigned, BuddyMentorEmployeeID,
            ProbationPeriod, ProbationReviewSchedule, ConfirmationStatus,
            CreatedOn, CreatedByUserID)
        VALUES (
            @JobRequisitionNumber, @RecruitmentSource, @PositionTitle, @DepartmentID, @HiringManagerEmployeeID,
            @CandidateName, @PersonalEmail, @PersonalPhone, @ApplicationDate,
            @InterviewDate, @InterviewStatus, @SelectionDate,
            @OfferLetterNumber, @OfferedSalary, @OfferDate, @OfferAcceptedDate,
            @BackgroundVerificationStatus, @ReferenceCheckStatus,
            @OnboardingStatus, @JoiningDate, @InductionCompleted, @InductionDate, @DocumentsSubmitted,
            @SystemAccessProvided, @OfficialEmailCreated, @EquipmentIssued, @AssetDetails,
            @TrainingScheduleAssigned, @BuddyMentorEmployeeID,
            @ProbationPeriod, @ProbationReviewSchedule, @ConfirmationStatus,
            GETDATE(), @CreatedByUserID);
        SELECT CAST(SCOPE_IDENTITY() AS INT);";

    private static string BuildUpdateSql() => @"
        UPDATE tblRecruitment SET
            JobRequisitionNumber = @JobRequisitionNumber,
            RecruitmentSource = @RecruitmentSource,
            PositionTitle = @PositionTitle,
            DepartmentID = @DepartmentID,
            HiringManagerEmployeeID = @HiringManagerEmployeeID,
            CandidateName = @CandidateName,
            PersonalEmail = @PersonalEmail,
            PersonalPhone = @PersonalPhone,
            ApplicationDate = @ApplicationDate,
            InterviewDate = @InterviewDate,
            InterviewStatus = @InterviewStatus,
            SelectionDate = @SelectionDate,
            OfferLetterNumber = @OfferLetterNumber,
            OfferedSalary = @OfferedSalary,
            OfferDate = @OfferDate,
            OfferAcceptedDate = @OfferAcceptedDate,
            BackgroundVerificationStatus = @BackgroundVerificationStatus,
            ReferenceCheckStatus = @ReferenceCheckStatus,
            OnboardingStatus = @OnboardingStatus,
            JoiningDate = @JoiningDate,
            InductionCompleted = @InductionCompleted,
            InductionDate = @InductionDate,
            DocumentsSubmitted = @DocumentsSubmitted,
            SystemAccessProvided = @SystemAccessProvided,
            OfficialEmailCreated = @OfficialEmailCreated,
            EquipmentIssued = @EquipmentIssued,
            AssetDetails = @AssetDetails,
            TrainingScheduleAssigned = @TrainingScheduleAssigned,
            BuddyMentorEmployeeID = @BuddyMentorEmployeeID,
            ProbationPeriod = @ProbationPeriod,
            ProbationReviewSchedule = @ProbationReviewSchedule,
            ConfirmationStatus = @ConfirmationStatus,
            ModifiedOn = GETDATE(),
            ModifiedByUserID = @ModifiedByUserID
        WHERE RecruitmentID = @RecruitmentID;";

    private static void BindParams(SqlCommand cmd, RecruitmentInput i)
    {
        cmd.Parameters.AddWithValue("@JobRequisitionNumber", Str(i.JobRequisitionNumber));
        cmd.Parameters.AddWithValue("@RecruitmentSource", Str(i.RecruitmentSource));
        cmd.Parameters.AddWithValue("@PositionTitle", Str(i.PositionTitle));
        cmd.Parameters.AddWithValue("@DepartmentID", Fk(i.DepartmentID));
        cmd.Parameters.AddWithValue("@HiringManagerEmployeeID", Fk(i.HiringManagerEmployeeID));
        cmd.Parameters.AddWithValue("@CandidateName", i.CandidateName.Trim());
        cmd.Parameters.AddWithValue("@PersonalEmail", Str(i.PersonalEmail));
        cmd.Parameters.AddWithValue("@PersonalPhone", Str(i.PersonalPhone));
        cmd.Parameters.AddWithValue("@ApplicationDate", ParseDate(i.ApplicationDate));
        cmd.Parameters.AddWithValue("@InterviewDate", ParseDate(i.InterviewDate));
        cmd.Parameters.AddWithValue("@InterviewStatus", Str(i.InterviewStatus));
        cmd.Parameters.AddWithValue("@SelectionDate", ParseDate(i.SelectionDate));
        cmd.Parameters.AddWithValue("@OfferLetterNumber", Str(i.OfferLetterNumber));
        cmd.Parameters.AddWithValue("@OfferedSalary", ParseDecimal(i.OfferedSalary));
        cmd.Parameters.AddWithValue("@OfferDate", ParseDate(i.OfferDate));
        cmd.Parameters.AddWithValue("@OfferAcceptedDate", ParseDate(i.OfferAcceptedDate));
        cmd.Parameters.AddWithValue("@BackgroundVerificationStatus", Str(i.BackgroundVerificationStatus));
        cmd.Parameters.AddWithValue("@ReferenceCheckStatus", Str(i.ReferenceCheckStatus));
        cmd.Parameters.AddWithValue("@OnboardingStatus", Str(i.OnboardingStatus));
        cmd.Parameters.AddWithValue("@JoiningDate", ParseDate(i.JoiningDate));
        cmd.Parameters.AddWithValue("@InductionCompleted", i.InductionCompleted);
        cmd.Parameters.AddWithValue("@InductionDate", ParseDate(i.InductionDate));
        cmd.Parameters.AddWithValue("@DocumentsSubmitted", i.DocumentsSubmitted);
        cmd.Parameters.AddWithValue("@SystemAccessProvided", i.SystemAccessProvided);
        cmd.Parameters.AddWithValue("@OfficialEmailCreated", Str(i.OfficialEmailCreated));
        cmd.Parameters.AddWithValue("@EquipmentIssued", i.EquipmentIssued);
        cmd.Parameters.AddWithValue("@AssetDetails", Str(i.AssetDetails));
        cmd.Parameters.AddWithValue("@TrainingScheduleAssigned", i.TrainingScheduleAssigned);
        cmd.Parameters.AddWithValue("@BuddyMentorEmployeeID", Fk(i.BuddyMentorEmployeeID));
        cmd.Parameters.AddWithValue("@ProbationPeriod", Str(i.ProbationPeriod));
        cmd.Parameters.AddWithValue("@ProbationReviewSchedule", ParseDate(i.ProbationReviewSchedule));
        cmd.Parameters.AddWithValue("@ConfirmationStatus", Str(i.ConfirmationStatus));
    }

    private void LoadRecords()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT r.RecruitmentID,
                   ISNULL(r.JobRequisitionNumber, '') AS JobRequisitionNumber,
                   ISNULL(r.CandidateName, '') AS CandidateName,
                   ISNULL(r.PositionTitle, '') AS PositionTitle,
                   ISNULL(d.DepartmentName, '') AS DepartmentName,
                   ISNULL(r.InterviewStatus, '') AS InterviewStatus,
                   r.JoiningDate,
                   ISNULL(r.OnboardingStatus, '') AS OnboardingStatus,
                   ISNULL(r.RecruitmentSource, '') AS RecruitmentSource
            FROM tblRecruitment r
            LEFT JOIN tblDepartment d ON d.DepartmentID = r.DepartmentID
            ORDER BY r.RecruitmentID DESC;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Records.Add(new RecruitmentListItem
            {
                RecruitmentID = dr.GetInt32(0),
                JobRequisitionNumber = dr.GetString(1),
                CandidateName = dr.GetString(2),
                PositionTitle = dr.GetString(3),
                DepartmentName = dr.GetString(4),
                InterviewStatus = dr.GetString(5),
                JoiningDate = dr.IsDBNull(6) ? null : dr.GetDateTime(6),
                OnboardingStatus = dr.GetString(7),
                RecruitmentSource = dr.GetString(8)
            });
        }
    }

    private void LoadForEdit(int id)
    {
        using var conn = new SqlConnection(_conn);
        conn.Open();
        using var cmd = new SqlCommand(@"
            SELECT r.RecruitmentID, r.JobRequisitionNumber, r.RecruitmentSource, r.PositionTitle,
                   r.DepartmentID, r.HiringManagerEmployeeID,
                   r.CandidateName, r.PersonalEmail, r.PersonalPhone, r.ApplicationDate,
                   r.InterviewDate, r.InterviewStatus, r.SelectionDate,
                   r.OfferLetterNumber, r.OfferedSalary, r.OfferDate, r.OfferAcceptedDate,
                   r.BackgroundVerificationStatus, r.ReferenceCheckStatus,
                   r.OnboardingStatus, r.JoiningDate, r.InductionCompleted, r.InductionDate, r.DocumentsSubmitted,
                   r.SystemAccessProvided, r.OfficialEmailCreated, r.EquipmentIssued, r.AssetDetails,
                   r.TrainingScheduleAssigned, r.BuddyMentorEmployeeID,
                   r.ProbationPeriod, r.ProbationReviewSchedule, r.ConfirmationStatus,
                   r.CreatedOn, r.ModifiedOn,
                   ISNULL(cu.FullName, cu.Username) AS CreatedByName,
                   ISNULL(mu.FullName, mu.Username) AS ModifiedByName
            FROM tblRecruitment r
            LEFT JOIN tblUser cu ON cu.UserID = r.CreatedByUserID
            LEFT JOIN tblUser mu ON mu.UserID = r.ModifiedByUserID
            WHERE r.RecruitmentID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", id);

        using var dr = cmd.ExecuteReader();
        if (!dr.Read()) return;

        Input = new RecruitmentInput
        {
            RecruitmentID = dr.GetInt32(0),
            JobRequisitionNumber = dr.IsDBNull(1) ? "" : dr.GetString(1),
            RecruitmentSource = dr.IsDBNull(2) ? "" : dr.GetString(2),
            PositionTitle = dr.IsDBNull(3) ? "" : dr.GetString(3),
            DepartmentID = dr.IsDBNull(4) ? 0 : dr.GetInt32(4),
            HiringManagerEmployeeID = dr.IsDBNull(5) ? 0 : dr.GetInt32(5),
            CandidateName = dr.IsDBNull(6) ? "" : dr.GetString(6),
            PersonalEmail = dr.IsDBNull(7) ? "" : dr.GetString(7),
            PersonalPhone = dr.IsDBNull(8) ? "" : dr.GetString(8),
            ApplicationDate = FmtDate(dr, 9),
            InterviewDate = FmtDate(dr, 10),
            InterviewStatus = dr.IsDBNull(11) ? "" : dr.GetString(11),
            SelectionDate = FmtDate(dr, 12),
            OfferLetterNumber = dr.IsDBNull(13) ? "" : dr.GetString(13),
            OfferedSalary = dr.IsDBNull(14) ? "" : dr.GetDecimal(14).ToString("0.##"),
            OfferDate = FmtDate(dr, 15),
            OfferAcceptedDate = FmtDate(dr, 16),
            BackgroundVerificationStatus = dr.IsDBNull(17) ? "" : dr.GetString(17),
            ReferenceCheckStatus = dr.IsDBNull(18) ? "" : dr.GetString(18),
            OnboardingStatus = dr.IsDBNull(19) ? "" : dr.GetString(19),
            JoiningDate = FmtDate(dr, 20),
            InductionCompleted = !dr.IsDBNull(21) && dr.GetBoolean(21),
            InductionDate = FmtDate(dr, 22),
            DocumentsSubmitted = !dr.IsDBNull(23) && dr.GetBoolean(23),
            SystemAccessProvided = !dr.IsDBNull(24) && dr.GetBoolean(24),
            OfficialEmailCreated = dr.IsDBNull(25) ? "" : dr.GetString(25),
            EquipmentIssued = !dr.IsDBNull(26) && dr.GetBoolean(26),
            AssetDetails = dr.IsDBNull(27) ? "" : dr.GetString(27),
            TrainingScheduleAssigned = !dr.IsDBNull(28) && dr.GetBoolean(28),
            BuddyMentorEmployeeID = dr.IsDBNull(29) ? 0 : dr.GetInt32(29),
            ProbationPeriod = dr.IsDBNull(30) ? "" : dr.GetString(30),
            ProbationReviewSchedule = FmtDate(dr, 31),
            ConfirmationStatus = dr.IsDBNull(32) ? "" : dr.GetString(32)
        };

        Audit = new RecruitmentAuditInfo
        {
            CreatedBy = dr.IsDBNull(35) ? "" : dr.GetString(35),
            CreatedDate = dr.IsDBNull(33) ? "" : dr.GetDateTime(33).ToString("dd MMM yyyy HH:mm"),
            UpdatedBy = dr.IsDBNull(36) ? "" : dr.GetString(36),
            UpdatedDate = dr.IsDBNull(34) ? "" : dr.GetDateTime(34).ToString("dd MMM yyyy HH:mm")
        };
    }

    private void LoadLookups()
    {
        Departments = new List<LookupItem>();
        Employees = new List<LookupItem>();

        using var conn = new SqlConnection(_conn);
        conn.Open();

        using (var cmd = new SqlCommand(@"
            SELECT DepartmentID, DepartmentName FROM tblDepartment
            WHERE IsActive = 1 ORDER BY DepartmentName;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
                Departments.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
        }

        using (var cmd = new SqlCommand(@"
            SELECT EmployeeID, EmployeeCode, FirstName, LastName
            FROM tblEmployee WHERE Status = 'Active'
            ORDER BY FirstName, LastName;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
            {
                var code = dr.IsDBNull(1) ? "" : dr.GetString(1);
                var name = $"{dr.GetString(2)} {dr.GetString(3)}".Trim();
                Employees.Add(new LookupItem
                {
                    Id = dr.GetInt32(0),
                    Name = string.IsNullOrEmpty(code) ? name : $"{code} – {name}"
                });
            }
        }
    }

    private void LoadAlert()
    {
        if (TempData["Alert"] is string msg) AlertMessage = msg;
        if (TempData["AlertType"] is string type) AlertType = type;
    }

    private static string FmtDate(SqlDataReader dr, int i) =>
        dr.IsDBNull(i) ? "" : dr.GetDateTime(i).ToString("yyyy-MM-dd");

    private static object Str(string? s) =>
        string.IsNullOrWhiteSpace(s) ? DBNull.Value : s.Trim();

    private static object Fk(int id) => id > 0 ? id : DBNull.Value;

    private static object ParseDate(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : (object)DateTime.Parse(value);

    private static object ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
        return decimal.TryParse(value, out var d) ? d : DBNull.Value;
    }
}
