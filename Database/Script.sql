-- =============================================
-- HRMS - Employee Master Data
-- Database Setup Script
-- =============================================

USE master;
GO

-- Create Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'HRMSDB')
BEGIN
    CREATE DATABASE HRMSDB;
END
GO

USE HRMSDB;
GO

-- Create Divisions lookup table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblDivision' AND type = 'U')
BEGIN
    CREATE TABLE tblDivision (
        DivisionID   INT IDENTITY(1,1) PRIMARY KEY,
        DivisionName NVARCHAR(100) NOT NULL UNIQUE,
        IsActive     BIT NOT NULL DEFAULT 1,
        CreatedOn    DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn   DATETIME NULL
    );

    INSERT INTO tblDivision (DivisionName) VALUES
        ('Corporate'),
        ('Operations'),
        ('Commercial'),
        ('Support Services');
END
GO

-- Create Departments lookup table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblDepartment' AND type = 'U')
BEGIN
    CREATE TABLE tblDepartment (
        DepartmentID   INT IDENTITY(1,1) PRIMARY KEY,
        DivisionID     INT              NULL REFERENCES tblDivision(DivisionID),
        DepartmentName NVARCHAR(100)     NOT NULL,
        IsActive       BIT              NOT NULL DEFAULT 1
    );

    INSERT INTO tblDepartment (DepartmentName) VALUES
        ('Human Resources'),
        ('Finance'),
        ('Information Technology'),
        ('Operations'),
        ('Marketing'),
        ('Sales'),
        ('Administration');
END
GO

IF COL_LENGTH('tblDepartment', 'DivisionID') IS NULL
    ALTER TABLE tblDepartment ADD DivisionID INT NULL;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_tblDepartment_tblDivision'
      AND parent_object_id = OBJECT_ID('tblDepartment')
)
BEGIN
    ALTER TABLE tblDepartment
    ADD CONSTRAINT FK_tblDepartment_tblDivision
        FOREIGN KEY (DivisionID) REFERENCES tblDivision(DivisionID);
END
GO

-- Create Employee Master table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmployee' AND type = 'U')
BEGIN
    CREATE TABLE tblEmployee (
        EmployeeID     INT IDENTITY(1,1)  PRIMARY KEY,
        EmployeeCode   NVARCHAR(20)       NOT NULL UNIQUE,
        FirstName      NVARCHAR(100)      NOT NULL,
        LastName       NVARCHAR(100)      NOT NULL,
        Gender         NVARCHAR(10)       NOT NULL,
        DateOfBirth    DATE               NULL,
        Email          NVARCHAR(150)      NULL,
        Phone          NVARCHAR(20)       NULL,
        DepartmentID   INT                NOT NULL REFERENCES tblDepartment(DepartmentID),
        Designation    NVARCHAR(100)      NOT NULL,
        DateOfJoining  DATE               NOT NULL,
        BasicSalary    DECIMAL(12,2)      NOT NULL DEFAULT 0,
        Address        NVARCHAR(300)      NULL,
        PersonalEmail  NVARCHAR(150)      NULL,
        OfficialEmail  NVARCHAR(150)      NULL,
        PersonalMobile NVARCHAR(20)       NULL,
        OfficialMobile NVARCHAR(20)       NULL,
        WhatsAppNumber NVARCHAR(20)       NULL,
        EmergencyContactName NVARCHAR(100) NULL,
        EmergencyContactRelationship NVARCHAR(50) NULL,
        EmergencyContactNumber NVARCHAR(20) NULL,
        CurrentAddress NVARCHAR(500)      NULL,
        CurrentCity    NVARCHAR(100)      NULL,
        CurrentProvince NVARCHAR(100)     NULL,
        PostalCode     NVARCHAR(10)       NULL,
        PermanentSameAsCurrent BIT        NOT NULL DEFAULT 1,
        PermanentAddress NVARCHAR(500)    NULL,
        Status         NVARCHAR(20)       NOT NULL DEFAULT 'Active', -- Active / Inactive
        CreatedOn      DATETIME           NOT NULL DEFAULT GETDATE(),
        ModifiedOn     DATETIME           NULL
    );
END
GO

-- Contact records are maintained in tblEmployeeContact, so core Email/Phone are optional summaries.
DECLARE @DropEmailUniqueSql NVARCHAR(MAX) = N'';
SELECT @DropEmailUniqueSql = @DropEmailUniqueSql +
    N'ALTER TABLE tblEmployee DROP CONSTRAINT ' + QUOTENAME(kc.name) + N';'
FROM sys.key_constraints kc
INNER JOIN sys.index_columns ic
    ON ic.object_id = kc.parent_object_id
   AND ic.index_id = kc.unique_index_id
INNER JOIN sys.columns c
    ON c.object_id = ic.object_id
   AND c.column_id = ic.column_id
