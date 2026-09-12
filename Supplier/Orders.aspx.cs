using System;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.SupplierPortal
{
    /// <summary>
    /// Focus C, supplier side. Everything on this page is scoped to the company on the signed-in
    /// user's own record - a supplier can never see or act on another supplier's orders, and the
    /// service layer re-checks that on every write.
    /// </summary>
    public partial class Orders : SecurePageBase
    {
        protected override string[] AllowedRoles { get { return new[] { RoleNames.Supplier }; } }

        protected bool DetailModalVisible { get; private set; }
        protected string PendingStyle { get; private set; }

        private int? MySupplierId { get { return CurrentUser.SupplierId; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "My orders";
            master.PageSubtitle = "Purchase orders from Forte Supermarket";
            PendingStyle = string.Empty;

            if (!MySupplierId.HasValue)
            {
                // A supplier login with no company attached can't be scoped safely, so it sees nothing.
                alert.ShowError("Your account isn't linked to a supplier company yet. Ask the store's administrator to fix this.");
                phNone.Visible = true;
                return;
            }

            if (IsPostBack) return;

            BindHeadline();
            BindGrid();
        }

        private void BindHeadline()
        {
            var orders = PurchaseOrderService.GetOrders(MySupplierId, null, 500);

            var pending = orders.Count(o => o.Status == PurchaseOrderStatus.Sent);
            litPending.Text = pending.ToString("N0");
            if (pending > 0) PendingStyle = "color:var(--color-accent-dark);";

            litConfirmed.Text = orders.Count(o => o.Status == PurchaseOrderStatus.Confirmed
                                                  || o.Status == PurchaseOrderStatus.Dispatched
                                                  || o.Status == PurchaseOrderStatus.PartiallyDelivered)
                                      .ToString("N0");
            litDelivered.Text = orders.Count(o => o.Status == PurchaseOrderStatus.Delivered).ToString("N0");

            var value = orders
                .Where(o => o.Status != PurchaseOrderStatus.Cancelled && o.Lines != null)
                .Sum(o => o.Lines.Sum(l => l.QuantityOrdered * l.UnitCost));
            litValue.Text = Money(value);

            var supplier = CatalogService.GetSupplier(MySupplierId.Value);
            litCompany.Text = Server.HtmlEncode(supplier != null ? supplier.Name : "your company");
        }

        private void BindGrid()
        {
            PurchaseOrderStatus parsed;
            var status = Enum.TryParse(ddlStatus.SelectedValue, out parsed) ? (PurchaseOrderStatus?)parsed : null;

            var orders = PurchaseOrderService.GetOrders(MySupplierId, status, 200)
                // A draft has not been sent yet, so it is none of the supplier's business.
                .Where(o => o.Status != PurchaseOrderStatus.Draft)
                .ToList();

            rptOrders.DataSource = orders;
            rptOrders.DataBind();
            phNone.Visible = orders.Count == 0;
        }

        protected void ddlStatus_Changed(object sender, EventArgs e)
        {
            alert.Hide();
            BindGrid();
        }

        protected void rptOrders_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int orderId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out orderId)) return;

            switch (e.CommandName)
            {
                case "view":
                case "confirm":
                case "dispatch":
                    OpenDetail(orderId);
                    break;
            }

            BindHeadline();
            BindGrid();
        }

        private void OpenDetail(int orderId)
        {
            var order = PurchaseOrderService.GetOrder(orderId);

            if (order == null || !MySupplierId.HasValue || order.SupplierId != MySupplierId.Value)
            {
                alert.ShowError("That order isn't one of yours.");
                return;
            }

            DetailModalVisible = true;
            hfOrderId.Value = order.PurchaseOrderId.ToString();

            litDetailNumber.Text = Server.HtmlEncode(order.OrderNumber);
            litDetailMeta.Text = string.Format("{0} · wanted by {1} · {2}",
                PurchaseOrderService.StatusLabel(order.Status),
                order.ExpectedDate.ToString("dd MMM yyyy"),
                Money(order.Lines == null ? 0m : order.Lines.Sum(l => l.QuantityOrdered * l.UnitCost)));

            rptDetailLines.DataSource = order.Lines;
            rptDetailLines.DataBind();

            if (!string.IsNullOrWhiteSpace(order.Notes))
            {
                phStoreNote.Visible = true;
                litStoreNote.Text = Server.HtmlEncode(order.Notes);
            }

            txtSupplierNote.Text = order.SupplierNote;
            txtRevisedDate.Text = order.ExpectedDate.ToString("yyyy-MM-dd");

            var canConfirm = order.Status == PurchaseOrderStatus.Sent;
            // Already-dispatched orders stay actionable so the supplier can revise their note.
            var canDispatch = order.Status == PurchaseOrderStatus.Confirmed
                              || order.Status == PurchaseOrderStatus.Dispatched
                              || order.Status == PurchaseOrderStatus.PartiallyDelivered;

            phActions.Visible = canConfirm || canDispatch;
            phConfirmFields.Visible = canConfirm;
            btnConfirm.Visible = canConfirm;
            btnDispatch.Visible = canDispatch;
        }

        protected void btnConfirm_Click(object sender, EventArgs e)
        {
            int orderId;
            if (!int.TryParse(hfOrderId.Value, out orderId) || !MySupplierId.HasValue) return;

            // A date that will not parse used to be dropped silently, so the supplier was told the
            // order was confirmed on a date the store never received. Say so instead.
            DateTime revised;
            DateTime? revisedDate = null;
            var typedDate = (txtRevisedDate.Text ?? string.Empty).Trim();

            if (typedDate.Length > 0)
            {
                if (!DateTime.TryParse(typedDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out revised)
                    && !DateTime.TryParse(typedDate, CultureInfo.CurrentCulture, DateTimeStyles.None, out revised))
                {
                    alert.ShowError("That delivery date isn't a valid date. Use the date picker, or clear the box to accept the date the store asked for.");
                    OpenDetail(orderId);
                    return;
                }

                if (revised.Date > DateTime.Today.AddYears(2))
                {
                    alert.ShowError("That delivery date is more than two years away - please check it.");
                    OpenDetail(orderId);
                    return;
                }

                revisedDate = revised;
            }

            try
            {
                PurchaseOrderService.ConfirmOrder(orderId, MySupplierId.Value, txtSupplierNote.Text, revisedDate);
                DetailModalVisible = false;
                alert.ShowSuccess("Order confirmed. The store can see your commitment and any note you left.");
            }
            catch (BusinessRuleException ex)
            {
                alert.ShowError(ex.Message);
                OpenDetail(orderId);
            }

            BindHeadline();
            BindGrid();
        }

        protected void btnDispatch_Click(object sender, EventArgs e)
        {
            int orderId;
            if (!int.TryParse(hfOrderId.Value, out orderId) || !MySupplierId.HasValue) return;

            try
            {
                PurchaseOrderService.UpdateDeliveryStatus(orderId, MySupplierId.Value, txtSupplierNote.Text);
                DetailModalVisible = false;
                alert.ShowSuccess("Marked as dispatched. The store will book the stock in when it arrives.");
            }
            catch (BusinessRuleException ex)
            {
                alert.ShowError(ex.Message);
                OpenDetail(orderId);
            }

            BindHeadline();
            BindGrid();
        }

        protected void btnCloseDetail_Click(object sender, EventArgs e)
        {
            DetailModalVisible = false;
            alert.Hide();
            BindHeadline();
            BindGrid();
        }

        // ---------------- Binding helpers ----------------

        protected string SentOn(object dataItem)
        {
            var order = (PurchaseOrder)dataItem;
            return (order.SentAt ?? order.CreatedAt).ToString("dd MMM yyyy");
        }

        protected string ExpectedCell(object dataItem)
        {
            var order = (PurchaseOrder)dataItem;
            var text = order.ExpectedDate.ToString("dd MMM yyyy");

            if (order.IsOpen && order.ExpectedDate.Date < DateTime.Today)
                return "<span style=\"color:var(--color-accent-dark);font-weight:600;\">" + text + " (overdue)</span>";

            return text;
        }

        protected string LineCount(object dataItem)
        {
            var order = (PurchaseOrder)dataItem;
            return order.Lines == null ? "0" : order.Lines.Count.ToString();
        }

        protected string UnitCount(object dataItem)
        {
            var order = (PurchaseOrder)dataItem;
            return order.Lines == null ? "0" : order.Lines.Sum(l => l.QuantityOrdered).ToString("N0");
        }

        protected string OrderValue(object dataItem)
        {
            var order = (PurchaseOrder)dataItem;
            var value = order.Lines == null ? 0m : order.Lines.Sum(l => l.QuantityOrdered * l.UnitCost);
            return value.ToString("N2");
        }

        protected string StatusLabel(object dataItem)
        {
            return PurchaseOrderService.StatusLabel(((PurchaseOrder)dataItem).Status);
        }

        protected string StatusCss(object dataItem)
        {
            return PurchaseOrderService.StatusPillCss(((PurchaseOrder)dataItem).Status);
        }

        protected bool NeedsConfirming(object dataItem)
        {
            return ((PurchaseOrder)dataItem).Status == PurchaseOrderStatus.Sent;
        }

        protected bool CanDispatch(object dataItem)
        {
            var status = ((PurchaseOrder)dataItem).Status;
            return status == PurchaseOrderStatus.Confirmed
                   || status == PurchaseOrderStatus.Dispatched
                   || status == PurchaseOrderStatus.PartiallyDelivered;
        }

        protected string DetailProductName(object dataItem)
        {
            var line = (PurchaseOrderLine)dataItem;
            return Server.HtmlEncode(line.Product != null ? line.Product.Name : "—");
        }
    }
}
