using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class EmployeeMaster : Page
{
    // -------------------------------------------------------
    // Connection string pulled from Web.config
    // -------------------------------------------------------
    private readonly string _connStr =
        ConfigurationManager.ConnectionStrings["HRMSConnection"].ConnectionString;

    // -------------------------------------------------------
    // Page Load
    // -------------------------------------------------------
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            LoadDepartments();
            LoadEmployees();
        }
    }

    // -------------------------------------------------------
    // Populate Department dropdown via ADO.NET
    // -------------------------------------------------------
    private void LoadDepartments()
    {
        using (SqlConnection conn = new SqlConnection(_connStr))
        using (SqlCommand cmd = new SqlCommand("sp_GetDepartments", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            conn.Open();
            SqlDataReader dr = cmd.ExecuteReader();

            ddlDepartment.Items.Clear();
            ddlDepartment.Items.Add(new ListItem("-- Select Department --", ""));

            while (dr.Read())
            {
                ddlDepartment.Items.Add(
                    new ListItem(dr["DepartmentName"].ToString(),
                                 dr["DepartmentID"].ToString()));
            }
        }
    }

    // -------------------------------------------------------
    // Load / Refresh employee grid
    // -------------------------------------------------------
    private void LoadEmployees()
    {
        DataTable dt = new DataTable();

        using (SqlConnection conn = new SqlConnection(_connStr))
        using (SqlCommand cmd = new SqlCommand("sp_GetAllEmployees", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            conn.Open();
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                da.Fill(dt);
            }
        }

        gvEmployee.DataSource = dt;
        gvEmployee.DataBind();

        lblRecordCount.Text = $"Total Records: {dt.Rows.Count}";
    }

    // -------------------------------------------------------
    // SAVE button – Insert or Update
    // -------------------------------------------------------
    protected void btnSave_Click(object sender, EventArgs e)
    {
        string mode       = hdnMode.Value;
        int    employeeID = Convert.ToInt32(hdnEmployeeID.Value);

        try
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                string spName = (mode == "EDIT") ? "sp_UpdateEmployee" : "sp_InsertEmployee";

                using (SqlCommand cmd = new SqlCommand(spName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (mode == "EDIT")
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeID);

                    cmd.Parameters.AddWithValue("@EmployeeCode",
                        txtEmpCode.Text.Trim());
                    cmd.Parameters.AddWithValue("@FirstName",
                        txtFirstName.Text.Trim());
                    cmd.Parameters.AddWithValue("@LastName",
                        txtLastName.Text.Trim());
                    cmd.Parameters.AddWithValue("@Gender",
                        ddlGender.SelectedValue);
                    cmd.Parameters.AddWithValue("@DateOfBirth",
                        string.IsNullOrEmpty(txtDOB.Text)
                            ? (object)DBNull.Value
                            : Convert.ToDateTime(txtDOB.Text));
                    cmd.Parameters.AddWithValue("@Email",
                        txtEmail.Text.Trim());
                    cmd.Parameters.AddWithValue("@Phone",
                        txtPhone.Text.Trim());
                    cmd.Parameters.AddWithValue("@DepartmentID",
                        Convert.ToInt32(ddlDepartment.SelectedValue));
                    cmd.Parameters.AddWithValue("@Designation",
                        txtDesignation.Text.Trim());
                    cmd.Parameters.AddWithValue("@DateOfJoining",
                        Convert.ToDateTime(txtDOJ.Text));
                    cmd.Parameters.AddWithValue("@BasicSalary",
                        Convert.ToDecimal(txtSalary.Text));
                    cmd.Parameters.AddWithValue("@Address",
                        string.IsNullOrEmpty(txtAddress.Text)
                            ? (object)DBNull.Value
                            : txtAddress.Text.Trim());
                    cmd.Parameters.AddWithValue("@Status",
                        ddlStatus.SelectedValue);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            string msg = (mode == "EDIT")
                ? "Employee record updated successfully."
                : "New employee added successfully.";

            ShowMessage(msg, "success");
            ClearForm();
            LoadEmployees();
        }
        catch (SqlException ex)
        {
            // Friendly messages for common SQL violations
            if (ex.Number == 2627 || ex.Number == 2601)
                ShowMessage("Duplicate Employee Code or Email. Please use unique values.", "error");
            else
                ShowMessage("Database error: " + ex.Message, "error");
        }
        catch (Exception ex)
        {
            ShowMessage("An unexpected error occurred: " + ex.Message, "error");
        }
    }

    // -------------------------------------------------------
    // CLEAR button
    // -------------------------------------------------------
    protected void btnClear_Click(object sender, EventArgs e)
    {
        ClearForm();
        pnlMessage.Visible = false;
    }

    // -------------------------------------------------------
    // GridView row command – Edit / Delete
    // -------------------------------------------------------
    protected void gvEmployee_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        int empID = Convert.ToInt32(e.CommandArgument);

        if (e.CommandName == "EditRecord")
        {
            LoadEmployeeForEdit(empID);
        }
        else if (e.CommandName == "DeleteRecord")
        {
            DeleteEmployee(empID);
        }
    }

    // -------------------------------------------------------
    // Load a single employee into the form for editing
    // -------------------------------------------------------
    private void LoadEmployeeForEdit(int employeeID)
    {
        DataTable dt = new DataTable();

        using (SqlConnection conn = new SqlConnection(_connStr))
        using (SqlCommand cmd = new SqlCommand("sp_GetEmployeeByID", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            conn.Open();
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                da.Fill(dt);
            }
        }

        if (dt.Rows.Count == 0) return;

        DataRow row = dt.Rows[0];

        // Re-populate departments first (PostBack wipes it)
        LoadDepartments();

        hdnEmployeeID.Value = employeeID.ToString();
        hdnMode.Value       = "EDIT";
        formTitle.InnerText = "Edit Employee";
        btnSave.Text        = "Update Employee";

        txtEmpCode.Text     = row["EmployeeCode"].ToString();
        txtFirstName.Text   = row["FirstName"].ToString();
        txtLastName.Text    = row["LastName"].ToString();
        ddlGender.SelectedValue = row["Gender"].ToString();

        txtDOB.Text = row["DateOfBirth"] != DBNull.Value
            ? Convert.ToDateTime(row["DateOfBirth"]).ToString("yyyy-MM-dd")
            : "";

        txtEmail.Text       = row["Email"].ToString();
        txtPhone.Text       = row["Phone"].ToString();

        if (ddlDepartment.Items.FindByValue(row["DepartmentID"].ToString()) != null)
            ddlDepartment.SelectedValue = row["DepartmentID"].ToString();

        txtDesignation.Text = row["Designation"].ToString();

        txtDOJ.Text = row["DateOfJoining"] != DBNull.Value
            ? Convert.ToDateTime(row["DateOfJoining"]).ToString("yyyy-MM-dd")
            : "";

        txtSalary.Text  = row["BasicSalary"].ToString();
        txtAddress.Text = row["Address"] != DBNull.Value ? row["Address"].ToString() : "";
        ddlStatus.SelectedValue = row["Status"].ToString();

        // Scroll to top so the form is visible
        ScriptManager.RegisterStartupScript(this, GetType(), "scroll",
            "window.scrollTo({top: 0, behavior: 'smooth'});", true);
    }

    // -------------------------------------------------------
    // Delete an employee record
    // -------------------------------------------------------
    private void DeleteEmployee(int employeeID)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand("sp_DeleteEmployee", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            ShowMessage("Employee deleted successfully.", "success");
            ClearForm();
            LoadEmployees();
        }
        catch (Exception ex)
        {
            ShowMessage("Error deleting record: " + ex.Message, "error");
        }
    }

    // -------------------------------------------------------
    // Clear all form fields and reset to ADD mode
    // -------------------------------------------------------
    private void ClearForm()
    {
        hdnEmployeeID.Value = "0";
        hdnMode.Value       = "ADD";
        formTitle.InnerText = "Add New Employee";
        btnSave.Text        = "Save Employee";

        txtEmpCode.Text     = "";
        txtFirstName.Text   = "";
        txtLastName.Text    = "";
        ddlGender.SelectedIndex = 0;
        txtDOB.Text         = "";
        txtEmail.Text       = "";
        txtPhone.Text       = "";
        ddlDepartment.SelectedIndex = 0;
        txtDesignation.Text = "";
        txtDOJ.Text         = "";
        txtSalary.Text      = "";
        txtAddress.Text     = "";
        ddlStatus.SelectedValue = "Active";
    }

    // -------------------------------------------------------
    // Display an alert message in the page
    // -------------------------------------------------------
    private void ShowMessage(string message, string type)
    {
        pnlMessage.Visible = true;
        alertBox.InnerHtml = message;
        alertBox.Attributes["class"] = $"alert alert-{type}";
    }
}
