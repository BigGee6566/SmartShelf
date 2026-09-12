using System;
using System.Linq;
using System.Web.UI.WebControls;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Admin
{
    /// <summary>
    /// Account administration. A password only ever travels in one direction here: typed in,
    /// hashed, stored. Nothing on this page can display an existing one.
    /// </summary>
    public partial class Users : SecurePageBase
    {
        protected override string[] AllowedRoles { get { return new[] { RoleNames.Admin }; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "User accounts";
            master.PageSubtitle = "Who can sign in, and what they can reach";

            if (IsPostBack) return;

            BindDropDowns();
            ResetForm();
            BindList();
        }

        private void BindDropDowns()
        {
            ddlRole.Items.Clear();
            foreach (var role in UserService.GetRoles())
            {
                var label = role.Name == RoleNames.Manager ? "Store Manager" : role.Name;
                ddlRole.Items.Add(new ListItem(label, role.RoleId.ToString()));
            }

            ddlSupplier.Items.Clear();
            ddlSupplier.Items.Add(new ListItem("(choose a company)", ""));
            foreach (var supplier in CatalogService.GetSuppliers())
            {
                ddlSupplier.Items.Add(new ListItem(supplier.Name, supplier.SupplierId.ToString()));
            }
        }

        private void BindList()
        {
            rptUsers.DataSource = UserService.GetUsers();
            rptUsers.DataBind();
        }

        /// <summary>The supplier company picker only appears for supplier accounts.</summary>
        protected void ddlRole_Changed(object sender, EventArgs e)
        {
            UpdateSupplierVisibility();
            BindList();
        }

        private void UpdateSupplierVisibility()
        {
            var selected = ddlRole.SelectedItem == null ? string.Empty : ddlRole.SelectedItem.Text;
            phSupplier.Visible = selected == RoleNames.Supplier;
        }

        protected void rptUsers_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int userId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out userId)) return;

            if (e.CommandName == "edit")
            {
                var user = UserService.GetUsers().FirstOrDefault(u => u.UserId == userId);
                if (user == null)
                {
                    alert.ShowError("That user no longer exists.");
                }
                else
                {
                    hfUserId.Value = user.UserId.ToString();
                    txtFullName.Text = user.FullName;
                    txtUsername.Text = user.Username;
                    txtEmail.Text = user.Email;
                    SelectValue(ddlRole, user.RoleId.ToString());
                    UpdateSupplierVisibility();
                    SelectValue(ddlSupplier, user.SupplierId.HasValue ? user.SupplierId.Value.ToString() : "");
                    chkActive.Checked = user.IsActive;
                    txtPassword.Text = string.Empty;
                    litFormTitle.Text = "Edit account";
                    litPasswordHint.Text = "Leave blank to keep the current password. At least 8 characters with a letter and a number.";
                    alert.Hide();
                }
            }
            else if (e.CommandName == "toggle")
            {
                try
                {
                    var user = UserService.GetUser(userId);
                    if (user == null)
                    {
                        alert.ShowError("That user no longer exists.");
                    }
                    else
                    {
                        UserService.SetActive(userId, !user.IsActive, CurrentUser.UserId);
                        alert.ShowSuccess(user.FullName + (user.IsActive ? " can no longer sign in." : " can sign in again."));
                    }
                }
                catch (BusinessRuleException ex)
                {
                    alert.ShowError(ex.Message);
                }
            }

            BindList();
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            Page.Validate("User");
            if (!Page.IsValid) return;

            int userId, roleId;
            int.TryParse(hfUserId.Value, out userId);

            if (!int.TryParse(ddlRole.SelectedValue, out roleId))
            {
                alert.ShowError("Choose a role for this account.");
                return;
            }

            int supplierId;
            var supplier = int.TryParse(ddlSupplier.SelectedValue, out supplierId) ? (int?)supplierId : null;

            try
            {
                UserService.SaveUser(userId, txtUsername.Text, txtFullName.Text, txtEmail.Text,
                                     roleId, supplier, chkActive.Checked, txtPassword.Text,
                                     CurrentUser.UserId);

                alert.ShowSuccess(userId == 0 ? "Account created." : "Account updated.");
                ResetForm();
            }
            catch (BusinessRuleException ex)
            {
                alert.ShowError(ex.Message);
            }

            BindList();
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            alert.Hide();
            ResetForm();
            BindList();
        }

        private void ResetForm()
        {
            hfUserId.Value = "0";
            txtFullName.Text = txtUsername.Text = txtEmail.Text = txtPassword.Text = string.Empty;
            chkActive.Checked = true;
            if (ddlRole.Items.Count > 0) ddlRole.SelectedIndex = 0;
            if (ddlSupplier.Items.Count > 0) ddlSupplier.SelectedIndex = 0;
            UpdateSupplierVisibility();
            litFormTitle.Text = "Add an account";
            litPasswordHint.Text = "Required for a new account. At least 8 characters with a letter and a number.";
        }

        private static void SelectValue(DropDownList list, string value)
        {
            var item = list.Items.FindByValue(value ?? string.Empty);
            if (item != null) list.SelectedValue = item.Value;
        }

        // ---------------- Binding helpers ----------------

        protected string RoleCell(object dataItem)
        {
            var user = (User)dataItem;
            var roleName = user.Role != null ? user.Role.Name : "—";
            var label = roleName == RoleNames.Manager ? "Store Manager" : roleName;

            if (user.Supplier != null)
                label += "<br /><span class=\"muted\" style=\"font-size:12px;\">"
                       + Server.HtmlEncode(user.Supplier.Name) + "</span>";

            return label;
        }

        protected string LastLogin(object dataItem)
        {
            var user = (User)dataItem;
            return user.LastLoginAt.HasValue
                ? user.LastLoginAt.Value.ToString("dd MMM yyyy, HH:mm")
                : "never";
        }
    }
}
