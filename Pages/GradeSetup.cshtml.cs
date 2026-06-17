namespace HRMS.Pages;

public class GradeSetupModel : LookupSetupPageModel
{
    public GradeSetupModel(IConfiguration config) : base(config) { }

    protected override string TableName => "tblGrade";
    protected override string IdColumn => "GradeID";
    protected override string NameColumn => "GradeName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle => "Grade Setup";
    public override string ItemLabel => "Grade";
    public override string PagePath => "/GradeSetup";
}
