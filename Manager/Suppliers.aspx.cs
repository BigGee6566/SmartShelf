using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Manager
{
    /// <summary>Supplier records: who they are, how to reach them, and how long they take.</summary>
    public partial class Suppliers : SecurePageBase
    {
        protected override string[] AllowedRoles { get { return new[] { RoleNames.Manager }; } }

        private Dictionary<int, int> _productCounts = new Dictionary<int, int>();

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "Suppliers";
            master.PageSubtitle = "Who the store orders from";

            if (IsPostBack) return;

            ResetForm();
            BindList();
        }

        private void BindList()
        {
            _productCounts = CatalogService
                .SearchProducts(null, null, false, 5000)
                .Where(p => p.SupplierId.HasValue)
                .GroupBy(p => p.SupplierId.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            rptSuppliers.DataSource = CatalogService.GetSuppliers();
            rptSuppliers.DataBind();
        }

        protected string ProductCount(int supplierId)
        {
            int count;
            return _productCounts.TryGetValue(supplierId, out count) ? count.ToString() : "0";
        }

        protected void rptSuppliers_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "edit") return;

            int supplierId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out supplierId)) return;

            var supplier = CatalogService.GetSupplier(supplierId);
            if (supplier == null)
            {
                alert.ShowError("That supplier no longer exists.");
            }
            else
            {
                hfSupplierId.Value = supplier.SupplierId.ToString();
                txtName.Text = supplier.Name;
                txtContact.Text = supplier.ContactPerson;
                txtEmail.Text = supplier.Email;
                txtPhone.Text = supplier.Phone;
                txtLeadTime.Text = supplier.LeadTimeDays.ToString();
                chkActive.Checked = supplier.IsActive;
                litFormTitle.Text = "Edit supplier";
                alert.Hide();
            }

            BindList();
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            Page.Validate("Supplier");
            if (!Page.IsValid) return;

            int supplierId, leadTime;
            int.TryParse(hfSupplierId.Value, out supplierId);

            if (!int.TryParse(txtLeadTime.Text, out leadTime))
            {
                alert.ShowError("Enter the lead time as a whole number of days.");
                return;
            }

            try
            {
                CatalogService.SaveSupplier(new Supplier
                {
                    SupplierId = supplierId,
                    Name = txtName.Text,
                    ContactPerson = txtContact.Text,
                    Email = txtEmail.Text,
                    Phone = txtPhone.Text,
                    LeadTimeDays = leadTime,
                    IsActive = chkActive.Checked
                });

                alert.ShowSuccess(supplierId == 0 ? "Supplier added." : "Supplier updated.");
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
            hfSupplierId.Value = "0";
            txtName.Text = txtContact.Text = txtEmail.Text = txtPhone.Text = string.Empty;
            txtLeadTime.Text = "3";
            chkActive.Checked = true;
            litFormTitle.Text = "Add a supplier";
        }
    }
}
