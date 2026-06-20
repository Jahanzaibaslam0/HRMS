using Microsoft.Data.SqlClient;
using HRMS.Services;
using HRMS.Middleware;
using HRMS.Filters;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.ConfigureFilter(new ServiceFilterAttribute(typeof(AuditPageFilter)));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<AuditPageFilter>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<MemorandumService>();

var app = builder.Build();

// ── Auto-migrate: create any missing tables on startup ──────────────────────
RunStartupMigrations(app.Configuration);
SeedAppData(app.Configuration);

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseMiddleware<LoginRequiredMiddleware>();
app.MapRazorPages();

// Redirect root to the Employee Master page
app.MapGet("/", () => Results.Redirect("/EmployeeMaster"));

app.Run();
// ── Startup migration helper ─────────────────────────────────────────────────
static void RunStartupMigrations(IConfiguration config)
{
    var conn = config.GetConnectionString("HRMSConnection");
    if (string.IsNullOrWhiteSpace(conn)) return;

    var migrations = new[]
    {
        // tblSkill
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblSkill' AND type = 'U')
          CREATE TABLE tblSkill (
              SkillID        INT IDENTITY(1,1) PRIMARY KEY,
              SkillCode      NVARCHAR(20)  NOT NULL UNIQUE,
              SkillName      NVARCHAR(150) NOT NULL,
              FieldType      NVARCHAR(50)  NULL,
              DefaultTier    NVARCHAR(50)  NULL,
              Description    NVARCHAR(500) NULL,
              ESCOAnchor     NVARCHAR(200) NULL,
              RoleCoverage   NVARCHAR(200) NULL,
              EmployeeNeed   NVARCHAR(50)  NULL,
              IsActive       BIT           NOT NULL DEFAULT 1,
              CreatedOn      DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn     DATETIME      NULL
          );",

        // tblWorkerCategory
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblWorkerCategory' AND type = 'U')
          CREATE TABLE tblWorkerCategory (
              WorkerCategoryID    INT IDENTITY(1,1) PRIMARY KEY,
              WorkerCategoryCode  NVARCHAR(20)  NOT NULL UNIQUE,
              WorkerCategoryName  NVARCHAR(150) NOT NULL,
              AliasName           NVARCHAR(100) NULL,
              Description         NVARCHAR(500) NULL,
              IsActive            BIT           NOT NULL DEFAULT 1,
              CreatedOn           DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn          DATETIME      NULL
          );",

        // tblLegalEntity
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblLegalEntity' AND type = 'U')
          CREATE TABLE tblLegalEntity (
              LegalEntityID    INT IDENTITY(1,1) PRIMARY KEY,
              LegalEntityCode  NVARCHAR(20)  NOT NULL UNIQUE,
              LegalEntityName  NVARCHAR(150) NOT NULL,
              AliasName        NVARCHAR(100) NULL,
              Description      NVARCHAR(500) NULL,
              IsActive         BIT           NOT NULL DEFAULT 1,
              CreatedOn        DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn       DATETIME      NULL
          );",

        // tblSalesTeam
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblSalesTeam' AND type = 'U')
          CREATE TABLE tblSalesTeam (
              SalesTeamID    INT IDENTITY(1,1) PRIMARY KEY,
              SalesTeamCode  NVARCHAR(20)  NOT NULL UNIQUE,
              SalesTeamName  NVARCHAR(150) NOT NULL,
              DivisionID     INT           NULL,
              AliasName      NVARCHAR(100) NULL,
              Description    NVARCHAR(500) NULL,
              IsActive       BIT           NOT NULL DEFAULT 1,
              CreatedOn      DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn     DATETIME      NULL
          );",

        // tblBenefit – standalone lookup (no FK to entitlement)
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblBenefit' AND type = 'U')
          CREATE TABLE tblBenefit (
              BenefitID    INT IDENTITY(1,1) PRIMARY KEY,
              BenefitCode  NVARCHAR(20)  NULL,
              BenefitName  NVARCHAR(150) NOT NULL,
              BenefitType  NVARCHAR(50)  NULL,
              Description  NVARCHAR(500) NULL,
              IsActive     BIT           NOT NULL DEFAULT 1,
              CreatedOn    DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn   DATETIME      NULL
          );",

        // If tblBenefit already existed with old schema, make BenefitEntitlementID nullable
        @"IF COL_LENGTH('tblBenefit','BenefitEntitlementID') IS NOT NULL AND
             COLUMNPROPERTY(OBJECT_ID('tblBenefit'),'BenefitEntitlementID','AllowsNull') = 0
              ALTER TABLE tblBenefit ALTER COLUMN BenefitEntitlementID INT NULL;",

        // Add BenefitCode column if missing
        @"IF COL_LENGTH('tblBenefit','BenefitCode') IS NULL
              ALTER TABLE tblBenefit ADD BenefitCode NVARCHAR(20) NULL;",

        // tblBenefitEntitlementDetail – junction table (entitlement ↔ benefit)
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblBenefitEntitlementDetail' AND type = 'U')
          CREATE TABLE tblBenefitEntitlementDetail (
              DetailID              INT IDENTITY(1,1) PRIMARY KEY,
              BenefitEntitlementID  INT NOT NULL,
              BenefitID             INT NOT NULL,
              CreatedOn             DATETIME NOT NULL DEFAULT GETDATE()
          );",

        // tblWorkforceSegment
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblWorkforceSegment' AND type = 'U')
          CREATE TABLE tblWorkforceSegment (
              WorkforceSegmentID    INT IDENTITY(1,1) PRIMARY KEY,
              WorkforceSegmentName  NVARCHAR(100) NOT NULL UNIQUE,
              AliasName             NVARCHAR(50)  NULL,
              IsActive              BIT           NOT NULL DEFAULT 1,
              CreatedOn             DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn            DATETIME      NULL
          );",

        // tblBusinessUnit
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblBusinessUnit' AND type = 'U')
          CREATE TABLE tblBusinessUnit (
              BusinessUnitID    INT IDENTITY(1,1) PRIMARY KEY,
              BusinessUnitName  NVARCHAR(100) NOT NULL UNIQUE,
              AliasName         NVARCHAR(50)  NULL,
              IsActive          BIT           NOT NULL DEFAULT 1,
              CreatedOn         DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn        DATETIME      NULL
          );",

        // ── New FK columns on tblEmployee ────────────────────────────────
        @"IF COL_LENGTH('tblEmployee','NationalityID')       IS NULL ALTER TABLE tblEmployee ADD NationalityID       INT NULL;",
        @"IF COL_LENGTH('tblEmployee','ReligionID')          IS NULL ALTER TABLE tblEmployee ADD ReligionID          INT NULL;",
        @"IF COL_LENGTH('tblEmployee','LanguageID')          IS NULL ALTER TABLE tblEmployee ADD LanguageID          INT NULL;",
        @"IF COL_LENGTH('tblEmployee','WorkerCategoryID')    IS NULL ALTER TABLE tblEmployee ADD WorkerCategoryID    INT NULL;",
        @"IF COL_LENGTH('tblEmployee','EmploymentTypeID')    IS NULL ALTER TABLE tblEmployee ADD EmploymentTypeID    INT NULL;",
        @"IF COL_LENGTH('tblEmployee','EmploymentStatusID')  IS NULL ALTER TABLE tblEmployee ADD EmploymentStatusID  INT NULL;",
        @"IF COL_LENGTH('tblEmployee','WorkforceSegmentID')  IS NULL ALTER TABLE tblEmployee ADD WorkforceSegmentID  INT NULL;",
        @"IF COL_LENGTH('tblEmployee','LegalEntityID')       IS NULL ALTER TABLE tblEmployee ADD LegalEntityID       INT NULL;",
        @"IF COL_LENGTH('tblEmployee','BusinessUnitID')      IS NULL ALTER TABLE tblEmployee ADD BusinessUnitID      INT NULL;",
        @"IF COL_LENGTH('tblEmployee','DivisionID')          IS NULL ALTER TABLE tblEmployee ADD DivisionID          INT NULL;",
        @"IF COL_LENGTH('tblEmployee','SalesTeamID')         IS NULL ALTER TABLE tblEmployee ADD SalesTeamID         INT NULL;",
        @"IF COL_LENGTH('tblEmployee','CostCenterID')        IS NULL ALTER TABLE tblEmployee ADD CostCenterID        INT NULL;",
        @"IF COL_LENGTH('tblEmployee','MaritalStatus')       IS NULL ALTER TABLE tblEmployee ADD MaritalStatus       NVARCHAR(20) NULL;",
        @"IF COL_LENGTH('tblEmployee','UserID')             IS NULL ALTER TABLE tblEmployee ADD UserID               INT NULL;",

        // Worker joining & tenure columns on tblEmployee
        @"IF COL_LENGTH('tblEmployee','EmploymentStartDate') IS NULL ALTER TABLE tblEmployee ADD EmploymentStartDate DATE NULL;",
        @"IF COL_LENGTH('tblEmployee','ProbationPeriodDays') IS NULL ALTER TABLE tblEmployee ADD ProbationPeriodDays INT NULL;",
        @"IF COL_LENGTH('tblEmployee','ProbationEndDate')   IS NULL ALTER TABLE tblEmployee ADD ProbationEndDate   DATE NULL;",
        @"IF COL_LENGTH('tblEmployee','ConfirmationDate')    IS NULL ALTER TABLE tblEmployee ADD ConfirmationDate    DATE NULL;",

        // Primary identifier columns on tblEmployee
        @"IF COL_LENGTH('tblEmployee','FathersHusbandsName') IS NULL ALTER TABLE tblEmployee ADD FathersHusbandsName NVARCHAR(150) NULL;",
        @"IF COL_LENGTH('tblEmployee','DisplayName')         IS NULL ALTER TABLE tblEmployee ADD DisplayName         NVARCHAR(200) NULL;",
        @"IF COL_LENGTH('tblEmployee','NationalIDNumber')    IS NULL ALTER TABLE tblEmployee ADD NationalIDNumber    NVARCHAR(50)  NULL;",

        // Organization assignment & demographic columns on tblEmployee
        @"IF COL_LENGTH('tblEmployee','RegionID')           IS NULL ALTER TABLE tblEmployee ADD RegionID           INT NULL;",
        @"IF COL_LENGTH('tblEmployee','LocationID')         IS NULL ALTER TABLE tblEmployee ADD LocationID         INT NULL;",
        @"IF COL_LENGTH('tblEmployee','JobID')              IS NULL ALTER TABLE tblEmployee ADD JobID              INT NULL;",
        @"IF COL_LENGTH('tblEmployee','WorkerLocationID')   IS NULL ALTER TABLE tblEmployee ADD WorkerLocationID   INT NULL;",
        @"IF COL_LENGTH('tblEmployee','Domicile')           IS NULL ALTER TABLE tblEmployee ADD Domicile           NVARCHAR(150) NULL;",
        @"IF COL_LENGTH('tblEmployee','CityID')             IS NULL ALTER TABLE tblEmployee ADD CityID             INT NULL;",
        @"IF COL_LENGTH('tblEmployee','ProvinceID')         IS NULL ALTER TABLE tblEmployee ADD ProvinceID         INT NULL;",
        @"IF COL_LENGTH('tblEmployee','SalesGroupID')      IS NULL ALTER TABLE tblEmployee ADD SalesGroupID      INT NULL;",
        @"IF COL_LENGTH('tblEmployee','GradeID')           IS NULL ALTER TABLE tblEmployee ADD GradeID           INT NULL;",
        @"IF COL_LENGTH('tblEmployee','ExtensionID')        IS NULL ALTER TABLE tblEmployee ADD ExtensionID        INT NULL;",

        // Ensure AliasName & ModifiedOn exist on tblLanguage (added later)
        @"IF COL_LENGTH('tblLanguage','ModifiedOn') IS NULL
              ALTER TABLE tblLanguage ADD ModifiedOn DATETIME NULL;",

        // Ensure ModifiedOn exists on tblDepartment
        @"IF COL_LENGTH('tblDepartment','ModifiedOn') IS NULL
              ALTER TABLE tblDepartment ADD ModifiedOn DATETIME NULL;",

        // Department lookup FK columns
        @"IF COL_LENGTH('tblDepartment','WingID') IS NULL
              ALTER TABLE tblDepartment ADD WingID INT NULL;",
        @"IF COL_LENGTH('tblDepartment','BusinessSegmentID') IS NULL
              ALTER TABLE tblDepartment ADD BusinessSegmentID INT NULL;",
        @"IF COL_LENGTH('tblDepartment','BusinessUnitID') IS NULL
              ALTER TABLE tblDepartment ADD BusinessUnitID INT NULL;",
        @"IF COL_LENGTH('tblDepartment','DepartmentManagerID') IS NULL
              ALTER TABLE tblDepartment ADD DepartmentManagerID INT NULL;",

        // tblJob
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblJob' AND type = 'U')
          CREATE TABLE tblJob (
              JobID                        INT IDENTITY(1,1) PRIMARY KEY,
              JobTitle                     NVARCHAR(100) NOT NULL,
              JobCode                      NVARCHAR(20)  NOT NULL UNIQUE,
              GradeID                      INT           NULL,
              JobLevel                     NVARCHAR(50)  NOT NULL,
              PositionNumber               NVARCHAR(20)  NOT NULL UNIQUE,
              ReportsToEmployeeID          INT           NULL,
              FunctionalManagerEmployeeID  INT           NULL,
              DottedLineManagerEmployeeID  INT           NULL,
              BackupApproverEmployeeID     INT           NULL,
              IsActive                     BIT           NOT NULL DEFAULT 1,
              CreatedOn                    DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn                   DATETIME      NULL
          );",

        // tblWorkLocationType
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblWorkLocationType' AND type = 'U')
          CREATE TABLE tblWorkLocationType (
              WorkLocationTypeID   INT IDENTITY(1,1) PRIMARY KEY,
              WorkLocationTypeCode NVARCHAR(20)  NOT NULL UNIQUE,
              WorkLocationTypeName NVARCHAR(150) NOT NULL,
              AliasName            NVARCHAR(100) NULL,
              IsActive             BIT           NOT NULL DEFAULT 1,
              CreatedOn            DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn           DATETIME      NULL
          );",

        // tblWorkArrangement
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblWorkArrangement' AND type = 'U')
          CREATE TABLE tblWorkArrangement (
              WorkArrangementID   INT IDENTITY(1,1) PRIMARY KEY,
              WorkArrangementCode NVARCHAR(20)  NOT NULL UNIQUE,
              WorkArrangementName NVARCHAR(150) NOT NULL,
              AliasName           NVARCHAR(100) NULL,
              IsActive            BIT           NOT NULL DEFAULT 1,
              CreatedOn           DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn          DATETIME      NULL
          );",

        // tblExtension
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblExtension' AND type = 'U')
          CREATE TABLE tblExtension (
              ExtensionID   INT IDENTITY(1,1) PRIMARY KEY,
              ExtensionCode NVARCHAR(20)  NOT NULL UNIQUE,
              ExtensionName NVARCHAR(150) NOT NULL,
              AliasName     NVARCHAR(100) NULL,
              DepartmentID  INT           NULL,
              LocationID    INT           NULL,
              IsActive      BIT           NOT NULL DEFAULT 1,
              CreatedOn     DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn    DATETIME      NULL
          );",

        @"IF COL_LENGTH('tblExtension','DepartmentID') IS NULL ALTER TABLE tblExtension ADD DepartmentID INT NULL;",
        @"IF COL_LENGTH('tblExtension','LocationID')   IS NULL ALTER TABLE tblExtension ADD LocationID   INT NULL;",

        // tblCity
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblCity' AND type = 'U')
          CREATE TABLE tblCity (
              CityID     INT IDENTITY(1,1) PRIMARY KEY,
              CityCode   NVARCHAR(20)  NOT NULL UNIQUE,
              CityName   NVARCHAR(150) NOT NULL,
              AliasName  NVARCHAR(100) NULL,
              IsActive   BIT           NOT NULL DEFAULT 1,
              CreatedOn  DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn DATETIME      NULL
          );",

        // tblProvince
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblProvince' AND type = 'U')
          CREATE TABLE tblProvince (
              ProvinceID   INT IDENTITY(1,1) PRIMARY KEY,
              ProvinceCode NVARCHAR(20)  NOT NULL UNIQUE,
              ProvinceName NVARCHAR(150) NOT NULL,
              AliasName    NVARCHAR(100) NULL,
              IsActive     BIT           NOT NULL DEFAULT 1,
              CreatedOn    DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn   DATETIME      NULL
          );",

        // tblSalesGroup
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblSalesGroup' AND type = 'U')
          CREATE TABLE tblSalesGroup (
              SalesGroupID   INT IDENTITY(1,1) PRIMARY KEY,
              SalesGroupCode NVARCHAR(20)  NOT NULL UNIQUE,
              SalesGroupName NVARCHAR(150) NOT NULL,
              AliasName      NVARCHAR(100) NULL,
              IsActive       BIT           NOT NULL DEFAULT 1,
              CreatedOn      DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn     DATETIME      NULL
          );",

        // tblWorkerLocation
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblWorkerLocation' AND type = 'U')
          CREATE TABLE tblWorkerLocation (
              WorkerLocationID          INT IDENTITY(1,1) PRIMARY KEY,
              EmployeeID                INT           NOT NULL UNIQUE,
              PrimaryLocationID         INT           NOT NULL,
              SecondaryLocationID       INT           NULL,
              WorkLocationTypeID        INT           NULL,
              WorkArrangementID         INT           NULL,
              HybridSchedule            NVARCHAR(500) NULL,
              TerritoryRegionAssignment NVARCHAR(500) NULL,
              ClientSiteAccess          NVARCHAR(500) NULL,
              IsActive                  BIT           NOT NULL DEFAULT 1,
              CreatedOn                 DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn                DATETIME      NULL
          );",

        // tblAppForm – registry of permission-controlled forms
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblAppForm' AND type = 'U')
          CREATE TABLE tblAppForm (
              FormID     INT IDENTITY(1,1) PRIMARY KEY,
              FormKey    NVARCHAR(80)  NOT NULL UNIQUE,
              FormName   NVARCHAR(150) NOT NULL,
              PagePath   NVARCHAR(200) NOT NULL,
              Category   NVARCHAR(80)  NOT NULL,
              SortOrder  INT           NOT NULL DEFAULT 0,
              IsActive   BIT           NOT NULL DEFAULT 1
          );",

        // tblUser
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblUser' AND type = 'U')
          CREATE TABLE tblUser (
              UserID       INT IDENTITY(1,1) PRIMARY KEY,
              UserCode     NVARCHAR(20)  NULL,
              Username     NVARCHAR(50)  NOT NULL UNIQUE,
              PasswordHash NVARCHAR(200) NOT NULL,
              FullName     NVARCHAR(100) NOT NULL,
              Email        NVARCHAR(100) NULL,
              IsActive     BIT           NOT NULL DEFAULT 1,
              IsAdmin      BIT           NOT NULL DEFAULT 0,
              CreatedOn    DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn   DATETIME      NULL
          );",

        // tblUserPermission
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblUserPermission' AND type = 'U')
          CREATE TABLE tblUserPermission (
              UserPermissionID INT IDENTITY(1,1) PRIMARY KEY,
              UserID           INT NOT NULL,
              FormKey          NVARCHAR(80) NOT NULL,
              CanRead          BIT NOT NULL DEFAULT 0,
              CanWrite         BIT NOT NULL DEFAULT 0,
              CanDelete        BIT NOT NULL DEFAULT 0,
              CONSTRAINT UQ_UserForm UNIQUE (UserID, FormKey)
          );",

        // tblExpense – expense claim header (parent)
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblExpense' AND type = 'U')
          CREATE TABLE tblExpense (
              ExpenseID        INT IDENTITY(1,1) PRIMARY KEY,
              EmployeeID       INT           NOT NULL,
              ExpenseDate      DATE          NULL,
              LocationID       INT           NULL,
              ExpensePurpose   NVARCHAR(500) NULL,
              WorkflowStatus   NVARCHAR(50)  NOT NULL DEFAULT 'Draft',
              VehicleNo        NVARCHAR(50)  NULL,
              MeterReading     NVARCHAR(50)  NULL,
              DocumentStatus   NVARCHAR(50)  NULL,
              CreatedOn        DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn       DATETIME      NULL,
              CreatedByUserID  INT           NULL,
              ModifiedByUserID INT           NULL
          );",

        // tblExpenseDetail – expense line items (child)
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblExpenseDetail' AND type = 'U')
          CREATE TABLE tblExpenseDetail (
              DetailID               INT IDENTITY(1,1) PRIMARY KEY,
              ExpenseID              INT           NOT NULL,
              ExpenseCategoryID      INT           NULL,
              Description            NVARCHAR(500) NULL,
              PaymentMethod          NVARCHAR(50)  NULL,
              TransactionDate        DATE          NULL,
              Currency               NVARCHAR(10)  NULL DEFAULT 'PKR',
              TransactionAmount      DECIMAL(18,2) NULL,
              Amount                 DECIMAL(18,2) NULL,
              ApprovalStatus         NVARCHAR(50)  NULL DEFAULT 'Pending',
              OriginalReceiptID      NVARCHAR(100) NULL,
              OriginalReceiptDocPath NVARCHAR(500) NULL,
              SortOrder              INT           NOT NULL DEFAULT 0,
              CreatedOn              DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn             DATETIME      NULL,
              CreatedByUserID        INT           NULL,
              ModifiedByUserID       INT           NULL
          );",

        // tblEmployeeEducation – employee education records (child)
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmployeeEducation' AND type = 'U')
          CREATE TABLE tblEmployeeEducation (
              EducationID            INT IDENTITY(1,1) PRIMARY KEY,
              EmployeeID             INT           NOT NULL,
              HighestQualification   NVARCHAR(100) NULL,
              DegreeCertificate      NVARCHAR(150) NULL,
              Specialization         NVARCHAR(150) NULL,
              Institution            NVARCHAR(200) NULL,
              YearOfPassing          INT           NULL,
              GradeCGPA              NVARCHAR(20)  NULL,
              SortOrder              INT           NOT NULL DEFAULT 1,
              CreatedOn              DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn             DATETIME      NULL,
              CreatedByUserID        INT           NULL,
              ModifiedByUserID       INT           NULL
          );",

        // tblSoftwareLink – organization software quick links
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblSoftwareLink' AND type = 'U')
          CREATE TABLE tblSoftwareLink (
              SoftwareLinkID   INT IDENTITY(1,1) PRIMARY KEY,
              SoftwareName     NVARCHAR(100) NOT NULL,
              SoftwareUrl      NVARCHAR(500) NOT NULL,
              Category         NVARCHAR(50)  NULL,
              Description      NVARCHAR(250) NULL,
              SortOrder        INT           NOT NULL DEFAULT 1,
              IsActive         BIT           NOT NULL DEFAULT 1,
              CreatedOn        DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn       DATETIME      NULL,
              CreatedByUserID  INT           NULL,
              ModifiedByUserID INT           NULL
          );",

        // tblDocumentType – document type lookup
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblDocumentType' AND type = 'U')
          CREATE TABLE tblDocumentType (
              DocumentTypeID   INT IDENTITY(1,1) PRIMARY KEY,
              DocumentTypeName NVARCHAR(100) NOT NULL UNIQUE,
              AliasName        NVARCHAR(50)  NULL,
              IsActive         BIT           NOT NULL DEFAULT 1,
              CreatedOn        DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn       DATETIME      NULL,
              CreatedByUserID  INT           NULL,
              ModifiedByUserID INT           NULL
          );",

        // tblEmployeeDocument – employee document uploads (child)
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmployeeDocument' AND type = 'U')
          CREATE TABLE tblEmployeeDocument (
              EmployeeDocumentID INT IDENTITY(1,1) PRIMARY KEY,
              EmployeeID         INT           NOT NULL,
              DocumentTypeID     INT           NULL,
              DocumentNumber     NVARCHAR(100) NULL,
              IssueDate          DATE          NULL,
              ExpiryDate         DATE          NULL,
              Remarks            NVARCHAR(250) NULL,
              DocumentPath       NVARCHAR(500) NULL,
              OriginalFileName   NVARCHAR(255) NULL,
              VerificationStatus NVARCHAR(20)  NOT NULL DEFAULT 'Pending',
              VerifiedOn         DATETIME      NULL,
              VerifiedByUserID   INT           NULL,
              SortOrder          INT           NOT NULL DEFAULT 1,
              CreatedOn          DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn         DATETIME      NULL,
              CreatedByUserID    INT           NULL,
              ModifiedByUserID   INT           NULL
          );",

        // tblEmployeeCertificate – employee certification records (child)
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmployeeCertificate' AND type = 'U')
          CREATE TABLE tblEmployeeCertificate (
              CertificateID        INT IDENTITY(1,1) PRIMARY KEY,
              EmployeeID           INT           NOT NULL,
              CertificationName    NVARCHAR(200) NULL,
              CertificationBody    NVARCHAR(200) NULL,
              CertificateNumber    NVARCHAR(100) NULL,
              IssueDate            DATE          NULL,
              ExpiryDate           DATE          NULL,
              RenewalRequired      BIT           NOT NULL DEFAULT 0,
              CertificateCopyPath  NVARCHAR(500) NULL,
              SortOrder            INT           NOT NULL DEFAULT 1,
              CreatedOn            DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn           DATETIME      NULL,
              CreatedByUserID      INT           NULL,
              ModifiedByUserID     INT           NULL
          );",

        // tblEmployeePerformance – employee performance review records
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmployeePerformance' AND type = 'U')
          CREATE TABLE tblEmployeePerformance (
              PerformanceID                INT IDENTITY(1,1) PRIMARY KEY,
              EmployeeID                   INT           NOT NULL,
              PerformanceReviewCycle       NVARCHAR(50)  NULL,
              LastReviewDate               DATE          NULL,
              LastReviewRating             NVARCHAR(50)  NULL,
              LastReviewScore              DECIMAL(5,2)  NULL,
              NextReviewDue                DATE          NULL,
              KPIsAssigned                 BIT           NOT NULL DEFAULT 0,
              GoalAchievementPercent       DECIMAL(5,2)  NULL,
              PerformanceImprovementPlan   BIT           NOT NULL DEFAULT 0,
              CareerPath                   NVARCHAR(100) NULL,
              PromotionReady               BIT           NOT NULL DEFAULT 0,
              SuccessionPool               BIT           NOT NULL DEFAULT 0,
              CreatedOn                    DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn                   DATETIME      NULL,
              CreatedByUserID              INT           NULL,
              ModifiedByUserID             INT           NULL
          );",

        // tblEmployeeTraining – employee training records
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmployeeTraining' AND type = 'U')
          CREATE TABLE tblEmployeeTraining (
              EmployeeTrainingID           INT IDENTITY(1,1) PRIMARY KEY,
              EmployeeID                   INT           NOT NULL,
              MandatoryTrainingStatus      NVARCHAR(50)  NULL,
              SafetyTrainingValidTill      DATE          NULL,
              GMPTrainingValidTill         DATE          NULL,
              TrainingHoursYTD             DECIMAL(8,2)  NULL,
              TrainingHoursRequiredAnnual  DECIMAL(8,2)  NULL,
              LastTrainingDate             DATE          NULL,
              NextTrainingDue              DATE          NULL,
              TrainingName                 NVARCHAR(200) NULL,
              TrainingCode                 NVARCHAR(50)  NULL,
              TrainingDepartment           NVARCHAR(150) NOT NULL DEFAULT 'All',
              CreatedOn                    DATETIME      NOT NULL DEFAULT GETDATE(),
              ModifiedOn                   DATETIME      NULL,
              CreatedByUserID              INT           NULL,
              ModifiedByUserID             INT           NULL
          );",

        // tblRecruitment – end-to-end recruitment / onboarding record
        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblRecruitment' AND type = 'U')
          CREATE TABLE tblRecruitment (
              RecruitmentID                INT IDENTITY(1,1) PRIMARY KEY,
              JobRequisitionNumber         NVARCHAR(50)   NULL,
              RecruitmentSource            NVARCHAR(80)   NULL,
              PositionTitle                NVARCHAR(200)  NULL,
              DepartmentID                 INT            NULL,
              HiringManagerEmployeeID      INT            NULL,
              CandidateName                NVARCHAR(200)  NOT NULL,
              PersonalEmail                NVARCHAR(150)  NULL,
              PersonalPhone                NVARCHAR(30)   NULL,
              ApplicationDate              DATE           NULL,
              InterviewDate                DATE           NULL,
              InterviewStatus              NVARCHAR(30)   NULL,
              SelectionDate                DATE           NULL,
              OfferLetterNumber            NVARCHAR(50)   NULL,
              OfferedSalary                DECIMAL(18,2)  NULL,
              OfferDate                    DATE           NULL,
              OfferAcceptedDate            DATE           NULL,
              BackgroundVerificationStatus NVARCHAR(50)   NULL,
              ReferenceCheckStatus         NVARCHAR(50)   NULL,
              OnboardingStatus             NVARCHAR(50)   NULL,
              JoiningDate                  DATE           NULL,
              InductionCompleted           BIT            NOT NULL DEFAULT 0,
              InductionDate                DATE           NULL,
              DocumentsSubmitted           BIT            NOT NULL DEFAULT 0,
              SystemAccessProvided         BIT            NOT NULL DEFAULT 0,
              OfficialEmailCreated         NVARCHAR(150)  NULL,
              EquipmentIssued              BIT            NOT NULL DEFAULT 0,
              AssetDetails                 NVARCHAR(500)  NULL,
              TrainingScheduleAssigned     BIT            NOT NULL DEFAULT 0,
              BuddyMentorEmployeeID        INT            NULL,
              ProbationPeriod              NVARCHAR(30)   NULL,
              ProbationReviewSchedule      DATE           NULL,
              ConfirmationStatus           NVARCHAR(50)   NULL,
              CreatedOn                    DATETIME       NOT NULL DEFAULT GETDATE(),
              ModifiedOn                   DATETIME       NULL,
              CreatedByUserID              INT            NULL,
              ModifiedByUserID             INT            NULL
          );",

        // ── Audit columns (CreatedByUserID, ModifiedByUserID, CreatedOn, ModifiedOn) ──
        @"IF COL_LENGTH('tblDivision','CreatedByUserID') IS NULL ALTER TABLE tblDivision ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblDivision','ModifiedByUserID') IS NULL ALTER TABLE tblDivision ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblDivision','CreatedOn') IS NULL ALTER TABLE tblDivision ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblDivision','ModifiedOn') IS NULL ALTER TABLE tblDivision ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblDepartment','CreatedByUserID') IS NULL ALTER TABLE tblDepartment ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblDepartment','ModifiedByUserID') IS NULL ALTER TABLE tblDepartment ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblDepartment','CreatedOn') IS NULL ALTER TABLE tblDepartment ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblDepartment','ModifiedOn') IS NULL ALTER TABLE tblDepartment ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblEmployee','CreatedByUserID') IS NULL ALTER TABLE tblEmployee ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblEmployee','ModifiedByUserID') IS NULL ALTER TABLE tblEmployee ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblEmployee','CreatedOn') IS NULL ALTER TABLE tblEmployee ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblEmployee','ModifiedOn') IS NULL ALTER TABLE tblEmployee ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblGender','CreatedByUserID') IS NULL ALTER TABLE tblGender ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblGender','ModifiedByUserID') IS NULL ALTER TABLE tblGender ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblGender','CreatedOn') IS NULL ALTER TABLE tblGender ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblGender','ModifiedOn') IS NULL ALTER TABLE tblGender ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblBloodGroup','CreatedByUserID') IS NULL ALTER TABLE tblBloodGroup ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblBloodGroup','ModifiedByUserID') IS NULL ALTER TABLE tblBloodGroup ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblBloodGroup','CreatedOn') IS NULL ALTER TABLE tblBloodGroup ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblBloodGroup','ModifiedOn') IS NULL ALTER TABLE tblBloodGroup ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblBenefitEntitlement','CreatedByUserID') IS NULL ALTER TABLE tblBenefitEntitlement ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblBenefitEntitlement','ModifiedByUserID') IS NULL ALTER TABLE tblBenefitEntitlement ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblBenefitEntitlement','CreatedOn') IS NULL ALTER TABLE tblBenefitEntitlement ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblBenefitEntitlement','ModifiedOn') IS NULL ALTER TABLE tblBenefitEntitlement ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblExpenseCategory','CreatedByUserID') IS NULL ALTER TABLE tblExpenseCategory ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblExpenseCategory','ModifiedByUserID') IS NULL ALTER TABLE tblExpenseCategory ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblExpenseCategory','CreatedOn') IS NULL ALTER TABLE tblExpenseCategory ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblExpenseCategory','ModifiedOn') IS NULL ALTER TABLE tblExpenseCategory ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblBankMaster','CreatedByUserID') IS NULL ALTER TABLE tblBankMaster ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblBankMaster','ModifiedByUserID') IS NULL ALTER TABLE tblBankMaster ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblBankMaster','CreatedOn') IS NULL ALTER TABLE tblBankMaster ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblBankMaster','ModifiedOn') IS NULL ALTER TABLE tblBankMaster ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblReligion','CreatedByUserID') IS NULL ALTER TABLE tblReligion ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblReligion','ModifiedByUserID') IS NULL ALTER TABLE tblReligion ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblReligion','CreatedOn') IS NULL ALTER TABLE tblReligion ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblReligion','ModifiedOn') IS NULL ALTER TABLE tblReligion ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblNationality','CreatedByUserID') IS NULL ALTER TABLE tblNationality ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblNationality','ModifiedByUserID') IS NULL ALTER TABLE tblNationality ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblNationality','CreatedOn') IS NULL ALTER TABLE tblNationality ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblNationality','ModifiedOn') IS NULL ALTER TABLE tblNationality ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblLanguage','CreatedByUserID') IS NULL ALTER TABLE tblLanguage ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblLanguage','ModifiedByUserID') IS NULL ALTER TABLE tblLanguage ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblLanguage','CreatedOn') IS NULL ALTER TABLE tblLanguage ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblLanguage','ModifiedOn') IS NULL ALTER TABLE tblLanguage ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblUnit','CreatedByUserID') IS NULL ALTER TABLE tblUnit ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblUnit','ModifiedByUserID') IS NULL ALTER TABLE tblUnit ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblUnit','CreatedOn') IS NULL ALTER TABLE tblUnit ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblUnit','ModifiedOn') IS NULL ALTER TABLE tblUnit ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblLocation','CreatedByUserID') IS NULL ALTER TABLE tblLocation ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblLocation','ModifiedByUserID') IS NULL ALTER TABLE tblLocation ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblLocation','CreatedOn') IS NULL ALTER TABLE tblLocation ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblLocation','ModifiedOn') IS NULL ALTER TABLE tblLocation ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblRegion','CreatedByUserID') IS NULL ALTER TABLE tblRegion ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblRegion','ModifiedByUserID') IS NULL ALTER TABLE tblRegion ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblRegion','CreatedOn') IS NULL ALTER TABLE tblRegion ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblRegion','ModifiedOn') IS NULL ALTER TABLE tblRegion ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblWing','CreatedByUserID') IS NULL ALTER TABLE tblWing ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblWing','ModifiedByUserID') IS NULL ALTER TABLE tblWing ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblWing','CreatedOn') IS NULL ALTER TABLE tblWing ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblWing','ModifiedOn') IS NULL ALTER TABLE tblWing ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblGrade','CreatedByUserID') IS NULL ALTER TABLE tblGrade ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblGrade','ModifiedByUserID') IS NULL ALTER TABLE tblGrade ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblGrade','CreatedOn') IS NULL ALTER TABLE tblGrade ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblGrade','ModifiedOn') IS NULL ALTER TABLE tblGrade ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblEmploymentType','CreatedByUserID') IS NULL ALTER TABLE tblEmploymentType ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblEmploymentType','ModifiedByUserID') IS NULL ALTER TABLE tblEmploymentType ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblEmploymentType','CreatedOn') IS NULL ALTER TABLE tblEmploymentType ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblEmploymentType','ModifiedOn') IS NULL ALTER TABLE tblEmploymentType ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblDesignationLevel','CreatedByUserID') IS NULL ALTER TABLE tblDesignationLevel ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblDesignationLevel','ModifiedByUserID') IS NULL ALTER TABLE tblDesignationLevel ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblDesignationLevel','CreatedOn') IS NULL ALTER TABLE tblDesignationLevel ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblDesignationLevel','ModifiedOn') IS NULL ALTER TABLE tblDesignationLevel ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblCostCenter','CreatedByUserID') IS NULL ALTER TABLE tblCostCenter ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblCostCenter','ModifiedByUserID') IS NULL ALTER TABLE tblCostCenter ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblCostCenter','CreatedOn') IS NULL ALTER TABLE tblCostCenter ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblCostCenter','ModifiedOn') IS NULL ALTER TABLE tblCostCenter ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblEmploymentStatus','CreatedByUserID') IS NULL ALTER TABLE tblEmploymentStatus ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblEmploymentStatus','ModifiedByUserID') IS NULL ALTER TABLE tblEmploymentStatus ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblEmploymentStatus','CreatedOn') IS NULL ALTER TABLE tblEmploymentStatus ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblEmploymentStatus','ModifiedOn') IS NULL ALTER TABLE tblEmploymentStatus ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblBusinessSegment','CreatedByUserID') IS NULL ALTER TABLE tblBusinessSegment ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblBusinessSegment','ModifiedByUserID') IS NULL ALTER TABLE tblBusinessSegment ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblBusinessSegment','CreatedOn') IS NULL ALTER TABLE tblBusinessSegment ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblBusinessSegment','ModifiedOn') IS NULL ALTER TABLE tblBusinessSegment ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblBusinessUnit','CreatedByUserID') IS NULL ALTER TABLE tblBusinessUnit ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblBusinessUnit','ModifiedByUserID') IS NULL ALTER TABLE tblBusinessUnit ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblBusinessUnit','CreatedOn') IS NULL ALTER TABLE tblBusinessUnit ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblBusinessUnit','ModifiedOn') IS NULL ALTER TABLE tblBusinessUnit ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblWorkforceSegment','CreatedByUserID') IS NULL ALTER TABLE tblWorkforceSegment ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblWorkforceSegment','ModifiedByUserID') IS NULL ALTER TABLE tblWorkforceSegment ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblWorkforceSegment','CreatedOn') IS NULL ALTER TABLE tblWorkforceSegment ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblWorkforceSegment','ModifiedOn') IS NULL ALTER TABLE tblWorkforceSegment ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblEmployeeContact','CreatedByUserID') IS NULL ALTER TABLE tblEmployeeContact ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblEmployeeContact','ModifiedByUserID') IS NULL ALTER TABLE tblEmployeeContact ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblEmployeeContact','CreatedOn') IS NULL ALTER TABLE tblEmployeeContact ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblEmployeeContact','ModifiedOn') IS NULL ALTER TABLE tblEmployeeContact ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblEmployeeAddress','CreatedByUserID') IS NULL ALTER TABLE tblEmployeeAddress ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblEmployeeAddress','ModifiedByUserID') IS NULL ALTER TABLE tblEmployeeAddress ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblEmployeeAddress','CreatedOn') IS NULL ALTER TABLE tblEmployeeAddress ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblEmployeeAddress','ModifiedOn') IS NULL ALTER TABLE tblEmployeeAddress ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblEmployeeFamilyMember','CreatedByUserID') IS NULL ALTER TABLE tblEmployeeFamilyMember ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblEmployeeFamilyMember','ModifiedByUserID') IS NULL ALTER TABLE tblEmployeeFamilyMember ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblEmployeeFamilyMember','CreatedOn') IS NULL ALTER TABLE tblEmployeeFamilyMember ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblEmployeeFamilyMember','ModifiedOn') IS NULL ALTER TABLE tblEmployeeFamilyMember ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblEmployeeBank','CreatedByUserID') IS NULL ALTER TABLE tblEmployeeBank ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblEmployeeBank','ModifiedByUserID') IS NULL ALTER TABLE tblEmployeeBank ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblEmployeeBank','CreatedOn') IS NULL ALTER TABLE tblEmployeeBank ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblEmployeeBank','ModifiedOn') IS NULL ALTER TABLE tblEmployeeBank ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblSkill','CreatedByUserID') IS NULL ALTER TABLE tblSkill ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblSkill','ModifiedByUserID') IS NULL ALTER TABLE tblSkill ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblSkill','CreatedOn') IS NULL ALTER TABLE tblSkill ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblSkill','ModifiedOn') IS NULL ALTER TABLE tblSkill ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblWorkerCategory','CreatedByUserID') IS NULL ALTER TABLE tblWorkerCategory ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblWorkerCategory','ModifiedByUserID') IS NULL ALTER TABLE tblWorkerCategory ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblWorkerCategory','CreatedOn') IS NULL ALTER TABLE tblWorkerCategory ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblWorkerCategory','ModifiedOn') IS NULL ALTER TABLE tblWorkerCategory ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblLegalEntity','CreatedByUserID') IS NULL ALTER TABLE tblLegalEntity ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblLegalEntity','ModifiedByUserID') IS NULL ALTER TABLE tblLegalEntity ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblLegalEntity','CreatedOn') IS NULL ALTER TABLE tblLegalEntity ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblLegalEntity','ModifiedOn') IS NULL ALTER TABLE tblLegalEntity ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblSalesTeam','CreatedByUserID') IS NULL ALTER TABLE tblSalesTeam ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblSalesTeam','ModifiedByUserID') IS NULL ALTER TABLE tblSalesTeam ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblSalesTeam','CreatedOn') IS NULL ALTER TABLE tblSalesTeam ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblSalesTeam','ModifiedOn') IS NULL ALTER TABLE tblSalesTeam ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblWorkLocationType','CreatedByUserID') IS NULL ALTER TABLE tblWorkLocationType ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblWorkLocationType','ModifiedByUserID') IS NULL ALTER TABLE tblWorkLocationType ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblWorkLocationType','CreatedOn') IS NULL ALTER TABLE tblWorkLocationType ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblWorkLocationType','ModifiedOn') IS NULL ALTER TABLE tblWorkLocationType ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblWorkArrangement','CreatedByUserID') IS NULL ALTER TABLE tblWorkArrangement ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblWorkArrangement','ModifiedByUserID') IS NULL ALTER TABLE tblWorkArrangement ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblWorkArrangement','CreatedOn') IS NULL ALTER TABLE tblWorkArrangement ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblWorkArrangement','ModifiedOn') IS NULL ALTER TABLE tblWorkArrangement ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblExtension','CreatedByUserID') IS NULL ALTER TABLE tblExtension ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblExtension','ModifiedByUserID') IS NULL ALTER TABLE tblExtension ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblExtension','CreatedOn') IS NULL ALTER TABLE tblExtension ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblExtension','ModifiedOn') IS NULL ALTER TABLE tblExtension ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblCity','CreatedByUserID') IS NULL ALTER TABLE tblCity ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblCity','ModifiedByUserID') IS NULL ALTER TABLE tblCity ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblCity','CreatedOn') IS NULL ALTER TABLE tblCity ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblCity','ModifiedOn') IS NULL ALTER TABLE tblCity ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblProvince','CreatedByUserID') IS NULL ALTER TABLE tblProvince ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblProvince','ModifiedByUserID') IS NULL ALTER TABLE tblProvince ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblProvince','CreatedOn') IS NULL ALTER TABLE tblProvince ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblProvince','ModifiedOn') IS NULL ALTER TABLE tblProvince ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblSalesGroup','CreatedByUserID') IS NULL ALTER TABLE tblSalesGroup ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblSalesGroup','ModifiedByUserID') IS NULL ALTER TABLE tblSalesGroup ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblSalesGroup','CreatedOn') IS NULL ALTER TABLE tblSalesGroup ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblSalesGroup','ModifiedOn') IS NULL ALTER TABLE tblSalesGroup ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblWorkerLocation','CreatedByUserID') IS NULL ALTER TABLE tblWorkerLocation ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblWorkerLocation','ModifiedByUserID') IS NULL ALTER TABLE tblWorkerLocation ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblWorkerLocation','CreatedOn') IS NULL ALTER TABLE tblWorkerLocation ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblWorkerLocation','ModifiedOn') IS NULL ALTER TABLE tblWorkerLocation ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblJob','CreatedByUserID') IS NULL ALTER TABLE tblJob ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblJob','ModifiedByUserID') IS NULL ALTER TABLE tblJob ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblJob','CreatedOn') IS NULL ALTER TABLE tblJob ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblJob','ModifiedOn') IS NULL ALTER TABLE tblJob ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblBenefit','CreatedByUserID') IS NULL ALTER TABLE tblBenefit ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblBenefit','ModifiedByUserID') IS NULL ALTER TABLE tblBenefit ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblBenefit','CreatedOn') IS NULL ALTER TABLE tblBenefit ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblBenefit','ModifiedOn') IS NULL ALTER TABLE tblBenefit ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblUser','CreatedByUserID') IS NULL ALTER TABLE tblUser ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblUser','ModifiedByUserID') IS NULL ALTER TABLE tblUser ADD ModifiedByUserID INT NULL;",
        @"IF COL_LENGTH('tblAppForm','CreatedByUserID') IS NULL ALTER TABLE tblAppForm ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblAppForm','ModifiedByUserID') IS NULL ALTER TABLE tblAppForm ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblAppForm','CreatedOn') IS NULL ALTER TABLE tblAppForm ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblAppForm','ModifiedOn') IS NULL ALTER TABLE tblAppForm ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblUserPermission','CreatedByUserID') IS NULL ALTER TABLE tblUserPermission ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblUserPermission','ModifiedByUserID') IS NULL ALTER TABLE tblUserPermission ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblUserPermission','CreatedOn') IS NULL ALTER TABLE tblUserPermission ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblUserPermission','ModifiedOn') IS NULL ALTER TABLE tblUserPermission ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblExpense','CreatedByUserID') IS NULL ALTER TABLE tblExpense ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblExpense','ModifiedByUserID') IS NULL ALTER TABLE tblExpense ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblExpense','CreatedOn') IS NULL ALTER TABLE tblExpense ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblExpense','ModifiedOn') IS NULL ALTER TABLE tblExpense ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblExpenseDetail','CreatedByUserID') IS NULL ALTER TABLE tblExpenseDetail ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblExpenseDetail','ModifiedByUserID') IS NULL ALTER TABLE tblExpenseDetail ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblExpenseDetail','CreatedOn') IS NULL ALTER TABLE tblExpenseDetail ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblExpenseDetail','ModifiedOn') IS NULL ALTER TABLE tblExpenseDetail ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblEmployeeEducation','CreatedByUserID') IS NULL ALTER TABLE tblEmployeeEducation ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblEmployeeEducation','ModifiedByUserID') IS NULL ALTER TABLE tblEmployeeEducation ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblEmployeeEducation','CreatedOn') IS NULL ALTER TABLE tblEmployeeEducation ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblEmployeeEducation','ModifiedOn') IS NULL ALTER TABLE tblEmployeeEducation ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblEmployeeCertificate','CreatedByUserID') IS NULL ALTER TABLE tblEmployeeCertificate ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblEmployeeCertificate','ModifiedByUserID') IS NULL ALTER TABLE tblEmployeeCertificate ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblEmployeeCertificate','CreatedOn') IS NULL ALTER TABLE tblEmployeeCertificate ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblEmployeeCertificate','ModifiedOn') IS NULL ALTER TABLE tblEmployeeCertificate ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblEmployeePerformance','CreatedByUserID') IS NULL ALTER TABLE tblEmployeePerformance ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblEmployeePerformance','ModifiedByUserID') IS NULL ALTER TABLE tblEmployeePerformance ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblEmployeePerformance','CreatedOn') IS NULL ALTER TABLE tblEmployeePerformance ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblEmployeePerformance','ModifiedOn') IS NULL ALTER TABLE tblEmployeePerformance ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblEmployeeTraining','CreatedByUserID') IS NULL ALTER TABLE tblEmployeeTraining ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblEmployeeTraining','ModifiedByUserID') IS NULL ALTER TABLE tblEmployeeTraining ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblEmployeeTraining','CreatedOn') IS NULL ALTER TABLE tblEmployeeTraining ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblEmployeeTraining','ModifiedOn') IS NULL ALTER TABLE tblEmployeeTraining ADD ModifiedOn DATETIME NULL;",
        @"IF COL_LENGTH('tblRecruitment','CreatedByUserID') IS NULL ALTER TABLE tblRecruitment ADD CreatedByUserID INT NULL;
          IF COL_LENGTH('tblRecruitment','ModifiedByUserID') IS NULL ALTER TABLE tblRecruitment ADD ModifiedByUserID INT NULL;
          IF COL_LENGTH('tblRecruitment','CreatedOn') IS NULL ALTER TABLE tblRecruitment ADD CreatedOn DATETIME NOT NULL DEFAULT GETDATE();
          IF COL_LENGTH('tblRecruitment','ModifiedOn') IS NULL ALTER TABLE tblRecruitment ADD ModifiedOn DATETIME NULL;",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblAuditLog' AND type = 'U')
          CREATE TABLE tblAuditLog (
              AuditLogID   BIGINT IDENTITY(1,1) PRIMARY KEY,
              ActionAt     DATETIME      NOT NULL DEFAULT GETDATE(),
              UserID       INT           NULL,
              Username     NVARCHAR(100) NULL,
              FormKey      NVARCHAR(100) NULL,
              PagePath     NVARCHAR(200) NULL,
              HandlerName  NVARCHAR(100) NULL,
              ActionType   NVARCHAR(50)  NOT NULL,
              EntityType   NVARCHAR(100) NULL,
              EntityID     INT           NULL,
              EntityName   NVARCHAR(250) NULL,
              Details      NVARCHAR(MAX) NULL,
              IpAddress    NVARCHAR(64)  NULL,
              Success      BIT           NOT NULL DEFAULT 1
          );
          IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblAuditLog_ActionAt' AND object_id = OBJECT_ID('tblAuditLog'))
              CREATE INDEX IX_tblAuditLog_ActionAt ON tblAuditLog(ActionAt DESC);
          IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblAuditLog_Username' AND object_id = OBJECT_ID('tblAuditLog'))
              CREATE INDEX IX_tblAuditLog_Username ON tblAuditLog(Username);",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblNotification' AND type = 'U')
          CREATE TABLE tblNotification (
              NotificationID   INT IDENTITY(1,1) PRIMARY KEY,
              NotificationName NVARCHAR(150) NOT NULL,
              Description      NVARCHAR(2000) NULL,
              DepartmentID     INT NULL,
              StartDate        DATE NOT NULL,
              ValidTillDate    DATE NOT NULL,
              IsActive         BIT NOT NULL DEFAULT 1,
              CreatedOn        DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn       DATETIME NULL,
              CreatedByUserID  INT NULL,
              ModifiedByUserID INT NULL
          );",

        @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblMemorandum' AND type = 'U')
          CREATE TABLE tblMemorandum (
              MemorandumID     INT IDENTITY(1,1) PRIMARY KEY,
              MemorandumName   NVARCHAR(150) NOT NULL,
              Description      NVARCHAR(2000) NULL,
              DepartmentID     INT NULL,
              StartDate        DATE NOT NULL,
              ValidTillDate    DATE NOT NULL,
              IsActive         BIT NOT NULL DEFAULT 1,
              DocumentPath     NVARCHAR(500) NULL,
              OriginalFileName NVARCHAR(260) NULL,
              CreatedOn        DATETIME NOT NULL DEFAULT GETDATE(),
              ModifiedOn       DATETIME NULL,
              CreatedByUserID  INT NULL,
              ModifiedByUserID INT NULL
          );",
    };

    try
    {
        using var sqlConn = new SqlConnection(conn);
        sqlConn.Open();

        foreach (var sql in migrations)
        {
            try
            {
                using var cmd = new SqlCommand(sql, sqlConn);
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // Individual migration failure should not crash the app
            }
        }
    }
    catch
    {
        // DB unreachable at startup — pages will surface their own errors
    }
}

