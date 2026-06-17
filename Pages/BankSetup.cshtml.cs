using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class BankRecord
{
    public int BankID { get; set; }
    public string BankName { get; set; } = "";
    public string BranchCode { get; set; } = "";
    public string BranchName { get; set; } = "";
    public string AccountTitle { get; set; } = "";
    public string IBAN { get; set; } = "";
    public string SwiftBICCode { get; set; } = "";
    public string AccountType { get; set; } = "";
    public string AccountVerificationStatus { get; set; } = "Pending";
    public bool IsActive { get; set; } = true;
}

public class BankSetupModel : PageModel
{
    private readonly string _conn;

    public BankSetupModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public BankRecord Input { get; set; } = new();
    public List<BankRecord> Banks { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        if (editId.HasValue && editId > 0)
        {
            LoadForEdit(editId.Value);
        }

        LoadBanks();
    }

    public IActionResult OnPostSave(
        int bankID,
        string bankName,
        string branchCode,
        string branchName,
        string accountTitle,
        string iban,
        string swiftBICCode,
        string accountType,
        string accountVerificationStatus,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(bankName))
        {
            TempData["Alert"] = "Bank Name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (bankID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblBankMaster
                    SET BankName = @BankName,
                        BranchCode = @BranchCode,
                        BranchName = @BranchName,
                        AccountTitle = @AccountTitle,
                        IBAN = @IBAN,
                        SwiftBICCode = @SwiftBICCode,
                        AccountType = @AccountType,
                        AccountVerificationStatus = @AccountVerificationStatus,
                        IsActive = @IsActive,
                        ModifiedOn = GETDATE()
                    WHERE BankID = @BankID;", conn);
                AddSaveParameters(cmd, bankID, bankName, branchCode, branchName, accountTitle, iban, swiftBICCode, accountType, accountVerificationStatus, isActive);
                cmd.ExecuteNonQuery();

                TempData["Alert"] = "Bank Master record updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblBankMaster
                        (BankName, BranchCode, BranchName, AccountTitle, IBAN, SwiftBICCode, AccountType, AccountVerificationStatus, IsActive)
                    VALUES
                        (@BankName, @BranchCode, @BranchName, @AccountTitle, @IBAN, @SwiftBICCode, @AccountType, @AccountVerificationStatus, @IsActive);", conn);
                AddSaveParameters(cmd, bankID, bankName, branchCode, branchName, accountTitle, iban, swiftBICCode, accountType, accountVerificationStatus, isActive);
                cmd.ExecuteNonQuery();

                TempData["Alert"] = "Bank Master record added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "This bank/account combination already exists.";
            TempData["AlertType"] = "error";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                UPDATE tblBankMaster
                SET IsActive = 0,
                    ModifiedOn = GETDATE()
                WHERE BankID = @BankID;", conn);
            cmd.Parameters.AddWithValue("@BankID", deleteId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Bank Master record removed successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error removing record: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private static void AddSaveParameters(
        SqlCommand cmd,
        int bankID,
        string bankName,
        string branchCode,
        string branchName,
        string accountTitle,
        string iban,
        string swiftBICCode,
        string accountType,
        string accountVerificationStatus,
        bool isActive)
    {
        cmd.Parameters.AddWithValue("@BankID", bankID);
        cmd.Parameters.AddWithValue("@BankName", bankName.Trim());
        cmd.Parameters.AddWithValue("@BranchCode", string.IsNullOrWhiteSpace(branchCode) ? DBNull.Value : branchCode.Trim());
        cmd.Parameters.AddWithValue("@BranchName", string.IsNullOrWhiteSpace(branchName) ? DBNull.Value : branchName.Trim());
        cmd.Parameters.AddWithValue("@AccountTitle", string.IsNullOrWhiteSpace(accountTitle) ? DBNull.Value : accountTitle.Trim());
        cmd.Parameters.AddWithValue("@IBAN", string.IsNullOrWhiteSpace(iban) ? DBNull.Value : iban.Trim());
        cmd.Parameters.AddWithValue("@SwiftBICCode", string.IsNullOrWhiteSpace(swiftBICCode) ? DBNull.Value : swiftBICCode.Trim());
        cmd.Parameters.AddWithValue("@AccountType", string.IsNullOrWhiteSpace(accountType) ? DBNull.Value : accountType.Trim());
        cmd.Parameters.AddWithValue("@AccountVerificationStatus", string.IsNullOrWhiteSpace(accountVerificationStatus) ? "Pending" : accountVerificationStatus.Trim());
        cmd.Parameters.AddWithValue("@IsActive", isActive);
    }

    private void LoadForEdit(int bankID)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT BankID, BankName, BranchCode, BranchName, AccountTitle, IBAN, SwiftBICCode,
                   AccountType, AccountVerificationStatus, IsActive
            FROM tblBankMaster
            WHERE BankID = @BankID;", conn);
        cmd.Parameters.AddWithValue("@BankID", bankID);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            Input = ReadBank(dr);
        }
    }

    private void LoadBanks()
    {
        Banks.Clear();

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT BankID, BankName, BranchCode, BranchName, AccountTitle, IBAN, SwiftBICCode,
                   AccountType, AccountVerificationStatus, IsActive
            FROM tblBankMaster
            ORDER BY IsActive DESC, BankName, BranchName;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Banks.Add(ReadBank(dr));
        }
    }

    private static BankRecord ReadBank(SqlDataReader dr)
    {
        return new BankRecord
        {
            BankID = Convert.ToInt32(dr["BankID"]),
            BankName = dr["BankName"].ToString() ?? "",
            BranchCode = dr["BranchCode"].ToString() ?? "",
            BranchName = dr["BranchName"].ToString() ?? "",
            AccountTitle = dr["AccountTitle"].ToString() ?? "",
            IBAN = dr["IBAN"].ToString() ?? "",
            SwiftBICCode = dr["SwiftBICCode"].ToString() ?? "",
            AccountType = dr["AccountType"].ToString() ?? "",
            AccountVerificationStatus = dr["AccountVerificationStatus"].ToString() ?? "Pending",
            IsActive = Convert.ToBoolean(dr["IsActive"])
        };
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;

        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType = TempData["AlertType"]?.ToString() ?? "success";
    }
}
