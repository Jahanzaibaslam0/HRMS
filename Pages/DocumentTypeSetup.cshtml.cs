using HRMS.Services;

namespace HRMS.Pages;

public class DocumentTypeSetupModel : LookupSetupPageModel
{
    public DocumentTypeSetupModel(IConfiguration config, AuthService auth) : base(config, auth) { }

    protected override string TableName  => "tblDocumentType";
    protected override string IdColumn   => "DocumentTypeID";
    protected override string NameColumn => "DocumentTypeName";
    protected override string? AliasColumn => "AliasName";

    public override string PageTitle  => "Document Type Setup";
    public override string ItemLabel  => "Document Type";
    public override string PagePath   => "/DocumentTypeSetup";
}
