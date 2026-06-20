using HRMS.Services;
namespace HRMS.Pages;

public class EmploymentStatusSetupModel : LookupSetupPageModel
{
    public EmploymentStatusSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblEmploymentStatus";
    protected override string IdColumn => "EmploymentStatusID";
    protected override string NameColumn => "EmploymentStatusName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Employment Status Setup";
    public override string ItemLabel => "Employment Status";
    public override string PagePath => "/EmploymentStatusSetup";
}
