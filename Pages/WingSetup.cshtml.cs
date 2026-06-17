namespace HRMS.Pages;

public class WingSetupModel : LookupSetupPageModel
{
    public WingSetupModel(IConfiguration config) : base(config) { }

    protected override string TableName => "tblWing";
    protected override string IdColumn => "WingID";
    protected override string NameColumn => "WingName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Wing Setup";
    public override string ItemLabel => "Wing";
    public override string PagePath => "/WingSetup";
}
