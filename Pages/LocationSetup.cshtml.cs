namespace HRMS.Pages;

public class LocationSetupModel : LookupSetupPageModel
{
    public LocationSetupModel(IConfiguration config) : base(config) { }

    protected override string TableName => "tblLocation";
    protected override string IdColumn => "LocationID";
    protected override string NameColumn => "LocationName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Location Setup";
    public override string ItemLabel => "Location";
    public override string PagePath => "/LocationSetup";
}
