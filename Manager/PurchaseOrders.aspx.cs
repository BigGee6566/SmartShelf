using System;
using System.Linq;
using System.Web.UI.WebControls;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Manager
{
    /// <summary>Focus C, store side: the list of orders raised with suppliers.</summary>
    public partial class PurchaseOrders : SecurePageBase
    {
        protected override string[] AllowedRoles { get { return new[] { RoleNames.Manager }; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "Purchase orders";
            master.PageSubtitle = "Ordering and deliveries";

            if (IsPostBack) return;

            ddlSupplier.Items.Clear();
            ddlSupplier.Items.Add(new ListItem("All suppliers", ""));
            foreach (var supplier in CatalogService.GetSuppliers())
            {
                ddlSupplier.Items.Add(new ListItem(supplier.Name, supplier.SupplierId.ToString()));
            }

            if (Request.QueryString["saved"] == "1") alert.ShowSuccess("Purchase order saved.");

            BindGrid();
        }

        private void BindGrid()
        {
            int supplierId;
            var supplier = int.TryParse(ddlSupplier.SelectedValue, out supplierId) ? (int?)supplierId : null;

            PurchaseOrderStatus parsed;
            var status = Enum.TryParse(ddlStatus.SelectedValue, out parsed) ? (PurchaseOrderStatus?)parsed : null;

            var orders = PurchaseOrderService.GetOrders(supplier, status);

            rptOrders.DataSource = orders;
            rptOrders.DataBind();
            phNone.Visible = orders.Count == 0;
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            alert.Hide();
            BindGrid();
        }

        protected void rptOrders_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "send") return;

            int orderId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out orderId)) return;

            try
            {
                PurchaseOrderService.SendOrder(orderId, CurrentUser.UserId);
                alert.ShowSuccess("Order sent. It is now visible in the supplier's portal for them to confirm.");
            }
            catch (BusinessRuleException ex)
            {
                alert.ShowError(ex.Message);
            }

            BindGrid();
        }

        // ---------------- Binding helpers ----------------

        protected string SupplierName(object dataItem)
        {
            var order = (PurchaseOrder)dataItem;
            return Server.HtmlEncode(order.Supplier != null ? order.Supplier.Name : "—");
        }

        protected string LineCount(object dataItem)
        {
            var order = (PurchaseOrder)dataItem;
            return order.Lines == null ? "0" : order.Lines.Count.ToString();
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

        protected bool IsDraft(object dataItem)
        {
            return ((PurchaseOrder)dataItem).Status == PurchaseOrderStatus.Draft;
        }

        /// <summary>Flags an open order whose expected date has already passed.</summary>
        protected string ExpectedCell(object dataItem)
        {
            var order = (PurchaseOrder)dataItem;
            var text = order.ExpectedDate.ToString("dd MMM yyyy");

            if (order.IsOpen && order.ExpectedDate.Date < DateTime.Today)
                return "<span style=\"color:var(--color-accent-dark);font-weight:600;\">" + text + " (overdue)</span>";

            return text;
        }

        protected string Progress(object dataItem)
        {
            var order = (PurchaseOrder)dataItem;
            if (order.Lines == null || order.Lines.Count == 0) return "—";

            var ordered = order.Lines.Sum(l => l.QuantityOrdered);
            var received = order.Lines.Sum(l => l.QuantityReceived);
            if (ordered == 0) return "—";

            return string.Format("{0} of {1} units ({2:N0}%)", received, ordered, received * 100.0 / ordered);
        }
    }
}
