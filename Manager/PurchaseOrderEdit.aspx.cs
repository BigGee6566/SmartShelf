using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Manager
{
    /// <summary>
    /// Raise a new order, or work an existing one - including booking in the goods, which is the
    /// only path by which a delivery raises stock.
    /// </summary>
    public partial class PurchaseOrderEdit : SecurePageBase
    {
        protected override string[] AllowedRoles { get { return new[] { RoleNames.Manager }; } }

        protected bool ReceiveModalVisible { get; private set; }
        protected string StatusCssClass { get; private set; }

        /// <summary>Products the "fill from low stock" button pre-filled, kept across the postback.</summary>
        private Dictionary<int, int> SuggestedQuantities
        {
            get { return (Dictionary<int, int>)(ViewState["Suggested"] ?? new Dictionary<int, int>()); }
            set { ViewState["Suggested"] = value; }
        }

        private int OrderId
        {
            get
            {
                int id;
                return int.TryParse(Request.QueryString["id"], out id) ? id : 0;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = OrderId == 0 ? "Raise a purchase order" : "Purchase order";
            master.PageSubtitle = "Supplier ordering";
            StatusCssClass = "pill-grey";

            if (OrderId != 0)
            {
                phNew.Visible = false;
                phExisting.Visible = true;
                if (!IsPostBack) BindExisting();
                else BindExistingHeaderOnly();
                return;
            }

            if (!IsPostBack)
            {
                BindSupplierList();
                BindNewLines();
            }
        }

        // ==========================================================
        // New order
        // ==========================================================

        private void BindSupplierList()
        {
            ddlSupplier.Items.Clear();
            ddlSupplier.Items.Add(new ListItem("(choose a supplier)", ""));
            foreach (var supplier in CatalogService.GetSuppliers(true))
            {
                ddlSupplier.Items.Add(new ListItem(
                    string.Format("{0} — {1} day lead time", supplier.Name, supplier.LeadTimeDays),
                    supplier.SupplierId.ToString()));
            }
        }

        protected void ddlSupplier_Changed(object sender, EventArgs e)
        {
            alert.Hide();
            SuggestedQuantities = new Dictionary<int, int>();
            BindNewLines();
        }

        private void BindNewLines()
        {
            int supplierId;
            if (!int.TryParse(ddlSupplier.SelectedValue, out supplierId))
            {
                rptLines.DataSource = null;
                rptLines.DataBind();
                phPickSupplier.Visible = true;
                phNewActions.Visible = false;
                return;
            }

            var products = CatalogService.SearchProducts(null, null, true, 500)
                .Where(p => p.SupplierId == supplierId)
                .OrderBy(p => p.QuantityOnHand - p.ReorderLevel)
                .ToList();

            rptLines.DataSource = products;
            rptLines.DataBind();

            phPickSupplier.Visible = products.Count == 0;
            phNewActions.Visible = products.Count > 0;

            if (products.Count == 0)
                alert.ShowInfo("That supplier has no products assigned to them yet. Set a preferred supplier on the product first.");
        }

        /// <summary>Pre-fills the grid with the quantities the low-stock report recommends.</summary>
        protected void btnSuggest_Click(object sender, EventArgs e)
        {
            int supplierId;
            if (!int.TryParse(ddlSupplier.SelectedValue, out supplierId))
            {
                alert.ShowError("Choose a supplier first.");
                return;
            }

            SuggestedQuantities = ReportService.LowStockAlerts()
                .Where(r => r.SupplierId == supplierId)
                .ToDictionary(r => r.ProductId, r => r.SuggestedOrderQty);

            if (SuggestedQuantities.Count == 0)
                alert.ShowInfo("Nothing from this supplier is below its reorder level right now.");
            else
                alert.ShowSuccess(SuggestedQuantities.Count + " product(s) pre-filled from the low-stock report.");

            BindNewLines();
        }

        protected string SuggestedQty(object dataItem)
        {
            var product = (Product)dataItem;
            int qty;
            return SuggestedQuantities.TryGetValue(product.ProductId, out qty) ? qty.ToString() : "0";
        }

        /// <summary>Reads the quantity boxes out of the repeater, ignoring blanks and zeroes.</summary>
        private List<OrderLineRequest> CollectLines()
        {
            var lines = new List<OrderLineRequest>();

            foreach (RepeaterItem item in rptLines.Items)
            {
                var hf = (HiddenField)item.FindControl("hfProductId");
                var txt = (TextBox)item.FindControl("txtQty");
                if (hf == null || txt == null) continue;

                int productId, qty;
                if (!int.TryParse(hf.Value, out productId)) continue;
                if (!int.TryParse(txt.Text, out qty) || qty <= 0) continue;

                lines.Add(new OrderLineRequest { ProductId = productId, Quantity = qty });
            }

            return lines;
        }

        protected void btnCreate_Click(object sender, EventArgs e) { CreateOrder(false); }
        protected void btnCreateAndSend_Click(object sender, EventArgs e) { CreateOrder(true); }

        private void CreateOrder(bool sendImmediately)
        {
            int supplierId;
            if (!int.TryParse(ddlSupplier.SelectedValue, out supplierId))
            {
                alert.ShowError("Choose a supplier for this order.");
                return;
            }

            try
            {
                var order = PurchaseOrderService.CreateOrder(supplierId, CollectLines(),
                                                             txtNotes.Text, CurrentUser.UserId);

                if (sendImmediately)
                    PurchaseOrderService.SendOrder(order.PurchaseOrderId, CurrentUser.UserId);

                Response.Redirect("~/Manager/PurchaseOrderEdit.aspx?id=" + order.PurchaseOrderId, true);
            }
            catch (System.Threading.ThreadAbortException)
            {
                throw; // Response.Redirect
            }
            catch (BusinessRuleException ex)
            {
                alert.ShowError(ex.Message);
                BindNewLines();
            }
        }

        // ==========================================================
        // Existing order
        // ==========================================================

        private void BindExisting()
        {
            var order = PurchaseOrderService.GetOrder(OrderId);
            if (order == null)
            {
                phExisting.Visible = false;
                alert.ShowError("That purchase order no longer exists.");
                return;
            }

            litOrderNumber.Text = Server.HtmlEncode(order.OrderNumber);
            litStatus.Text = PurchaseOrderService.StatusLabel(order.Status);
            StatusCssClass = PurchaseOrderService.StatusPillCss(order.Status);

            litSupplier.Text = Server.HtmlEncode(order.Supplier != null ? order.Supplier.Name : "—");
            litRaisedBy.Text = Server.HtmlEncode(order.CreatedByUser != null ? order.CreatedByUser.FullName : "—");
            litRaisedOn.Text = order.CreatedAt.ToString("dd MMM yyyy");
            litExpected.Text = order.ExpectedDate.ToString("dd MMM yyyy");
            litConfirmed.Text = order.ConfirmedAt.HasValue ? order.ConfirmedAt.Value.ToString("dd MMM yyyy") : "—";
            litDelivered.Text = order.DeliveredAt.HasValue ? order.DeliveredAt.Value.ToString("dd MMM yyyy") : "—";

            var value = order.Lines == null ? 0m : order.Lines.Sum(l => l.QuantityOrdered * l.UnitCost);
            litValue.Text = Money(value);

            if (!string.IsNullOrWhiteSpace(order.Notes))
            {
                phNotes.Visible = true;
                litNotes.Text = Server.HtmlEncode(order.Notes);
            }
            if (!string.IsNullOrWhiteSpace(order.SupplierNote))
            {
                phSupplierNote.Visible = true;
                litSupplierNote.Text = Server.HtmlEncode(order.SupplierNote);
            }

            btnSend.Visible = order.Status == PurchaseOrderStatus.Draft;
            btnCancelOrder.Visible = order.Status == PurchaseOrderStatus.Draft
                                     || order.Status == PurchaseOrderStatus.Sent
                                     || order.Status == PurchaseOrderStatus.Confirmed;

            rptExistingLines.DataSource = order.Lines;
            rptExistingLines.DataBind();
        }

        /// <summary>
        /// On a postback the repeater rebuilds from view state; only the header labels, which are
        /// plain literals, need refreshing.
        /// </summary>
        private void BindExistingHeaderOnly()
        {
            var order = PurchaseOrderService.GetOrder(OrderId);
            if (order == null) return;
            StatusCssClass = PurchaseOrderService.StatusPillCss(order.Status);
        }

        protected void rptExistingLines_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "receive") return;

            int lineId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out lineId)) return;

            OpenReceiveModal(lineId);
        }

        private void OpenReceiveModal(int lineId)
        {
            var order = PurchaseOrderService.GetOrder(OrderId);
            if (order == null || order.Lines == null) return;

            var line = order.Lines.FirstOrDefault(l => l.PurchaseOrderLineId == lineId);
            if (line == null)
            {
                alert.ShowError("That order line no longer exists.");
                return;
            }

            ReceiveModalVisible = true;
            hfLineId.Value = lineId.ToString();
            litReceiveProduct.Text = Server.HtmlEncode(line.Product != null ? line.Product.Name : "Product");

            var outstanding = line.QuantityOrdered - line.QuantityReceived;
            litReceiveOutstanding.Text = outstanding + " unit(s)";
            txtReceiveQty.Text = outstanding.ToString();
            txtExpiry.Text = string.Empty;
            txtBatchCode.Text = string.Empty;

            BindExisting();
        }

        protected void btnSaveReceive_Click(object sender, EventArgs e)
        {
            Page.Validate("Receive");
            if (!Page.IsValid)
            {
                ReceiveModalVisible = true;
                BindExisting();
                return;
            }

            int lineId, quantity;
            if (!int.TryParse(hfLineId.Value, out lineId) || !int.TryParse(txtReceiveQty.Text, out quantity))
            {
                alert.ShowError("Enter the units received as a whole number.");
                ReceiveModalVisible = true;
                BindExisting();
                return;
            }

            DateTime expiry;
            var expiryDate = DateTime.TryParse(txtExpiry.Text, out expiry) ? (DateTime?)expiry : null;

            try
            {
                InventoryService.ReceiveStock(lineId, quantity, expiryDate, txtBatchCode.Text, CurrentUser.UserId);
                ReceiveModalVisible = false;
                alert.ShowSuccess(quantity + " unit(s) booked in. Stock and the batch expiry have been updated.");
            }
            catch (BusinessRuleException ex)
            {
                alert.ShowError(ex.Message);
                ReceiveModalVisible = true;
            }

            BindExisting();
        }

        protected void btnCancelReceive_Click(object sender, EventArgs e)
        {
            ReceiveModalVisible = false;
            alert.Hide();
            BindExisting();
        }

        protected void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                PurchaseOrderService.SendOrder(OrderId, CurrentUser.UserId);
                alert.ShowSuccess("Order sent. The supplier can now see and confirm it.");
            }
            catch (BusinessRuleException ex)
            {
                alert.ShowError(ex.Message);
            }
            BindExisting();
        }

        protected void btnCancelOrder_Click(object sender, EventArgs e)
        {
            try
            {
                PurchaseOrderService.CancelOrder(OrderId, "Cancelled by " + CurrentUser.FullName, CurrentUser.UserId);
                alert.ShowSuccess("Order cancelled.");
            }
            catch (BusinessRuleException ex)
            {
                alert.ShowError(ex.Message);
            }
            BindExisting();
        }

        // ---------------- Binding helpers ----------------

        protected string ProductName(object dataItem)
        {
            var line = (PurchaseOrderLine)dataItem;
            return Server.HtmlEncode(line.Product != null ? line.Product.Name : "—");
        }

        protected string Outstanding(object dataItem)
        {
            var line = (PurchaseOrderLine)dataItem;
            return Math.Max(0, line.QuantityOrdered - line.QuantityReceived).ToString();
        }

        protected bool CanReceive(object dataItem)
        {
            var line = (PurchaseOrderLine)dataItem;
            if (line.QuantityReceived >= line.QuantityOrdered) return false;

            var order = line.PurchaseOrder;
            if (order == null) return true; // status is re-checked in the service anyway

            return order.Status != PurchaseOrderStatus.Draft
                   && order.Status != PurchaseOrderStatus.Cancelled;
        }
    }
}
