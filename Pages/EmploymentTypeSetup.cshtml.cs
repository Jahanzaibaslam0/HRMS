using HRMS.Services;
namespace HRMS.Pages;

public class EmploymentTypeSetupModel : LookupSetupPageModel
{
    public EmploymentTypeSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblEmploymentType";
    protected override string IdColumn => "EmploymentTypeID";
    protected override string NameColumn => "EmploymentTypeName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Employment Type Setup";
    public override string ItemLabel => "Employment Type";
    public override string PagePath => "/EmploymentTypeSetup";
}