WHERE kc.parent_object_id = OBJECT_ID('tblEmployee')
  AND kc.type = 'UQ'
  AND c.name = 'Email'
  AND NOT EXISTS (
      SELECT 1
      FROM sys.index_columns ic2
      INNER JOIN sys.columns c2
          ON c2.object_id = ic2.object_id
         AND c2.column_id = ic2.column_id
      WHERE ic2.object_id = kc.parent_object_id
        AND ic2.index_id = kc.unique_index_id
        AND c2.name <> 'Email'
  );

IF @DropEmailUniqueSql <> N''
    EXEC sp_executesql @DropEmailUniqueSql;

IF COL_LENGTH('tblEmployee', 'Email') IS NOT NULL
    ALTER TABLE tblEmployee ALTER COLUMN Email NVARCHAR(150) NULL;
IF COL_LENGTH('tblEmployee', 'Phone') IS NOT NULL
    ALTER TABLE tblEmployee ALTER COLUMN Phone NVARCHAR(20) NULL;
GO

-- Add contact + address columns for existing databases
IF COL_LENGTH('tblEmployee', 'PersonalEmail') IS NULL
    ALTER TABLE tblEmployee ADD PersonalEmail NVARCHAR(150) NULL;
IF COL_LENGTH('tblEmployee', 'OfficialEmail') IS NULL
    ALTER TABLE tblEmployee ADD OfficialEmail NVARCHAR(150) NULL;
IF COL_LENGTH('tblEmployee', 'PersonalMobile') IS NULL
    ALTER TABLE tblEmployee ADD PersonalMobile NVARCHAR(20) NULL;
IF COL_LENGTH('tblEmployee', 'OfficialMobile') IS NULL
    ALTER TABLE tblEmployee ADD OfficialMobile NVARCHAR(20) NULL;
IF COL_LENGTH('tblEmployee', 'WhatsAppNumber') IS NULL
    ALTER TABLE tblEmployee ADD WhatsAppNumber NVARCHAR(20) NULL;
IF COL_LENGTH('tblEmployee', 'EmergencyContactName') IS NULL
    ALTER TABLE tblEmployee ADD EmergencyContactName NVARCHAR(100) NULL;
IF COL_LENGTH('tblEmployee', 'EmergencyContactRelationship') IS NULL
    ALTER TABLE tblEmployee ADD EmergencyContactRelationship NVARCHAR(50) NULL;
IF COL_LENGTH('tblEmployee', 'EmergencyContactNumber') IS NULL
    ALTER TABLE tblEmployee ADD EmergencyContactNumber NVARCHAR(20) NULL;
IF COL_LENGTH('tblEmployee', 'CurrentAddress') IS NULL
    ALTER TABLE tblEmployee ADD CurrentAddress NVARCHAR(500) NULL;
IF COL_LENGTH('tblEmployee', 'CurrentCity') IS NULL
    ALTER TABLE tblEmployee ADD CurrentCity NVARCHAR(100) NULL;
IF COL_LENGTH('tblEmployee', 'CurrentProvince') IS NULL
    ALTER TABLE tblEmployee ADD CurrentProvince NVARCHAR(100) NULL;
IF COL_LENGTH('tblEmployee', 'PostalCode') IS NULL
    ALTER TABLE tblEmployee ADD PostalCode NVARCHAR(10) NULL;
IF COL_LENGTH('tblEmployee', 'PermanentSameAsCurrent') IS NULL
    ALTER TABLE tblEmployee ADD PermanentSameAsCurrent BIT NOT NULL CONSTRAINT DF_tblEmployee_PermSame DEFAULT 1;
IF COL_LENGTH('tblEmployee', 'PermanentAddress') IS NULL
    ALTER TABLE tblEmployee ADD PermanentAddress NVARCHAR(500) NULL;
IF COL_LENGTH('tblEmployee', 'GenderID') IS NULL
    ALTER TABLE tblEmployee ADD GenderID INT NULL;
IF COL_LENGTH('tblEmployee', 'BloodGroupID') IS NULL
    ALTER TABLE tblEmployee ADD BloodGroupID INT NULL;
IF COL_LENGTH('tblEmployee', 'BenefitEntitlementID') IS NULL
    ALTER TABLE tblEmployee ADD BenefitEntitlementID INT NULL;
IF COL_LENGTH('tblEmployee', 'EmploymentStartDate') IS NULL
    ALTER TABLE tblEmployee ADD EmploymentStartDate DATE NULL;
IF COL_LENGTH('tblEmployee', 'ProbationPeriodDays') IS NULL
    ALTER TABLE tblEmployee ADD ProbationPeriodDays INT NULL;
IF COL_LENGTH('tblEmployee', 'ProbationEndDate') IS NULL
    ALTER TABLE tblEmployee ADD ProbationEndDate DATE NULL;
IF COL_LENGTH('tblEmployee', 'ConfirmationDate') IS NULL
    ALTER TABLE tblEmployee ADD ConfirmationDate DATE NULL;
IF COL_LENGTH('tblEmployee', 'FathersHusbandsName') IS NULL
    ALTER TABLE tblEmployee ADD FathersHusbandsName NVARCHAR(150) NULL;
IF COL_LENGTH('tblEmployee', 'DisplayName') IS NULL
    ALTER TABLE tblEmployee ADD DisplayName NVARCHAR(200) NULL;