static void SeedAppData(IConfiguration config)
{
    var conn = config.GetConnectionString("HRMSConnection");
    if (string.IsNullOrWhiteSpace(conn)) return;

    try
    {
        using var sqlConn = new SqlConnection(conn);
        sqlConn.Open();

        // Seed form registry
        foreach (var form in AppForms.All)
        {
            using var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT 1 FROM tblAppForm WHERE FormKey = @Key)
                INSERT INTO tblAppForm (FormKey, FormName, PagePath, Category, SortOrder)
                VALUES (@Key, @Name, @Path, @Category, @Order);", sqlConn);
            cmd.Parameters.AddWithValue("@Key",      form.Key);
            cmd.Parameters.AddWithValue("@Name",      form.Name);
            cmd.Parameters.AddWithValue("@Path",     form.Path);
            cmd.Parameters.AddWithValue("@Category", form.Category);
            cmd.Parameters.AddWithValue("@Order",     form.SortOrder);
            cmd.ExecuteNonQuery();
        }

        // Sync organization software quick links
        using (var seedSoftware = new SqlCommand(@"
            MERGE tblSoftwareLink AS t
            USING (VALUES
                ('Attendance Portal', 'http://attendance.aibg.com/',              'HR',            'Mark and review employee attendance records',                          1),
                ('Rocket Chat',       'https://chat.ghazibrothers.com.pk/',     'Communication', 'Internal team messaging and collaboration',                            2),
                ('Email',             'https://mail.ghazibrothers.com.pk/interface/root', 'Communication', 'Corporate email and mailbox access',                   3),
                ('Power BI Web',      'https://app.powerbi.com/',               'Analytics',     'Power BI online portal and cloud reports',                             4),
                ('Power BI Desktop',  'http://gbhq-pbi-srv/Reports/powerbi/',   'Analytics',     'On-premises Power BI report server portal',                            5)
            ) AS s (SoftwareName, SoftwareUrl, Category, Description, SortOrder)
            ON t.SoftwareName = s.SoftwareName
            WHEN MATCHED THEN
                UPDATE SET SoftwareUrl = s.SoftwareUrl, Category = s.Category,
                           Description = s.Description, SortOrder = s.SortOrder, IsActive = 1
            WHEN NOT MATCHED THEN
                INSERT (SoftwareName, SoftwareUrl, Category, Description, SortOrder, IsActive, CreatedOn)
                VALUES (s.SoftwareName, s.SoftwareUrl, s.Category, s.Description, s.SortOrder, 1, GETDATE());

            UPDATE tblSoftwareLink SET IsActive = 0
            WHERE SoftwareName IN ('Microsoft Power BI', 'Microsoft Teams', 'Microsoft Outlook', 'SAP');", sqlConn))
        {
            seedSoftware.ExecuteNonQuery();
        }

        // Seed default document types
        using (var seedDocTypes = new SqlCommand(@"
            IF NOT EXISTS (SELECT 1 FROM tblDocumentType)
            BEGIN
                INSERT INTO tblDocumentType (DocumentTypeName, IsActive, CreatedOn) VALUES
                    ('CNIC', 1, GETDATE()),
                    ('Passport', 1, GETDATE()),
                    ('Offer Letter', 1, GETDATE()),
                    ('Appointment Letter', 1, GETDATE()),
                    ('Experience Letter', 1, GETDATE()),
                    ('Educational Certificate', 1, GETDATE()),
                    ('Resume / CV', 1, GETDATE()),
                    ('Other', 1, GETDATE());
            END;", sqlConn))
        {
            seedDocTypes.ExecuteNonQuery();
        }

        // Seed default admin user (username: admin / password: Admin@123)
        using (var check = new SqlCommand("SELECT COUNT(*) FROM tblUser WHERE Username = 'admin';", sqlConn))
        {
            if (Convert.ToInt32(check.ExecuteScalar()) == 0)
            {
                using var ins = new SqlCommand(@"
                    INSERT INTO tblUser (UserCode, Username, PasswordHash, FullName, Email, IsActive, IsAdmin)
                    VALUES ('GB-US-00001', 'admin', @Hash, 'System Administrator', 'admin@hrms.local', 1, 1);", sqlConn);
                ins.Parameters.AddWithValue("@Hash", PasswordHelper.HashPassword("Admin@123"));
                ins.ExecuteNonQuery();
            }
        }
    }
    catch
    {
        // Seeding failure should not crash the app
    }
}
