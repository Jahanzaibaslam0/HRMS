using HRMS.Services;
namespace HRMS.Pages;

public class RegionSetupModel : LookupSetupPageModel
{
    public RegionSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblRegion";
    protected override string IdColumn => "RegionID";
    protected override string NameColumn => "RegionName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Region Setup";
    public override string ItemLabel => "Region";
    public override string PagePath => "/RegionSetup";
}