IF COL_LENGTH('tblEmployee', 'NationalIDNumber') IS NULL
    ALTER TABLE tblEmployee ADD NationalIDNumber NVARCHAR(50) NULL;
IF COL_LENGTH('tblEmployee', 'RegionID') IS NULL
    ALTER TABLE tblEmployee ADD RegionID INT NULL;
IF COL_LENGTH('tblEmployee', 'LocationID') IS NULL
    ALTER TABLE tblEmployee ADD LocationID INT NULL;
IF COL_LENGTH('tblEmployee', 'JobID') IS NULL
    ALTER TABLE tblEmployee ADD JobID INT NULL;
IF COL_LENGTH('tblEmployee', 'WorkerLocationID') IS NULL
    ALTER TABLE tblEmployee ADD WorkerLocationID INT NULL;
IF COL_LENGTH('tblEmployee', 'Domicile') IS NULL
    ALTER TABLE tblEmployee ADD Domicile NVARCHAR(150) NULL;
IF COL_LENGTH('tblEmployee', 'CityID') IS NULL
    ALTER TABLE tblEmployee ADD CityID INT NULL;
IF COL_LENGTH('tblEmployee', 'ProvinceID') IS NULL
    ALTER TABLE tblEmployee ADD ProvinceID INT NULL;
IF COL_LENGTH('tblEmployee', 'SalesGroupID') IS NULL
    ALTER TABLE tblEmployee ADD SalesGroupID INT NULL;
IF COL_LENGTH('tblEmployee', 'GradeID') IS NULL
    ALTER TABLE tblEmployee ADD GradeID INT NULL;
IF COL_LENGTH('tblEmployee', 'ExtensionID') IS NULL
    ALTER TABLE tblEmployee ADD ExtensionID INT NULL;
GO

IF COL_LENGTH('tblDepartment', 'CreatedOn') IS NULL
    ALTER TABLE tblDepartment ADD CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblDepartment_CreatedOn DEFAULT GETDATE();
IF COL_LENGTH('tblDepartment', 'ModifiedOn') IS NULL
    ALTER TABLE tblDepartment ADD ModifiedOn DATETIME NULL;
GO

IF COL_LENGTH('tblBloodGroup', 'BloodGroupName') IS NOT NULL
    ALTER TABLE tblBloodGroup ALTER COLUMN BloodGroupName NVARCHAR(50) NOT NULL;
GO

IF COL_LENGTH('tblDepartment', 'AliasName') IS NULL
    ALTER TABLE tblDepartment ADD AliasName NVARCHAR(50) NULL;
IF COL_LENGTH('tblDivision', 'AliasName') IS NULL
    ALTER TABLE tblDivision ADD AliasName NVARCHAR(50) NULL;
IF COL_LENGTH('tblBloodGroup', 'AliasName') IS NULL
    ALTER TABLE tblBloodGroup ADD AliasName NVARCHAR(50) NULL;
IF COL_LENGTH('tblReligion', 'AliasName') IS NULL
    ALTER TABLE tblReligion ADD AliasName NVARCHAR(50) NULL;
IF COL_LENGTH('tblNationality', 'AliasName') IS NULL
    ALTER TABLE tblNationality ADD AliasName NVARCHAR(50) NULL;
IF COL_LENGTH('tblBenefitEntitlement', 'AliasName') IS NULL
    ALTER TABLE tblBenefitEntitlement ADD AliasName NVARCHAR(50) NULL;
GO

