using HRMS.Services;
namespace HRMS.Pages;

public class ExpenseCategorySetupModel : LookupSetupPageModel
{
    public ExpenseCategorySetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName => "tblExpenseCategory";
    protected override string IdColumn => "ExpenseCategoryID";
    protected override string NameColumn => "ExpenseCategoryName";

    public override string PageTitle => "Expense Category Setup";
    public override string ItemLabel => "Expense Category";
    public override string PagePath => "/ExpenseCategorySetup";
}
