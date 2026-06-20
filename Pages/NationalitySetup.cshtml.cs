using HRMS.Services;
namespace HRMS.Pages;

public class NationalitySetupModel : LookupSetupPageModel
{
    public NationalitySetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblNationality";
    protected override string IdColumn => "NationalityID";
    protected override string NameColumn => "NationalityName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Nationality Setup";
    public override string ItemLabel => "Nationality";
    public override string PagePath => "/NationalitySetup";
}
