using HRMS.Services;
namespace HRMS.Pages;

public class BusinessUnitSetupModel : LookupSetupPageModel
{
    public BusinessUnitSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName  => "tblBusinessUnit";
    protected override string IdColumn   => "BusinessUnitID";
    protected override string NameColumn => "BusinessUnitName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle  => "Business Unit Setup";
    public override string ItemLabel  => "Business Unit";
    public override string PagePath   => "/BusinessUnitSetup";
}
