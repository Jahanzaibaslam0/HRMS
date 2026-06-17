namespace HRMS.Pages;

public class BloodGroupSetupModel : LookupSetupPageModel
{
    public BloodGroupSetupModel(IConfiguration config) : base(config) { }

    protected override string TableName => "tblBloodGroup";
    protected override string IdColumn => "BloodGroupID";
    protected override string NameColumn => "BloodGroupName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Blood Group Setup";
    public override string ItemLabel => "Blood Group";
    public override string PagePath => "/BloodGroupSetup";
}
