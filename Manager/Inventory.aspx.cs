using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Manager
{
    /// <summary>
    /// Focus B: the shelf position, and the two ways a manager is allowed to change it by hand -
    /// a stock take correction and a waste write-off. Both demand a reason and both are refused
    /// if they would drive stock below zero.
    /// </summary>
    public partial class Inventory : SecurePageBase
    {
        protected override string[] AllowedRoles { get { return new[] { RoleNames.Manager }; } }

        protected bool AdjustModalVisible { get; private set; }
        protected bool WasteModalVisible { get; private set; }

        private Dictionary<int, DateTime> _soonestExpiry = new Dictionary<int, DateTime>();

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "Inventory";
            master.PageSubtitle = "Live stock levels, corrections and write-offs";

            if (IsPostBack) return;

            // Deep link from the dashboard or a product page opens straight onto the adjustment.
            int productId;
            if (int.TryParse(Request.QueryString["productId"], out productId))
            {
                ddlView.SelectedValue = "all";
                OpenAdjustModal(productId);
            }

            BindGrid();
        }

        // ----------------------------------------------------------
        // Grid
        // ----------------------------------------------------------

        private void BindGrid()
        {
            _soonestExpiry = InventoryService.GetSoonestExpiryByProduct();

            var products = CatalogService.SearchProducts(txtSearch.Text, null, true, 500);

            switch (ddlView.SelectedValue)
            {
                case "low":
                    products = products.Where(p => p.QuantityOnHand <= p.ReorderLevel).ToList();
                    break;
                case "expiring":
                    var horizon = DateTime.Today.AddDays(14);
                    products = products
                        .Where(p => _soonestExpiry.ContainsKey(p.ProductId) && _soonestExpiry[p.ProductId] <= horizon)
                        .ToList();
                    break;
            }

            products = products.OrderBy(p => p.QuantityOnHand - p.ReorderLevel).ToList();

            rptStock.DataSource = products;
            rptStock.DataBind();
            phNone.Visible = products.Count == 0;
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            alert.Hide();
            BindGrid();
        }

        protected void rptStock_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int productId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out productId)) return;

            if (e.CommandName == "adjust") OpenAdjustModal(productId);
            else if (e.CommandName == "waste") OpenWasteModal(productId);

            BindGrid();
        }

        // ----------------------------------------------------------
        // Adjustment
        // ----------------------------------------------------------

        private void OpenAdjustModal(int productId)
        {
            var product = CatalogService.GetProduct(productId);
            if (product == null)
            {
                alert.ShowError("That product no longer exists.");
                return;
            }

            AdjustModalVisible = true;
            hfAdjustProductId.Value = product.ProductId.ToString();
            litAdjustProduct.Text = Server.HtmlEncode(product.Name);
            litAdjustCurrent.Text = product.QuantityOnHand + " units";
            txtCounted.Text = product.QuantityOnHand.ToString();
            txtAdjustReason.Text = string.Empty;
        }

        protected void btnSaveAdjust_Click(object sender, EventArgs e)
        {
            Page.Validate("Adjust");
            if (!Page.IsValid)
            {
                ReopenAdjust();
                return;
            }

            int productId, counted;
            if (!int.TryParse(hfAdjustProductId.Value, out productId)
                || !int.TryParse(txtCounted.Text, out counted))
            {
                alert.ShowError("Enter the counted quantity as a whole number.");
                ReopenAdjust();
                return;
            }

            try
            {
                InventoryService.AdjustStock(productId, counted, txtAdjustReason.Text, CurrentUser.UserId);
                AdjustModalVisible = false;
                alert.ShowSuccess("Stock adjusted and logged against your name.");
            }
            catch (BusinessRuleException ex)
            {
                alert.ShowError(ex.Message);
                ReopenAdjust();
            }

            BindGrid();
        }

        private void ReopenAdjust()
        {
            AdjustModalVisible = true;

            int productId;
            if (!int.TryParse(hfAdjustProductId.Value, out productId)) return;

            var product = CatalogService.GetProduct(productId);
            if (product == null) return;

            litAdjustProduct.Text = Server.HtmlEncode(product.Name);
            litAdjustCurrent.Text = product.QuantityOnHand + " units";
        }

        // ----------------------------------------------------------
        // Waste
        // ----------------------------------------------------------

        private void OpenWasteModal(int productId)
        {
            var product = CatalogService.GetProduct(productId);
            if (product == null)
            {
                alert.ShowError("That product no longer exists.");
                return;
            }

            WasteModalVisible = true;
            hfWasteProductId.Value = product.ProductId.ToString();
            litWasteProduct.Text = Server.HtmlEncode(product.Name);
            litWasteCurrent.Text = product.QuantityOnHand + " units";
            txtWasteQty.Text = string.Empty;
            txtWasteReason.Text = string.Empty;

            BindBatchList(product.ProductId);
        }

        private void BindBatchList(int productId)
        {
            ddlWasteBatch.Items.Clear();
            ddlWasteBatch.Items.Add(new ListItem("Oldest stock first (recommended)", ""));

            foreach (var batch in InventoryService.GetOpenBatches(productId))
            {
                var label = string.Format("{0} — {1} left, {2}",
                    string.IsNullOrWhiteSpace(batch.BatchCode) ? "batch " + batch.StockBatchId : batch.BatchCode,
                    batch.QuantityRemaining,
                    batch.ExpiryDate.HasValue ? "expires " + batch.ExpiryDate.Value.ToString("dd MMM yyyy") : "no expiry date");

                ddlWasteBatch.Items.Add(new ListItem(label, batch.StockBatchId.ToString()));
            }
        }

        protected void btnSaveWaste_Click(object sender, EventArgs e)
        {
            Page.Validate("Waste");
            if (!Page.IsValid)
            {
                ReopenWaste();
                return;
            }

            int productId, quantity;
            if (!int.TryParse(hfWasteProductId.Value, out productId)
                || !int.TryParse(txtWasteQty.Text, out quantity))
            {
                alert.ShowError("Enter the number of units as a whole number.");
                ReopenWaste();
                return;
            }

            int batchId;
            var batch = int.TryParse(ddlWasteBatch.SelectedValue, out batchId) ? (int?)batchId : null;

            try
            {
                InventoryService.WriteOffWaste(productId, quantity, txtWasteReason.Text, CurrentUser.UserId, batch);
                WasteModalVisible = false;
                alert.ShowSuccess(quantity + " unit(s) written off. The waste report has been updated.");
            }
            catch (BusinessRuleException ex)
            {
                alert.ShowError(ex.Message);
                ReopenWaste();
            }

            BindGrid();
        }

        private void ReopenWaste()
        {
            WasteModalVisible = true;

            int productId;
            if (!int.TryParse(hfWasteProductId.Value, out productId)) return;

            var product = CatalogService.GetProduct(productId);
            if (product == null) return;

            litWasteProduct.Text = Server.HtmlEncode(product.Name);
            litWasteCurrent.Text = product.QuantityOnHand + " units";
            BindBatchList(productId);
        }

        protected void btnCancelModal_Click(object sender, EventArgs e)
        {
            AdjustModalVisible = false;
            WasteModalVisible = false;
            alert.Hide();
            BindGrid();
        }

        // ----------------------------------------------------------
        // Binding helpers
        // ----------------------------------------------------------

        protected string ImageFor(object dataItem)
        {
            var product = (Product)dataItem;
            if (product.ImageData == null && !string.IsNullOrWhiteSpace(product.ImageUrl))
                return product.ImageUrl;
            return ResolveUrl("~/Handlers/ProductImage.ashx?id=" + product.ProductId);
        }

        protected string PillCss(object dataItem)
        {
            var p = (Product)dataItem;
            return StockStatus.CssFor(p.QuantityOnHand, p.ReorderLevel);
        }

        protected string PillText(object dataItem)
        {
            var p = (Product)dataItem;
            return StockStatus.TextFor(p.QuantityOnHand, p.ReorderLevel);
        }

        protected string ExpiryCell(object dataItem)
        {
            var product = (Product)dataItem;

            DateTime soonest;
            if (!_soonestExpiry.TryGetValue(product.ProductId, out soonest))
                return "<span class=\"muted\">—</span>";

            var days = (int)(soonest.Date - DateTime.Today).TotalDays;
            var css = days < 0 ? "pill-red" : days <= 7 ? "pill-amber" : "pill-grey";
            var text = days < 0
                ? "Expired " + Math.Abs(days) + "d ago"
                : days == 0 ? "Today" : soonest.ToString("dd MMM") + " (" + days + "d)";

            return "<span class=\"pill " + css + "\">" + Server.HtmlEncode(text) + "</span>";
        }

        protected string StockValue(object dataItem)
        {
            var product = (Product)dataItem;
            return (product.CostPrice * product.QuantityOnHand).ToString("N2");
        }
    }
}
