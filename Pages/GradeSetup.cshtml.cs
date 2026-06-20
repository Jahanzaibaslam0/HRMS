using HRMS.Services;
namespace HRMS.Pages;

public class GradeSetupModel : LookupSetupPageModel
{
    public GradeSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblGrade";
    protected override string IdColumn => "GradeID";
    protected override string NameColumn => "GradeName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Grade Setup";
    public override string ItemLabel => "Grade";
    public override string PagePath => "/GradeSetup";
}
