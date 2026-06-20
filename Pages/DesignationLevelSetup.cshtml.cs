using HRMS.Services;
namespace HRMS.Pages;

public class DesignationLevelSetupModel : LookupSetupPageModel
{
    public DesignationLevelSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblDesignationLevel";
    protected override string IdColumn => "DesignationLevelID";
    protected override string NameColumn => "DesignationLevelName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Designation Level Setup";
    public override string ItemLabel => "Designation Level";
    public override string PagePath => "/DesignationLevelSetup";
}
