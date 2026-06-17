namespace HRMS.Pages;

public class GenderSetupModel : LookupSetupPageModel
{
    public GenderSetupModel(IConfiguration config) : base(config) { }

    protected override string TableName => "tblGender";
    protected override string IdColumn => "GenderID";
    protected override string NameColumn => "GenderName";

    public override string PageTitle => "Gender Setup";
    public override string ItemLabel => "Gender";
    public override string PagePath => "/GenderSetup";
}
