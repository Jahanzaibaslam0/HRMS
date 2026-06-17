using Microsoft.Data.SqlClient;
using HRMS.Services;
using HRMS.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
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