-- =============================================
-- Employee Master Lookup Tables
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblGender' AND type = 'U')
BEGIN
    CREATE TABLE tblGender (
        GenderID   INT IDENTITY(1,1) PRIMARY KEY,
        GenderName NVARCHAR(50) NOT NULL UNIQUE,
        IsActive   BIT NOT NULL DEFAULT 1,
        CreatedOn  DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn DATETIME NULL
    );

    INSERT INTO tblGender (GenderName) VALUES ('Male'), ('Female'), ('Other');
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblBloodGroup' AND type = 'U')
BEGIN
    CREATE TABLE tblBloodGroup (
        BloodGroupID   INT IDENTITY(1,1) PRIMARY KEY,
        BloodGroupName NVARCHAR(50) NOT NULL UNIQUE,
        IsActive       BIT NOT NULL DEFAULT 1,
        CreatedOn      DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn     DATETIME NULL
    );

    INSERT INTO tblBloodGroup (BloodGroupName) VALUES
        ('A+'), ('A-'), ('B+'), ('B-'), ('AB+'), ('AB-'), ('O+'), ('O-');
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblBenefitEntitlement' AND type = 'U')
BEGIN
    CREATE TABLE tblBenefitEntitlement (
        BenefitEntitlementID   INT IDENTITY(1,1) PRIMARY KEY,
        BenefitEntitlementName NVARCHAR(100) NOT NULL UNIQUE,
        IsActive               BIT NOT NULL DEFAULT 1,
        CreatedOn              DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn             DATETIME NULL
    );

    INSERT INTO tblBenefitEntitlement (BenefitEntitlementName) VALUES
        ('Standard Benefits'),
        ('Management Benefits'),
        ('Executive Benefits');
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblExpenseCategory' AND type = 'U')
BEGIN
    CREATE TABLE tblExpenseCategory (
        ExpenseCategoryID   INT IDENTITY(1,1) PRIMARY KEY,
        ExpenseCategoryName NVARCHAR(150) NOT NULL UNIQUE,
        IsActive            BIT NOT NULL DEFAULT 1,
        CreatedOn           DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn          DATETIME NULL
    );

    INSERT INTO tblExpenseCategory (ExpenseCategoryName) VALUES
        ('Expense Claims Eligible'),
        ('Expense Policy'),
        ('Expense Approval Limit'),
        ('Travel Authorization'),
        ('Per Diem Eligible'),
        ('Per Diem Rate - Domestic'),
        ('Per Diem Rate - International'),
        ('Mileage Tracking'),
        ('Mileage Rate'),
        ('Corporate Card Issued'),
        ('Corporate Card Number'),
        ('Fuel Card Issued'),
        ('Hotel Limit'),
        ('Meal Allowance');
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblBankMaster' AND type = 'U')
BEGIN
    CREATE TABLE tblBankMaster (
        BankID                    INT IDENTITY(1,1) PRIMARY KEY,
        BankName                  NVARCHAR(150) NOT NULL,
        BranchCode                NVARCHAR(50) NULL,
        BranchName                NVARCHAR(150) NULL,
        AccountTitle              NVARCHAR(150) NULL,
        IBAN                      NVARCHAR(50) NULL,
        SwiftBICCode              NVARCHAR(50) NULL,
        AccountType               NVARCHAR(50) NULL,
        AccountVerificationStatus NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        IsActive                  BIT NOT NULL DEFAULT 1,
        CreatedOn                 DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn                DATETIME NULL
    );

