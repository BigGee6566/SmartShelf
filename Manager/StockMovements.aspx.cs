using System;
using System.Globalization;
using System.Web.UI.WebControls;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Manager
{
    /// <summary>
    /// The stock audit trail. Its whole purpose is answering "why is this figure what it is,
    /// and who changed it" - so every row names a person.
    /// </summary>
    public partial class StockMovements : SecurePageBase
    {
        protected override string[] AllowedRoles { get { return new[] { RoleNames.Manager }; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "Stock movements";
            master.PageSubtitle = "Audit trail";

            if (IsPostBack) return;

            BindProductFilter();

            txtFrom.Text = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd");
            txtTo.Text = DateTime.Today.ToString("yyyy-MM-dd");

            // Arriving from a product page pre-selects that product.
            var productId = Request.QueryString["productId"];
            if (!string.IsNullOrEmpty(productId) && ddlProduct.Items.FindByValue(productId) != null)
                ddlProduct.SelectedValue = productId;

            BindGrid();
        }

        private void BindProductFilter()
        {
            ddlProduct.Items.Clear();
            ddlProduct.Items.Add(new ListItem("All products", ""));
            foreach (var product in CatalogService.SearchProducts(null, null, false, 1000))
            {
                ddlProduct.Items.Add(new ListItem(product.Name, product.ProductId.ToString()));
            }
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            alert.Hide();
            BindGrid();
        }

        private void BindGrid()
        {
            int productId;
            var product = int.TryParse(ddlProduct.SelectedValue, out productId) ? (int?)productId : null;

            MovementType parsedType;
            var type = Enum.TryParse(ddlType.SelectedValue, out parsedType) ? (MovementType?)parsedType : null;

            // Blank means "no bound". Something unreadable is an error rather than a silent
            // widening of the range - a manager must not be shown more than they filtered for.
            DateTime? fromDate, toDate;
            if (!TryReadDate(txtFrom.Text, "\"from\"", out fromDate)) return;
            if (!TryReadDate(txtTo.Text, "\"to\"", out toDate)) return;

            if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            {
                alert.ShowError("The \"from\" date is after the \"to\" date. Swap them and try again.");
                return;
            }

            var movements = InventoryService.GetMovements(product, type, fromDate, toDate, 300);

            rptMovements.DataSource = movements;
            rptMovements.DataBind();
            phNone.Visible = movements.Count == 0;
            litCount.Text = movements.Count.ToString("N0");
        }

        /// <summary>Blank means no bound; anything present must parse or the filter is refused.</summary>
        private bool TryReadDate(string input, string label, out DateTime? value)
        {
            value = null;
            var text = (input ?? string.Empty).Trim();
            if (text.Length == 0) return true;

            DateTime parsed;
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
                || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
            {
                value = parsed;
                return true;
            }

            alert.ShowError("The " + label + " date isn't a valid date. Use the date picker, or clear the box.");
            return false;
        }

        // ---------------- Binding helpers ----------------

        protected string ProductName(object dataItem)
        {
            var m = (StockMovement)dataItem;
            return Server.HtmlEncode(m.Product != null ? m.Product.Name : "—");
        }

        protected string UserName(object dataItem)
        {
            var m = (StockMovement)dataItem;
            return Server.HtmlEncode(m.User != null ? m.User.FullName : "—");
        }

        protected string TypeLabel(object dataItem)
        {
            var m = (StockMovement)dataItem;
            switch (m.MovementType)
            {
                case MovementType.Sale: return "Sale";
                case MovementType.Adjustment: return "Adjustment";
                case MovementType.Receipt: return "Received";
                case MovementType.Waste: return "Waste";
                case MovementType.VoidReversal: return "Void reversal";
                default: return m.MovementType.ToString();
            }
        }

        protected string TypeCss(object dataItem)
        {
            var m = (StockMovement)dataItem;
            switch (m.MovementType)
            {
                case MovementType.Sale: return "pill-blue";
                case MovementType.Adjustment: return "pill-amber";
                case MovementType.Receipt: return "pill-green";
                case MovementType.Waste: return "pill-red";
                default: return "pill-grey";
            }
        }

        protected string DeltaText(object dataItem)
        {
            var m = (StockMovement)dataItem;
            return (m.QuantityDelta > 0 ? "+" : string.Empty) + m.QuantityDelta;
        }

        protected string DeltaStyle(object dataItem)
        {
            var m = (StockMovement)dataItem;
            return m.QuantityDelta < 0
                ? "color:var(--color-accent-dark);font-weight:600;"
                : "color:var(--color-primary);font-weight:600;";
        }
    }
}
