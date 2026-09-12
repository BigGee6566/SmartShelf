using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Manager
{
    /// <summary>
    /// The four Key Reports, on one screen with a tab each. Each one is a live aggregate over the
    /// tables the rest of the system writes to, and each can be exported to CSV as-shown.
    /// </summary>
    public partial class Reports : SecurePageBase
    {
        protected override string[] AllowedRoles { get { return new[] { RoleNames.Manager }; } }

        private const string TabTop = "top";
        private const string TabLow = "low";
        private const string TabExpiry = "expiry";
        private const string TabSupplier = "supplier";

        /// <summary>Which report is on screen. Held in view state so postbacks stay on the same tab.</summary>
        private string ActiveTab
        {
            get
            {
                var stored = (string)ViewState["Tab"];
                if (!string.IsNullOrEmpty(stored)) return stored;

                var requested = (Request.QueryString["tab"] ?? TabTop).ToLowerInvariant();
                return IsKnownTab(requested) ? requested : TabTop;
            }
            set { ViewState["Tab"] = value; }
        }

        private static bool IsKnownTab(string tab)
        {
            return tab == TabTop || tab == TabLow || tab == TabExpiry || tab == TabSupplier;
        }

        private decimal _topSellerTotal = 1m;

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "Reports";
            master.PageSubtitle = "Top sellers · low stock · expiry and waste · supplier performance";

            if (!IsPostBack)
            {
                ActiveTab = ActiveTab; // resolve the query string once, then keep it in view state
                txtFrom.Text = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd");
                txtTo.Text = DateTime.Today.ToString("yyyy-MM-dd");
                BindTabs();
                RunReport();
            }
            else
            {
                BindTabs();
            }
        }

        private void BindTabs()
        {
            rptTabs.DataSource = new[]
            {
                new { Key = TabTop,      Label = "Top-selling products", IsActive = ActiveTab == TabTop },
                new { Key = TabLow,      Label = "Low-stock alerts",     IsActive = ActiveTab == TabLow },
                new { Key = TabExpiry,   Label = "Expiry & waste",       IsActive = ActiveTab == TabExpiry },
                new { Key = TabSupplier, Label = "Supplier performance", IsActive = ActiveTab == TabSupplier }
            };
            rptTabs.DataBind();
        }

        protected void btnRun_Click(object sender, EventArgs e)
        {
            alert.Hide();
            RunReport();
        }

        protected void QuickRange(object sender, CommandEventArgs e)
        {
            int days;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out days)) return;

            txtFrom.Text = DateTime.Today.AddDays(-days).ToString("yyyy-MM-dd");
            txtTo.Text = DateTime.Today.ToString("yyyy-MM-dd");

            alert.Hide();
            RunReport();
        }

        /// <summary>
        /// Reads the date filters. An empty box means "use the default"; a box with something
        /// unreadable in it is an error, because silently reporting a different period than the
        /// one on screen is how a manager ends up trusting the wrong number.
        /// </summary>
        private bool TryGetRange(out DateTime from, out DateTime to)
        {
            from = DateTime.Today.AddDays(-30);
            to = DateTime.Today;

            if (!TryReadDate(txtFrom.Text, "\"from\"", ref from)) return false;
            if (!TryReadDate(txtTo.Text, "\"to\"", ref to)) return false;

            if (from > to)
            {
                alert.ShowError("The \"from\" date is after the \"to\" date. Swap them and run the report again.");
                return false;
            }

            if ((to - from).TotalDays > 366 * 3)
            {
                alert.ShowError("Please choose a range of three years or less.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Blank keeps the supplied default; anything present must parse. Date inputs post
        /// ISO-formatted, so invariant is tried first and the page culture second.
        /// </summary>
        private bool TryReadDate(string input, string label, ref DateTime value)
        {
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

        private void RunReport()
        {
            HideAll();

            DateTime from, to;
            if (!TryGetRange(out from, out to)) return;

            switch (ActiveTab)
            {
                case TabLow: BindLowStock(); break;
                case TabExpiry: BindExpiryAndWaste(from, to); break;
                case TabSupplier: BindSupplierPerformance(from, to); break;
                default: BindTopSellers(from, to); break;
            }
        }

        private void HideAll()
        {
            rptTopSellers.Visible = false;
            rptLowStock.Visible = false;
            phExpiry.Visible = false;
            rptSuppliers.Visible = false;
            phEmpty.Visible = false;
            phDateFilter.Visible = true;
            phExpiryFilter.Visible = false;
        }

        // ----------------------------------------------------------
        // 1. Top-selling products
        // ----------------------------------------------------------
        private void BindTopSellers(DateTime from, DateTime to)
        {
            litReportTitle.Text = "Top-selling products";
            litReportBlurb.Text = "Ranked by revenue over the selected period, taken from completed sale lines. "
                                + "Gross profit compares that revenue to what the stock currently costs.";

            var rows = ReportService.TopSellers(from, to, 30);

            if (rows.Count == 0)
            {
                ShowEmpty("No completed sales in that period.");
                return;
            }

            // The share bar is each product against the best seller in the list.
            _topSellerTotal = Math.Max(1m, rows.Max(r => r.Revenue));

            rptTopSellers.DataSource = rows;
            rptTopSellers.DataBind();
            rptTopSellers.Visible = true;
        }

        protected string ShareWidth(object dataItem)
        {
            var row = (TopSellerRow)dataItem;
            var pct = (int)Math.Round(row.Revenue / _topSellerTotal * 100m);
            return Math.Max(3, Math.Min(100, pct)).ToString();
        }

        protected string ProfitCell(object dataItem)
        {
            var row = (TopSellerRow)dataItem;
            var colour = row.GrossProfit >= 0 ? "var(--color-primary)" : "var(--color-accent-dark)";
            return "<span style=\"color:" + colour + ";font-weight:600;\">R "
                   + row.GrossProfit.ToString("N2") + "</span>";
        }

        // ----------------------------------------------------------
        // 2. Low-stock alerts
        // ----------------------------------------------------------
        private void BindLowStock()
        {
            litReportTitle.Text = "Low-stock alerts";
            litReportBlurb.Text = "Every active product at or below its reorder level, with the quantity needed "
                                + "to get back to three times that level and what it would cost to buy.";

            phDateFilter.Visible = false; // this one is a snapshot, not a period

            var rows = ReportService.LowStockAlerts();

            if (rows.Count == 0)
            {
                ShowEmpty("Nothing is at or below its reorder level. The shelf is in good shape.");
                return;
            }

            rptLowStock.DataSource = rows;
            rptLowStock.DataBind();
            rptLowStock.Visible = true;
        }

        protected string OrderLink(object dataItem)
        {
            var row = (LowStockRow)dataItem;
            if (!row.SupplierId.HasValue) return "<span class=\"muted\">no supplier set</span>";

            return "<a class=\"btn btn-ghost btn-sm\" href=\""
                   + ResolveUrl("~/Manager/PurchaseOrderEdit.aspx") + "\">Order</a>";
        }

        // ----------------------------------------------------------
        // 3. Expiry and waste
        // ----------------------------------------------------------
        private void BindExpiryAndWaste(DateTime from, DateTime to)
        {
            litReportTitle.Text = "Expiry and waste";
            litReportBlurb.Text = "Stock still on the shelf that is close to, or past, its expiry date - and what "
                                + "has already been written off in the selected period.";

            phExpiryFilter.Visible = true;

            int horizon;
            if (!int.TryParse(ddlHorizon.SelectedValue, out horizon)) horizon = 14;

            var expiring = ReportService.ExpiringBatches(horizon);
            var waste = ReportService.WasteSummary(from, to);

            if (expiring.Count == 0 && waste.Count == 0)
            {
                ShowEmpty("Nothing is expiring within " + horizon + " days, and nothing was written off in this period.");
                phExpiryFilter.Visible = true;
                return;
            }

            rptExpiry.DataSource = expiring;
            rptExpiry.DataBind();

            rptWaste.DataSource = waste;
            rptWaste.DataBind();
            phNoWaste.Visible = waste.Count == 0;

            phExpiry.Visible = true;
        }

        protected string ExpiryDateText(object dataItem)
        {
            var row = (ExpiryRow)dataItem;
            return row.ExpiryDate.HasValue ? row.ExpiryDate.Value.ToString("dd MMM yyyy") : "—";
        }

        protected string ExpiryPillCss(object dataItem)
        {
            var row = (ExpiryRow)dataItem;
            if (row.DaysToExpiry < 0) return "pill-red";
            return row.DaysToExpiry <= 3 ? "pill-amber" : "pill-grey";
        }

        protected string ExpiryPillText(object dataItem)
        {
            var row = (ExpiryRow)dataItem;
            if (row.DaysToExpiry < 0) return "Expired " + Math.Abs(row.DaysToExpiry) + " days ago";
            if (row.DaysToExpiry == 0) return "Expires today";
            return "In " + row.DaysToExpiry + " days";
        }

        // ----------------------------------------------------------
        // 4. Supplier performance
        // ----------------------------------------------------------
        private void BindSupplierPerformance(DateTime from, DateTime to)
        {
            litReportTitle.Text = "Supplier performance";
            litReportBlurb.Text = "For orders raised in the selected period: how many arrived, how many arrived by "
                                + "the date promised, how much of what was ordered actually turned up, and how "
                                + "quickly the supplier confirmed.";

            var rows = ReportService.SupplierPerformance(from, to);

            if (rows.Count == 0)
            {
                ShowEmpty("No orders were raised with any supplier in that period.");
                return;
            }

            rptSuppliers.DataSource = rows;
            rptSuppliers.DataBind();
            rptSuppliers.Visible = true;
        }

        /// <summary>A percentage is meaningless with no denominator, so those cells show a dash instead.</summary>
        protected string RateCell(double rate, int denominator)
        {
            if (denominator == 0) return "<span class=\"muted\">—</span>";

            var colour = rate >= 90 ? "var(--color-primary)"
                       : rate >= 70 ? "var(--color-text)"
                       : "var(--color-accent-dark)";

            return "<span style=\"color:" + colour + ";font-weight:600;\">" + rate.ToString("N0") + "%</span>";
        }

        protected string ConfirmCell(object dataItem)
        {
            var row = (SupplierPerformanceRow)dataItem;
            if (!row.AverageConfirmHours.HasValue) return "—";

            var hours = row.AverageConfirmHours.Value;
            return hours < 48 ? hours.ToString("N1") + " h" : (hours / 24).ToString("N1") + " d";
        }

        private void ShowEmpty(string message)
        {
            litEmpty.Text = Server.HtmlEncode(message);
            phEmpty.Visible = true;
        }

        // ----------------------------------------------------------
        // CSV export
        // ----------------------------------------------------------

        protected void btnExport_Click(object sender, EventArgs e)
        {
            DateTime from, to;
            if (!TryGetRange(out from, out to)) return;

            string fileName;
            string csv;

            switch (ActiveTab)
            {
                case TabLow:
                    fileName = "low-stock-alerts";
                    csv = BuildCsv(
                        new[] { "Product", "Category", "Supplier", "On hand", "Reorder level", "Suggested order", "Estimated cost" },
                        ReportService.LowStockAlerts().Select(r => new object[]
                        {
                            r.ProductName, r.CategoryName, r.SupplierName,
                            r.QuantityOnHand, r.ReorderLevel, r.SuggestedOrderQty, r.EstimatedCost
                        }));
                    break;

                case TabExpiry:
                    int horizon;
                    if (!int.TryParse(ddlHorizon.SelectedValue, out horizon)) horizon = 14;
                    fileName = "expiry-and-waste";
                    csv = BuildCsv(
                        new[] { "Product", "Batch", "Units remaining", "Expiry date", "Days to expiry", "Value at risk" },
                        ReportService.ExpiringBatches(horizon).Select(r => new object[]
                        {
                            r.ProductName, r.BatchCode, r.QuantityRemaining,
                            r.ExpiryDate.HasValue ? r.ExpiryDate.Value.ToString("yyyy-MM-dd") : "",
                            r.DaysToExpiry, r.ValueAtRisk
                        }));
                    break;

                case TabSupplier:
                    fileName = "supplier-performance";
                    csv = BuildCsv(
                        new[] { "Supplier", "Orders placed", "Orders delivered", "Delivered on time", "On-time %", "Units ordered", "Units received", "Fill rate %", "Avg confirm hours", "Order value" },
                        ReportService.SupplierPerformance(from, to).Select(r => new object[]
                        {
                            r.SupplierName, r.OrdersPlaced, r.OrdersDelivered, r.OrdersOnTime,
                            r.OnTimeRate.ToString("N1"), r.UnitsOrdered, r.UnitsReceived,
                            r.FillRate.ToString("N1"),
                            r.AverageConfirmHours.HasValue ? r.AverageConfirmHours.Value.ToString("N1") : "",
                            r.OrderValue
                        }));
                    break;

                default:
                    fileName = "top-selling-products";
                    csv = BuildCsv(
                        new[] { "Product", "Category", "Units sold", "Revenue", "Gross profit" },
                        ReportService.TopSellers(from, to, 500).Select(r => new object[]
                        {
                            r.ProductName, r.CategoryName, r.UnitsSold, r.Revenue, r.GrossProfit
                        }));
                    break;
            }

            WriteCsvResponse(fileName + "-" + DateTime.Today.ToString("yyyy-MM-dd") + ".csv", csv);
        }

        private static string BuildCsv(string[] headers, IEnumerable<object[]> rows)
        {
            return CsvWriter.Build(headers, rows);
        }

        /// <summary>
        /// Quotes any field containing a comma, quote or newline, and doubles embedded quotes -
        /// otherwise a product name with a comma in it would shift every column after it.
        /// </summary>
        private static string EscapeCsv(string value)
        {
            return CsvWriter.Escape(value);
        }

        private void WriteCsvResponse(string fileName, string csv)
        {
            CsvWriter.Send(Response, fileName, csv);
        }
    }
}
