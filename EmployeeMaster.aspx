<%@ Page Language="C#" AutoEventWireup="true" CodeFile="EmployeeMaster.aspx.cs" Inherits="EmployeeMaster" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>HRMS – Employee Master</title>
    <link rel="stylesheet" href="css/style.css" />
</head>
<body>
    <form id="frmEmployee" runat="server">

        <!-- ===== TOP NAVBAR (App Review Mode) ===== -->
        <header class="navbar">
            <div class="navbar-brand">
                <svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" viewBox="0 0 24 24" fill="none"
                     stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
                    <circle cx="9" cy="7" r="4"/>
                    <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
                    <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
                </svg>
                <span>HRMS &ndash; Employee Master &mdash; <strong>App Review Mode</strong></span>
            </div>
            <div class="navbar-right">
                <span id="clock" class="clock"></span>
                <span class="review-badge">Review Mode</span>
            </div>
        </header>
   

        <main class="container">

            <!-- ===== ALERT BAR ===== -->
            <asp:Panel ID="pnlMessage" runat="server" Visible="false">
                <div id="alertBox" class="alert" runat="server"></div>
            </asp:Panel>

            <!-- ===== FORM CARD ===== -->
            <div class="card">
                <div class="card-header">
                    <h2 id="formTitle" runat="server">Add New Employee</h2>
                    <asp:HiddenField ID="hdnEmployeeID" runat="server" Value="0" />
                    <asp:HiddenField ID="hdnMode" runat="server" Value="ADD" />
                </div>

                <div class="card-body">
                    <div class="form-grid">

                        <!-- Employee Code -->
                        <div class="form-group">
                            <label for="txtEmpCode">Employee Code <span class="required">*</span></label>
                            <asp:TextBox ID="txtEmpCode" runat="server" CssClass="form-control"
                                         placeholder="e.g. EMP-001" MaxLength="20" />
                            <span class="field-error" id="errEmpCode"></span>
                        </div>

                        <!-- First Name -->
                        <div class="form-group">
                            <label for="txtFirstName">First Name <span class="required">*</span></label>
                            <asp:TextBox ID="txtFirstName" runat="server" CssClass="form-control"
                                         placeholder="First name" MaxLength="100" />
                            <span class="field-error" id="errFirstName"></span>
                        </div>

                        <!-- Last Name -->
                        <div class="form-group">
                            <label for="txtLastName">Last Name <span class="required">*</span></label>
                            <asp:TextBox ID="txtLastName" runat="server" CssClass="form-control"
                                         placeholder="Last name" MaxLength="100" />
                            <span class="field-error" id="errLastName"></span>
                        </div>

                        <!-- Gender -->
                        <div class="form-group">
                            <label>Gender <span class="required">*</span></label>
                            <asp:DropDownList ID="ddlGender" runat="server" CssClass="form-control">
                                <asp:ListItem Value="">-- Select --</asp:ListItem>
                                <asp:ListItem Value="Male">Male</asp:ListItem>
                                <asp:ListItem Value="Female">Female</asp:ListItem>
                                <asp:ListItem Value="Other">Other</asp:ListItem>
                            </asp:DropDownList>
                            <span class="field-error" id="errGender"></span>
                        </div>

                        <!-- Date of Birth -->
                        <div class="form-group">
                            <label for="txtDOB">Date of Birth</label>
                            <asp:TextBox ID="txtDOB" runat="server" CssClass="form-control"
                                         TextMode="Date" />
                        </div>

                        <!-- Email -->
                        <div class="form-group">
                            <label for="txtEmail">Email <span class="required">*</span></label>
                            <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control"
                                         TextMode="Email" placeholder="email@company.com" MaxLength="150" />
                            <span class="field-error" id="errEmail"></span>
                        </div>

                        <!-- Phone -->
                        <div class="form-group">
                            <label for="txtPhone">Phone <span class="required">*</span></label>
                            <asp:TextBox ID="txtPhone" runat="server" CssClass="form-control"
                                         placeholder="+92-300-0000000" MaxLength="20" />
                            <span class="field-error" id="errPhone"></span>
                        </div>

                        <!-- Department -->
                        <div class="form-group">
                            <label>Department <span class="required">*</span></label>
                            <asp:DropDownList ID="ddlDepartment" runat="server" CssClass="form-control">
                            </asp:DropDownList>
                            <span class="field-error" id="errDepartment"></span>
                        </div>

                        <!-- Designation -->
                        <div class="form-group">
                            <label for="txtDesignation">Designation <span class="required">*</span></label>
                            <asp:TextBox ID="txtDesignation" runat="server" CssClass="form-control"
                                         placeholder="e.g. Software Engineer" MaxLength="100" />
                            <span class="field-error" id="errDesignation"></span>
                        </div>

                        <!-- Date of Joining -->
                        <div class="form-group">
                            <label for="txtDOJ">Date of Joining <span class="required">*</span></label>
                            <asp:TextBox ID="txtDOJ" runat="server" CssClass="form-control"
                                         TextMode="Date" />
                            <span class="field-error" id="errDOJ"></span>
                        </div>

                        <!-- Basic Salary -->
                        <div class="form-group">
                            <label for="txtSalary">Basic Salary <span class="required">*</span></label>
                            <asp:TextBox ID="txtSalary" runat="server" CssClass="form-control"
                                         TextMode="Number" placeholder="0.00" />
                            <span class="field-error" id="errSalary"></span>
                        </div>

                        <!-- Status -->
                        <div class="form-group">
                            <label>Status <span class="required">*</span></label>
                            <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-control">
                                <asp:ListItem Value="Active" Selected="True">Active</asp:ListItem>
                                <asp:ListItem Value="Inactive">Inactive</asp:ListItem>
                            </asp:DropDownList>
                        </div>

                        <!-- Address (full width) -->
                        <div class="form-group full-width">
                            <label for="txtAddress">Address</label>
                            <asp:TextBox ID="txtAddress" runat="server" CssClass="form-control"
                                         TextMode="MultiLine" Rows="3" placeholder="Residential address..." />
                        </div>

                    </div><!-- /form-grid -->
                </div><!-- /card-body -->

                <div class="card-footer">
                    <asp:Button ID="btnSave" runat="server" Text="Save Employee"
                                CssClass="btn btn-primary" OnClick="btnSave_Click"
                                OnClientClick="return validateForm();" />
                    <asp:Button ID="btnClear" runat="server" Text="Clear Form"
                                CssClass="btn btn-secondary" OnClick="btnClear_Click"
                                CausesValidation="false" />
                </div>
            </div><!-- /card -->

            <!-- ===== EMPLOYEE LIST CARD ===== -->
            <div class="card mt-4">
                <div class="card-header space-between">
                    <h2>Employee List</h2>
                    <div class="search-box">
                        <input type="text" id="txtSearch" class="form-control"
                               placeholder="Search by name / code / dept..."
                               onkeyup="searchTable(this.value)" />
                    </div>
                </div>

                <div class="card-body table-responsive">
                    <asp:GridView ID="gvEmployee" runat="server"
                                  CssClass="data-table"
                                  AutoGenerateColumns="false"
                                  DataKeyNames="EmployeeID"
                                  OnRowCommand="gvEmployee_RowCommand"
                                  EmptyDataText="No employee records found."
                                  EmptyDataRowStyle-CssClass="empty-row">

                        <Columns>

                            <asp:BoundField DataField="EmployeeCode"  HeaderText="Emp Code"   />
                            <asp:BoundField DataField="FullName"      HeaderText="Full Name"  />
                            <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                            <asp:BoundField DataField="Designation"   HeaderText="Designation"/>
                            <asp:BoundField DataField="Phone"         HeaderText="Phone"      />
                            <asp:BoundField DataField="Email"         HeaderText="Email"      />
                            <asp:BoundField DataField="DateOfJoining" HeaderText="Joined"
                                            DataFormatString="{0:dd MMM yyyy}" />
                            <asp:BoundField DataField="BasicSalary"   HeaderText="Salary"
                                            DataFormatString="{0:N2}" />

                            <!-- Status badge -->
                            <asp:TemplateField HeaderText="Status">
                                <ItemTemplate>
                                    <span class='badge <%# Eval("Status").ToString() == "Active" ? "badge-success" : "badge-danger" %>'>
                                        <%# Eval("Status") %>
                                    </span>
                                </ItemTemplate>
                            </asp:TemplateField>

                            <!-- Actions -->
                            <asp:TemplateField HeaderText="Actions" ItemStyle-CssClass="actions-col">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbEdit" runat="server"
                                                   CommandName="EditRecord"
                                                   CommandArgument='<%# Eval("EmployeeID") %>'
                                                   CssClass="btn-icon btn-edit"
                                                   ToolTip="Edit">
                                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none"
                                             stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                                            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                                        </svg>
                                    </asp:LinkButton>

                                    <asp:LinkButton ID="lbDelete" runat="server"
                                                   CommandName="DeleteRecord"
                                                   CommandArgument='<%# Eval("EmployeeID") %>'
                                                   CssClass="btn-icon btn-delete"
                                                   ToolTip="Delete"
                                                   OnClientClick="return confirmDelete();">
                                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none"
                                             stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                            <polyline points="3 6 5 6 21 6"/>
                                            <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                                            <path d="M10 11v6"/>
                                            <path d="M14 11v6"/>
                                            <path d="M9 6V4h6v2"/>
                                        </svg>
                                    </asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>

                        </Columns>

                        <HeaderStyle CssClass="grid-header" />
                        <RowStyle CssClass="grid-row" />
                        <AlternatingRowStyle CssClass="grid-alt-row" />

                    </asp:GridView>
                </div>

                <div class="card-footer">
                    <asp:Label ID="lblRecordCount" runat="server" CssClass="record-count" Text="" />
                </div>
            </div><!-- /card -->

        </main><!-- /container -->

    </form>

    <script src="js/app.js"></script>
</body>
</html>
