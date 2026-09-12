using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Manager
{
    /// <summary>List and edit product categories, with the add/edit form beside the list.</summary>
    public partial class Categories : SecurePageBase
    {
        protected override string[] AllowedRoles { get { return new[] { RoleNames.Manager }; } }

        /// <summary>Counted once per render rather than once per row, to avoid a query per category.</summary>
        private Dictionary<int, int> _productCounts = new Dictionary<int, int>();

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "Categories";
            master.PageSubtitle = "How the catalogue is grouped";

            if (IsPostBack) return;

            ResetForm();
            BindList();
        }

        private void BindList()
        {
            var categories = CatalogService.GetCategories();

            _productCounts = CatalogService
                .SearchProducts(null, null, false, 5000)
                .GroupBy(p => p.CategoryId)
                .ToDictionary(g => g.Key, g => g.Count());

            rptCategories.DataSource = categories;
            rptCategories.DataBind();
        }

        protected string ProductCount(int categoryId)
        {
            int count;
            return _productCounts.TryGetValue(categoryId, out count) ? count.ToString() : "0";
        }

        protected void rptCategories_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int categoryId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out categoryId)) return;

            if (e.CommandName == "edit")
            {
                var category = CatalogService.GetCategories().FirstOrDefault(c => c.CategoryId == categoryId);
                if (category == null)
                {
                    alert.ShowError("That category no longer exists.");
                }
                else
                {
                    hfCategoryId.Value = category.CategoryId.ToString();
                    txtName.Text = category.Name;
                    txtDescription.Text = category.Description;
                    chkActive.Checked = category.IsActive;
                    litFormTitle.Text = "Edit category";
                    alert.Hide();
                }
            }
            else if (e.CommandName == "delete")
            {
                try
                {
                    CatalogService.DeleteCategory(categoryId);
                    alert.ShowSuccess("Category deleted.");
                    ResetForm();
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
            Page.Validate("Category");
            if (!Page.IsValid) return;

            int categoryId;
            int.TryParse(hfCategoryId.Value, out categoryId);

            try
            {
                CatalogService.SaveCategory(categoryId, txtName.Text, txtDescription.Text, chkActive.Checked);
                alert.ShowSuccess(categoryId == 0 ? "Category added." : "Category updated.");
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
            hfCategoryId.Value = "0";
            txtName.Text = string.Empty;
            txtDescription.Text = string.Empty;
            chkActive.Checked = true;
            litFormTitle.Text = "Add a category";
        }
    }
}