END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblReligion' AND type = 'U')
BEGIN
    CREATE TABLE tblReligion (
        ReligionID   INT IDENTITY(1,1) PRIMARY KEY,
        ReligionName NVARCHAR(100) NOT NULL UNIQUE,
        IsActive     BIT NOT NULL DEFAULT 1,
        CreatedOn    DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn   DATETIME NULL
    );

    INSERT INTO tblReligion (ReligionName) VALUES
        ('Islam'),
        ('Christianity'),
        ('Hinduism'),
        ('Sikhism'),
        ('Buddhism'),
        ('Other');
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblNationality' AND type = 'U')
BEGIN
    CREATE TABLE tblNationality (
        NationalityID   INT IDENTITY(1,1) PRIMARY KEY,
        NationalityName NVARCHAR(100) NOT NULL UNIQUE,
        IsActive        BIT NOT NULL DEFAULT 1,
        CreatedOn       DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn      DATETIME NULL
    );

    INSERT INTO tblNationality (NationalityName) VALUES
        ('Pakistani'),
        ('Afghan'),
        ('Bangladeshi'),
        ('Indian'),
        ('Sri Lankan'),
        ('Other');
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblLanguage' AND type = 'U')
BEGIN
    CREATE TABLE tblLanguage (
        LanguageID   INT IDENTITY(1,1) PRIMARY KEY,
        LanguageCode NVARCHAR(20) NOT NULL UNIQUE,
        LanguageName NVARCHAR(100) NOT NULL,
        NativeName   NVARCHAR(100) NULL,
        Region       NVARCHAR(100) NULL,
        Source       NVARCHAR(100) NULL,
        IsPriority   BIT NOT NULL DEFAULT 0,
        IsActive     BIT NOT NULL DEFAULT 1,
        CreatedOn    DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn   DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblUnit' AND type = 'U')
BEGIN
    CREATE TABLE tblUnit (
        UnitID INT IDENTITY(1,1) PRIMARY KEY,
        UnitName NVARCHAR(100) NOT NULL UNIQUE,
        AliasName NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblLocation' AND type = 'U')
BEGIN
    CREATE TABLE tblLocation (
        LocationID INT IDENTITY(1,1) PRIMARY KEY,
        LocationName NVARCHAR(100) NOT NULL UNIQUE,
        AliasName NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblRegion' AND type = 'U')
BEGIN
    CREATE TABLE tblRegion (
        RegionID INT IDENTITY(1,1) PRIMARY KEY,
        RegionName NVARCHAR(100) NOT NULL UNIQUE,
        AliasName NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblWing' AND type = 'U')
BEGIN
    CREATE TABLE tblWing (
        WingID INT IDENTITY(1,1) PRIMARY KEY,
        WingName NVARCHAR(100) NOT NULL UNIQUE,
        AliasName NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblGrade' AND type = 'U')
BEGIN
    CREATE TABLE tblGrade (
        GradeID INT IDENTITY(1,1) PRIMARY KEY,
        GradeName NVARCHAR(100) NOT NULL UNIQUE,
        AliasName NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmploymentType' AND type = 'U')
BEGIN
    CREATE TABLE tblEmploymentType (
        EmploymentTypeID INT IDENTITY(1,1) PRIMARY KEY,
        EmploymentTypeName NVARCHAR(100) NOT NULL UNIQUE,
        AliasName NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblDesignationLevel' AND type = 'U')
BEGIN
    CREATE TABLE tblDesignationLevel (
        DesignationLevelID INT IDENTITY(1,1) PRIMARY KEY,
        DesignationLevelName NVARCHAR(100) NOT NULL UNIQUE,
        AliasName NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblCostCenter' AND type = 'U')
BEGIN
    CREATE TABLE tblCostCenter (
        CostCenterID INT IDENTITY(1,1) PRIMARY KEY,
        CostCenterName NVARCHAR(100) NOT NULL UNIQUE,
        AliasName NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmploymentStatus' AND type = 'U')
BEGIN
    CREATE TABLE tblEmploymentStatus (
        EmploymentStatusID INT IDENTITY(1,1) PRIMARY KEY,
        EmploymentStatusName NVARCHAR(100) NOT NULL UNIQUE,
        AliasName NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblBusinessSegment' AND type = 'U')
BEGIN
    CREATE TABLE tblBusinessSegment (
        BusinessSegmentID INT IDENTITY(1,1) PRIMARY KEY,
        BusinessSegmentName NVARCHAR(100) NOT NULL UNIQUE,
        AliasName NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn DATETIME NULL
    );
END
GO

UPDATE e
SET GenderID = g.GenderID
FROM tblEmployee e
INNER JOIN tblGender g ON g.GenderName = e.Gender
WHERE e.GenderID IS NULL;
GO

-- Backfill old data into new fields for smooth transition
UPDATE tblEmployee
SET OfficialEmail  = ISNULL(OfficialEmail, Email),
    PersonalMobile = ISNULL(PersonalMobile, Phone),
    CurrentAddress = ISNULL(CurrentAddress, Address)
WHERE OfficialEmail IS NULL
   OR PersonalMobile IS NULL
   OR CurrentAddress IS NULL;
GO

-- =============================================
-- Multi-record Child Tables
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmployeeContact' AND type = 'U')
BEGIN
    CREATE TABLE tblEmployeeContact (
        ContactID      INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeID     INT NOT NULL REFERENCES tblEmployee(EmployeeID) ON DELETE CASCADE,
        ContactType    NVARCHAR(50) NOT NULL, -- OfficialEmail, PersonalMobile, Emergency, etc.
        ContactName    NVARCHAR(100) NULL,    -- used for named contacts like emergency contact
        Relationship   NVARCHAR(50) NULL,     -- used for emergency/family-like contact
        ContactValue   NVARCHAR(255) NULL,
        IsPrimary      BIT NOT NULL DEFAULT 0,
        SortOrder      INT NOT NULL DEFAULT 1,
        CreatedOn      DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn     DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmployeeAddress' AND type = 'U')
BEGIN
    CREATE TABLE tblEmployeeAddress (
        AddressID      INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeID     INT NOT NULL REFERENCES tblEmployee(EmployeeID) ON DELETE CASCADE,
        AddressType    NVARCHAR(50) NOT NULL, -- Current, Permanent, Temporary, Other
        AddressLine    NVARCHAR(500) NOT NULL,
        City           NVARCHAR(100) NULL,
        ProvinceState  NVARCHAR(100) NULL,
        PostalCode     NVARCHAR(10) NULL,
        IsPrimary      BIT NOT NULL DEFAULT 0,
        SortOrder      INT NOT NULL DEFAULT 1,
        CreatedOn      DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn     DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmployeeFamilyMember' AND type = 'U')
BEGIN
    CREATE TABLE tblEmployeeFamilyMember (
        FamilyMemberID INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeID     INT NOT NULL REFERENCES tblEmployee(EmployeeID) ON DELETE CASCADE,
        MemberName     NVARCHAR(150) NOT NULL,
        Relationship   NVARCHAR(50) NULL,
        Gender         NVARCHAR(20) NULL,
        DateOfBirth    DATE NULL,
        ContactNumber  NVARCHAR(20) NULL,
        IsDependent    BIT NOT NULL DEFAULT 1,
        SortOrder      INT NOT NULL DEFAULT 1,
        CreatedOn      DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn     DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblEmployeeBank' AND type = 'U')
BEGIN
    CREATE TABLE tblEmployeeBank (
        EmployeeBankID            INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeID                INT NOT NULL REFERENCES tblEmployee(EmployeeID) ON DELETE CASCADE,
        BankID                    INT NOT NULL REFERENCES tblBankMaster(BankID),
        BranchCode                NVARCHAR(50) NULL,
        BranchName                NVARCHAR(150) NULL,
        AccountTitle              NVARCHAR(150) NULL,
        IBAN                      NVARCHAR(50) NULL,
        SwiftBICCode              NVARCHAR(50) NULL,
        AccountType               NVARCHAR(50) NULL,
        AccountVerificationStatus NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        IsPrimary                 BIT NOT NULL DEFAULT 0,
        SortOrder                 INT NOT NULL DEFAULT 1,
        CreatedOn                 DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn                DATETIME NULL
    );
END
GO

IF COL_LENGTH('tblEmployeeBank', 'BranchCode') IS NULL
    ALTER TABLE tblEmployeeBank ADD BranchCode NVARCHAR(50) NULL;
IF COL_LENGTH('tblEmployeeBank', 'BranchName') IS NULL
    ALTER TABLE tblEmployeeBank ADD BranchName NVARCHAR(150) NULL;
GO

-- =============================================
-- Stored Procedure: Get All Employees
-- =============================================
IF OBJECT_ID('sp_GetAllEmployees', 'P') IS NOT NULL DROP PROCEDURE sp_GetAllEmployees;
GO
CREATE PROCEDURE sp_GetAllEmployees
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  e.EmployeeID, e.EmployeeCode,
            e.FirstName + ' ' + e.LastName  AS FullName,
            e.FirstName, e.LastName,
            e.Gender, e.DateOfBirth,
            ISNULL(e.OfficialEmail, e.Email) AS Email,
            ISNULL(e.PersonalMobile, e.Phone) AS Phone,
            d.DepartmentName, e.DepartmentID,
            e.Designation, e.DateOfJoining,
            e.BasicSalary, e.Address, e.Status
    FROM    tblEmployee e
    INNER JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
    ORDER BY e.EmployeeID DESC;
END
GO

-- =============================================
-- Stored Procedure: Get Employee By ID
-- =============================================
IF OBJECT_ID('sp_GetEmployeeByID', 'P') IS NOT NULL DROP PROCEDURE sp_GetEmployeeByID;
GO
CREATE PROCEDURE sp_GetEmployeeByID
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  e.EmployeeID, e.EmployeeCode,
            e.FirstName, e.LastName,
            e.Gender, e.DateOfBirth, e.Email, e.Phone,
            e.PersonalEmail, e.OfficialEmail,
            e.PersonalMobile, e.OfficialMobile, e.WhatsAppNumber,
            e.EmergencyContactName, e.EmergencyContactRelationship, e.EmergencyContactNumber,
            e.CurrentAddress, e.CurrentCity, e.CurrentProvince, e.PostalCode,
            e.PermanentSameAsCurrent, e.PermanentAddress,
            e.DepartmentID, e.Designation, e.DateOfJoining,
            e.BasicSalary, e.Address, e.Status
    FROM    tblEmployee e
    WHERE   e.EmployeeID = @EmployeeID;
END
GO

-- =============================================
-- Stored Procedure: Insert Employee
-- =============================================
IF OBJECT_ID('sp_InsertEmployee', 'P') IS NOT NULL DROP PROCEDURE sp_InsertEmployee;
GO
CREATE PROCEDURE sp_InsertEmployee
    @EmployeeCode  NVARCHAR(20),
    @FirstName     NVARCHAR(100),
    @LastName      NVARCHAR(100),
    @Gender        NVARCHAR(10),
    @DateOfBirth   DATE,
    @PersonalEmail NVARCHAR(150),
    @OfficialEmail NVARCHAR(150),
    @PersonalMobile NVARCHAR(20),
    @OfficialMobile NVARCHAR(20),
    @WhatsAppNumber NVARCHAR(20),
    @EmergencyContactName NVARCHAR(100),
    @EmergencyContactRelationship NVARCHAR(50),
    @EmergencyContactNumber NVARCHAR(20),
    @CurrentAddress NVARCHAR(500),
    @CurrentCity NVARCHAR(100),
    @CurrentProvince NVARCHAR(100),
    @PostalCode NVARCHAR(10),
    @PermanentSameAsCurrent BIT,
    @PermanentAddress NVARCHAR(500),
    @DepartmentID  INT,
    @Designation   NVARCHAR(100),
    @DateOfJoining DATE,
    @BasicSalary   DECIMAL(12,2),
    @Address       NVARCHAR(300),
    @Status        NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO tblEmployee
           (EmployeeCode, FirstName, LastName, Gender, DateOfBirth,
            Email, Phone, PersonalEmail, OfficialEmail, PersonalMobile, OfficialMobile, WhatsAppNumber,
            EmergencyContactName, EmergencyContactRelationship, EmergencyContactNumber,
            CurrentAddress, CurrentCity, CurrentProvince, PostalCode, PermanentSameAsCurrent, PermanentAddress,
            DepartmentID, Designation, DateOfJoining, BasicSalary, Address, Status)
    VALUES (@EmployeeCode, @FirstName, @LastName, @Gender, @DateOfBirth,
            @OfficialEmail, @PersonalMobile, @PersonalEmail, @OfficialEmail, @PersonalMobile, @OfficialMobile, @WhatsAppNumber,
            @EmergencyContactName, @EmergencyContactRelationship, @EmergencyContactNumber,
            @CurrentAddress, @CurrentCity, @CurrentProvince, @PostalCode, @PermanentSameAsCurrent, @PermanentAddress,
            @DepartmentID, @Designation, @DateOfJoining, @BasicSalary, @CurrentAddress, @Status);

    SELECT SCOPE_IDENTITY() AS NewEmployeeID;
END
GO

-- =============================================
-- Stored Procedure: Update Employee
-- =============================================
IF OBJECT_ID('sp_UpdateEmployee', 'P') IS NOT NULL DROP PROCEDURE sp_UpdateEmployee;
GO
CREATE PROCEDURE sp_UpdateEmployee
    @EmployeeID    INT,
    @EmployeeCode  NVARCHAR(20),
    @FirstName     NVARCHAR(100),
    @LastName      NVARCHAR(100),
    @Gender        NVARCHAR(10),
    @DateOfBirth   DATE,
    @PersonalEmail NVARCHAR(150),
    @OfficialEmail NVARCHAR(150),
    @PersonalMobile NVARCHAR(20),
    @OfficialMobile NVARCHAR(20),
    @WhatsAppNumber NVARCHAR(20),
    @EmergencyContactName NVARCHAR(100),
    @EmergencyContactRelationship NVARCHAR(50),
    @EmergencyContactNumber NVARCHAR(20),
    @CurrentAddress NVARCHAR(500),
    @CurrentCity NVARCHAR(100),
    @CurrentProvince NVARCHAR(100),
    @PostalCode NVARCHAR(10),
    @PermanentSameAsCurrent BIT,
    @PermanentAddress NVARCHAR(500),
    @DepartmentID  INT,
    @Designation   NVARCHAR(100),
    @DateOfJoining DATE,
    @BasicSalary   DECIMAL(12,2),
    @Address       NVARCHAR(300),
    @Status        NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE tblEmployee SET
        EmployeeCode  = @EmployeeCode,
        FirstName     = @FirstName,
        LastName      = @LastName,
        Gender        = @Gender,
        DateOfBirth   = @DateOfBirth,
        Email         = @OfficialEmail,
        Phone         = @PersonalMobile,
        PersonalEmail = @PersonalEmail,
        OfficialEmail = @OfficialEmail,
        PersonalMobile = @PersonalMobile,
        OfficialMobile = @OfficialMobile,
        WhatsAppNumber = @WhatsAppNumber,
        EmergencyContactName = @EmergencyContactName,
        EmergencyContactRelationship = @EmergencyContactRelationship,
        EmergencyContactNumber = @EmergencyContactNumber,
        CurrentAddress = @CurrentAddress,
        CurrentCity = @CurrentCity,
        CurrentProvince = @CurrentProvince,
        PostalCode = @PostalCode,
        PermanentSameAsCurrent = @PermanentSameAsCurrent,
        PermanentAddress = @PermanentAddress,
        DepartmentID  = @DepartmentID,
        Designation   = @Designation,
        DateOfJoining = @DateOfJoining,
        BasicSalary   = @BasicSalary,
        Address       = @CurrentAddress,
        Status        = @Status,
        ModifiedOn    = GETDATE()
    WHERE EmployeeID = @EmployeeID;
END
GO

-- =============================================
-- Stored Procedure: Delete Employee
-- =============================================
IF OBJECT_ID('sp_DeleteEmployee', 'P') IS NOT NULL DROP PROCEDURE sp_DeleteEmployee;
GO
CREATE PROCEDURE sp_DeleteEmployee
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM tblEmployee WHERE EmployeeID = @EmployeeID;
END
GO

-- =============================================
-- Stored Procedure: Get All Departments (for dropdown)
-- =============================================
IF OBJECT_ID('sp_GetDepartments', 'P') IS NOT NULL DROP PROCEDURE sp_GetDepartments;
GO
CREATE PROCEDURE sp_GetDepartments
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DepartmentID, DepartmentName
    FROM   tblDepartment
    WHERE  IsActive = 1
    ORDER BY DepartmentName;
END
GO

-- =============================================
-- Skill Setup Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblSkill' AND type = 'U')
BEGIN
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
    );
END
GO

-- =============================================
-- Worker Category Setup Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblWorkerCategory' AND type = 'U')
BEGIN
    CREATE TABLE tblWorkerCategory (
        WorkerCategoryID    INT IDENTITY(1,1) PRIMARY KEY,
        WorkerCategoryCode  NVARCHAR(20)  NOT NULL UNIQUE,
        WorkerCategoryName  NVARCHAR(150) NOT NULL,
        AliasName           NVARCHAR(100) NULL,
        Description         NVARCHAR(500) NULL,
        IsActive            BIT           NOT NULL DEFAULT 1,
        CreatedOn           DATETIME      NOT NULL DEFAULT GETDATE(),
        ModifiedOn          DATETIME      NULL
    );
END
GO

-- =============================================
-- Legal Entity Setup Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblLegalEntity' AND type = 'U')
BEGIN
    CREATE TABLE tblLegalEntity (
        LegalEntityID    INT IDENTITY(1,1) PRIMARY KEY,
        LegalEntityCode  NVARCHAR(20)  NOT NULL UNIQUE,
        LegalEntityName  NVARCHAR(150) NOT NULL,
        AliasName        NVARCHAR(100) NULL,
        Description      NVARCHAR(500) NULL,
        IsActive         BIT           NOT NULL DEFAULT 1,
        CreatedOn        DATETIME      NOT NULL DEFAULT GETDATE(),
        ModifiedOn       DATETIME      NULL
    );
END
GO

-- =============================================
-- Sales Team Setup Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblSalesTeam' AND type = 'U')
BEGIN
    CREATE TABLE tblSalesTeam (
        SalesTeamID    INT IDENTITY(1,1) PRIMARY KEY,
        SalesTeamCode  NVARCHAR(20)  NOT NULL UNIQUE,
        SalesTeamName  NVARCHAR(150) NOT NULL,
        DivisionID     INT           NULL REFERENCES tblDivision(DivisionID),
        AliasName      NVARCHAR(100) NULL,
        Description    NVARCHAR(500) NULL,
        IsActive       BIT           NOT NULL DEFAULT 1,
        CreatedOn      DATETIME      NOT NULL DEFAULT GETDATE(),
        ModifiedOn     DATETIME      NULL
    );
END
GO

-- =============================================
-- Work Location Type Setup Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblWorkLocationType' AND type = 'U')
BEGIN
    CREATE TABLE tblWorkLocationType (
        WorkLocationTypeID   INT IDENTITY(1,1) PRIMARY KEY,
        WorkLocationTypeCode NVARCHAR(20)  NOT NULL UNIQUE,
        WorkLocationTypeName NVARCHAR(150) NOT NULL,
        AliasName            NVARCHAR(100) NULL,
        IsActive             BIT           NOT NULL DEFAULT 1,
        CreatedOn            DATETIME      NOT NULL DEFAULT GETDATE(),
        ModifiedOn           DATETIME      NULL
    );
END
GO

-- =============================================
-- Work Arrangement Setup Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblWorkArrangement' AND type = 'U')
BEGIN
    CREATE TABLE tblWorkArrangement (
        WorkArrangementID   INT IDENTITY(1,1) PRIMARY KEY,
        WorkArrangementCode NVARCHAR(20)  NOT NULL UNIQUE,
        WorkArrangementName NVARCHAR(150) NOT NULL,
        AliasName           NVARCHAR(100) NULL,
        IsActive            BIT           NOT NULL DEFAULT 1,
        CreatedOn           DATETIME      NOT NULL DEFAULT GETDATE(),
        ModifiedOn          DATETIME      NULL
    );
END
GO

-- =============================================
-- Extension Master Setup Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblExtension' AND type = 'U')
BEGIN
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
    );
END
GO

IF COL_LENGTH('tblExtension', 'DepartmentID') IS NULL
    ALTER TABLE tblExtension ADD DepartmentID INT NULL;
IF COL_LENGTH('tblExtension', 'LocationID') IS NULL
    ALTER TABLE tblExtension ADD LocationID INT NULL;
GO

-- =============================================
-- City Setup Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblCity' AND type = 'U')
BEGIN
    CREATE TABLE tblCity (
        CityID     INT IDENTITY(1,1) PRIMARY KEY,
        CityCode   NVARCHAR(20)  NOT NULL UNIQUE,
        CityName   NVARCHAR(150) NOT NULL,
        AliasName  NVARCHAR(100) NULL,
        IsActive   BIT           NOT NULL DEFAULT 1,
        CreatedOn  DATETIME      NOT NULL DEFAULT GETDATE(),
        ModifiedOn DATETIME      NULL
    );
END
GO

-- =============================================
-- Province Setup Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblProvince' AND type = 'U')
BEGIN
    CREATE TABLE tblProvince (
        ProvinceID   INT IDENTITY(1,1) PRIMARY KEY,
        ProvinceCode NVARCHAR(20)  NOT NULL UNIQUE,
        ProvinceName NVARCHAR(150) NOT NULL,
        AliasName    NVARCHAR(100) NULL,
        IsActive     BIT           NOT NULL DEFAULT 1,
        CreatedOn    DATETIME      NOT NULL DEFAULT GETDATE(),
        ModifiedOn   DATETIME      NULL
    );
END
GO

-- =============================================
-- Sales Group Setup Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblSalesGroup' AND type = 'U')
BEGIN
    CREATE TABLE tblSalesGroup (
        SalesGroupID   INT IDENTITY(1,1) PRIMARY KEY,
        SalesGroupCode NVARCHAR(20)  NOT NULL UNIQUE,
        SalesGroupName NVARCHAR(150) NOT NULL,
        AliasName      NVARCHAR(100) NULL,
        IsActive       BIT           NOT NULL DEFAULT 1,
        CreatedOn      DATETIME      NOT NULL DEFAULT GETDATE(),
        ModifiedOn     DATETIME      NULL
    );
END
GO

-- =============================================
-- Worker Location Setup Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'tblWorkerLocation' AND type = 'U')
BEGIN
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
    );
END
GO

PRINT 'HRMSDB setup completed successfully.';
