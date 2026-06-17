namespace HRMS.Services;

/// <summary>All application forms/pages that can be permission-controlled.</summary>
public static class AppForms
{
    public record FormDef(string Key, string Name, string Path, string Category, int SortOrder);

    public static readonly FormDef[] All = new FormDef[]
    {
        new("EmployeeMaster",          "Employee Master",          "/EmployeeMaster",          "Transactions",       1),
        new("EmployeeReport",          "Internal Employee Directory","/EmployeeReport",          "Transactions",       2),

        new("DivisionSetup",           "Division Setup",           "/DivisionSetup",           "Organization Setup", 10),
        new("BusinessSegmentSetup",    "Business Segment Setup",   "/BusinessSegmentSetup",    "Organization Setup", 11),
        new("BusinessUnitSetup",       "Business Unit Setup",      "/BusinessUnitSetup",       "Organization Setup", 12),
        new("WorkforceSegmentSetup",   "Workforce Segment Setup",  "/WorkforceSegmentSetup",   "Organization Setup", 13),
        new("UnitSetup",               "Unit Setup",               "/UnitSetup",               "Organization Setup", 14),
        new("WingSetup",               "Wing Setup",               "/WingSetup",               "Organization Setup", 15),
        new("GenderSetup",             "Gender Setup",             "/GenderSetup",             "Organization Setup", 16),
        new("ReligionSetup",           "Religion Setup",           "/ReligionSetup",           "Organization Setup", 17),
        new("NationalitySetup",        "Nationality Setup",        "/NationalitySetup",        "Organization Setup", 18),
        new("LanguageSetup",           "Language Setup",           "/LanguageSetup",           "Organization Setup", 19),
        new("BankSetup",               "Bank Master Setup",        "/BankSetup",               "Organization Setup", 20),
        new("CostCenterSetup",         "Cost Center Setup",        "/CostCenterSetup",         "Organization Setup", 21),
        new("SkillSetup",              "Skill Setup",              "/SkillSetup",              "Organization Setup", 22),
        new("LegalEntitySetup",        "Legal Entity Setup",       "/LegalEntitySetup",        "Organization Setup", 23),
        new("SalesTeamSetup",          "Sales Team Setup",         "/SalesTeamSetup",          "Organization Setup", 24),
        new("WorkLocationTypeSetup",   "Work Location Type Setup", "/WorkLocationTypeSetup",   "Organization Setup", 25),
        new("WorkArrangementSetup",    "Work Arrangement Setup",   "/WorkArrangementSetup",    "Organization Setup", 26),
        new("ExtensionSetup",          "Extension Master Setup",   "/ExtensionSetup",          "Organization Setup", 27),
        new("CitySetup",               "City Setup",               "/CitySetup",               "Organization Setup", 28),
        new("ProvinceSetup",           "Province Setup",           "/ProvinceSetup",           "Organization Setup", 29),
        new("SalesGroupSetup",         "Sales Group Setup",        "/SalesGroupSetup",         "Organization Setup", 30),
        new("DepartmentSetup",         "Department Setup",         "/DepartmentSetup",         "Organization Setup", 31),
        new("RegionSetup",             "Region Setup",             "/RegionSetup",             "Organization Setup", 32),
        new("LocationSetup",           "Location Setup",           "/LocationSetup",           "Organization Setup", 33),

        new("GradeSetup",              "Grade Setup",              "/GradeSetup",              "Employee Setup",     33),
        new("EmploymentTypeSetup",     "Employment Type Setup",    "/EmploymentTypeSetup",     "Employee Setup",     34),
        new("DesignationLevelSetup",   "Designation Level Setup",  "/DesignationLevelSetup",   "Employee Setup",     35),
        new("EmploymentStatusSetup",   "Employment Status Setup",  "/EmploymentStatusSetup",   "Employee Setup",     36),
        new("BenefitSetup",            "Benefit Setup",            "/BenefitSetup",            "Employee Setup",     37),
        new("BenefitEntitlementSetup", "Benefit Entitlement Setup","/BenefitEntitlementSetup", "Employee Setup",     38),
        new("ExpenseCategorySetup",    "Expense Category Setup",   "/ExpenseCategorySetup",    "Employee Setup",     39),
        new("BloodGroupSetup",         "Blood Group Setup",        "/BloodGroupSetup",         "Employee Setup",     40),
        new("WorkerCategorySetup",     "Worker Category Setup",    "/WorkerCategorySetup",     "Employee Setup",     41),
        new("JobSetup",                "Job Setup",                "/JobSetup",                "Employee Setup",     42),
        new("WorkerLocationSetup",     "Worker Location Setup",    "/WorkerLocationSetup",     "Employee Setup",     43),

        new("UserSetup",               "User Setup",               "/UserSetup",               "Security",           50),
        new("UserRightsSetup",         "User Rights Setup",        "/UserRightsSetup",         "Security",           51),
    };

    public static FormDef? FindByPath(string path)
    {
        var p = path.TrimEnd('/').ToLowerInvariant();
        if (string.IsNullOrEmpty(p)) p = "/";
        return All.FirstOrDefault(f => f.Path.Equals(p, StringComparison.OrdinalIgnoreCase));
    }

    public static FormDef? FindByKey(string key)
        => All.FirstOrDefault(f => f.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
}
