using HRMS.Services;
namespace HRMS.Pages;

public class ReligionSetupModel : LookupSetupPageModel
{
    public ReligionSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblReligion";
    protected override string IdColumn => "ReligionID";
    protected override string NameColumn => "ReligionName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Religion Setup";
    public override string ItemLabel => "Religion";
    public override string PagePath => "/ReligionSetup";
}
