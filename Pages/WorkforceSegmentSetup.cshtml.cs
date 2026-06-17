namespace HRMS.Pages;

public class WorkforceSegmentSetupModel : LookupSetupPageModel
{
    public WorkforceSegmentSetupModel(IConfiguration config) : base(config) { }

    protected override string TableName  => "tblWorkforceSegment";
    protected override string IdColumn   => "WorkforceSegmentID";
    protected override string NameColumn => "WorkforceSegmentName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle  => "Workforce Segment Setup";
    public override string ItemLabel  => "Workforce Segment";
    public override string PagePath   => "/WorkforceSegmentSetup";
}
