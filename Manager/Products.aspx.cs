using System;
using System.Web.UI.WebControls;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Manager
{
    /// <summary>Focus A: the priced, categorised catalogue.</summary>
    public partial class Products : SecurePageBase
    {
        protected override string[] AllowedRoles { get { return new[] { RoleNames.Manager }; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "Products";
            master.PageSubtitle = "Catalogue, pricing and categories";

            if (IsPostBack) return;

            BindCategoryFilter();

            // Coming back from a save? Say so, then bind.
            if (Request.QueryString["saved"] == "1")
                alert.ShowSuccess("Product saved.");

            BindProducts();
        }

        private void BindCategoryFilter()
        {
            ddlCategory.Items.Clear();
            ddlCategory.Items.Add(new ListItem("All categories", ""));
            foreach (var category in CatalogService.GetCategories())
            {
                ddlCategory.Items.Add(new ListItem(category.Name, category.CategoryId.ToString()));
            }
        }

        private void BindProducts()
        {
            int categoryId;
            var category = int.TryParse(ddlCategory.SelectedValue, out categoryId) ? (int?)categoryId : null;
            var activeOnly = ddlStatus.SelectedValue != "all";

            var products = CatalogService.SearchProducts(txtSearch.Text, category, activeOnly);

            rptProducts.DataSource = products;
            rptProducts.DataBind();
            phNone.Visible = products.Count == 0;
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            alert.Hide();
            BindProducts();
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            alert.Hide();
            txtSearch.Text = string.Empty;
            ddlCategory.SelectedIndex = 0;
            ddlStatus.SelectedIndex = 0;
            BindProducts();
        }

        protected void rptProducts_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "toggle") return;

            int productId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out productId)) return;

            try
            {
                var product = CatalogService.GetProduct(productId);
                if (product == null)
                {
                    alert.ShowError("That product no longer exists.");
                }
                else
                {
                    CatalogService.SetProductActive(productId, !product.IsActive);
                    alert.ShowSuccess(product.Name + (product.IsActive
                        ? " has been delisted. Its sales history is untouched."
                        : " is back on the catalogue."));
                }
            }
            catch (BusinessRuleException ex)
            {
                alert.ShowError(ex.Message);
            }

            BindProducts();
        }

        // ---------------- Binding helpers ----------------

        protected string ImageFor(object dataItem)
        {
            var product = (Product)dataItem;
            if (product.ImageData == null && !string.IsNullOrWhiteSpace(product.ImageUrl))
                return product.ImageUrl;
            return ResolveUrl("~/Handlers/ProductImage.ashx?id=" + product.ProductId);
        }

        protected string CategoryName(object dataItem)
        {
            var product = (Product)dataItem;
            return Server.HtmlEncode(product.Category != null ? product.Category.Name : "—");
        }

        protected string SupplierName(object dataItem)
        {
            var product = (Product)dataItem;
            return Server.HtmlEncode(product.Supplier != null ? product.Supplier.Name : "—");
        }

        protected string VatLabel(object dataItem)
        {
            var product = (Product)dataItem;
            return product.VatRate <= 0m ? "zero" : (product.VatRate * 100m).ToString("0.#") + "%";
        }

        /// <summary>Delisted products show that first; otherwise the pill reports the stock position.</summary>
        protected string RowPillCss(object dataItem)
        {
            var product = (Product)dataItem;
            if (!product.IsActive) return "pill-grey";
            return StockStatus.CssFor(product.QuantityOnHand, product.ReorderLevel);
        }

        protected string RowPillText(object dataItem)
        {
            var product = (Product)dataItem;
            if (!product.IsActive) return "Delisted";
            return StockStatus.TextFor(product.QuantityOnHand, product.ReorderLevel);
        }
    }
}
