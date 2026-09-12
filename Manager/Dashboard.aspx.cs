using System;
using System.Linq;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Manager
{
    /// <summary>
    /// The manager's landing screen. Every tile and table on it is computed from live data -
    /// today's takings, the current shelf position and the outstanding cash-up queue.
    /// </summary>
    public partial class Dashboard : SecurePageBase
    {
        protected override string[] AllowedRoles { get { return new[] { RoleNames.Manager }; } }

        protected string RevenueDeltaCss { get; private set; }
        protected string LowStockStyle { get; private set; }
        protected string LowStockDeltaCss { get; private set; }
        protected string VarianceStyle { get; private set; }
        protected string VarianceDeltaCss { get; private set; }

        private decimal _topSellerMax = 1m;

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "Store Dashboard";
            master.PageSubtitle = AppMaster.TodayLabel();

            RevenueDeltaCss = "muted";
            LowStockStyle = string.Empty;
            LowStockDeltaCss = "muted";
            VarianceStyle = string.Empty;
            VarianceDeltaCss = "muted";

            if (IsPostBack) return;

            BindHeadline();
            BindLowStock();
            BindExpiring();
            BindTopSellers();
            BindQueues();
        }

        private void BindHeadline()
        {
            var today = ReportService.RevenueBetween(DateTime.Today, DateTime.Today);
            var yesterday = ReportService.RevenueBetween(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(-1));

            litRevenue.Text = Money(today);

            if (yesterday > 0m)
            {
                var change = (today - yesterday) / yesterday * 100m;
                RevenueDeltaCss = change >= 0 ? "up" : "down";
                litRevenueDelta.Text = string.Format("{0}{1:N1}% vs yesterday",
                                                     change >= 0 ? "▲ " : "▼ ", Math.Abs(change));
            }
            else
            {
                litRevenueDelta.Text = "No trading yesterday to compare";
            }

            litProducts.Text = ReportService.ActiveProductCount().ToString("N0");
            litCategories.Text = ReportService.CategoryCount() + " categories";

            var lowCount = InventoryService.CountLowStock();
            litLowStock.Text = lowCount.ToString("N0");
            if (lowCount > 0)
            {
                LowStockStyle = "color:var(--color-accent-dark);";
                LowStockDeltaCss = "down";
                litLowStockHint.Text = "At or below reorder level";
            }
            else
            {
                LowStockDeltaCss = "up";
                litLowStockHint.Text = "All products above reorder level";
            }

            var lastCashUp = CashUpService.GetMostRecent();
            if (lastCashUp == null)
            {
                litVariance.Text = "—";
                litVarianceHint.Text = "No cash-up recorded yet";
            }
            else
            {
                litVariance.Text = (lastCashUp.Variance > 0 ? "+" : string.Empty) + Money(lastCashUp.Variance);
                if (lastCashUp.Variance == 0m)
                {
                    VarianceDeltaCss = "up";
                    litVarianceHint.Text = "Drawer balanced";
                }
                else
                {
                    VarianceStyle = "color:var(--color-accent-dark);";
                    VarianceDeltaCss = "down";
                    litVarianceHint.Text = lastCashUp.ReviewedAt.HasValue ? "Reviewed" : "Flagged for review";
                }
            }

            var greetingHour = DateTime.Now.Hour;
            var partOfDay = greetingHour < 12 ? "morning" : greetingHour < 18 ? "afternoon" : "evening";
            var firstName = (CurrentUser.FullName ?? string.Empty).Split(' ')[0];
            litGreeting.Text = Server.HtmlEncode(string.Format("Good {0}, {1}.", partOfDay, firstName));

            litBriefing.Text = Server.HtmlEncode(BuildBriefing(lowCount, lastCashUp));
        }

        /// <summary>One sentence saying what actually needs the manager's attention right now.</summary>
        private static string BuildBriefing(int lowCount, Models.CashUp lastCashUp)
        {
            var parts = new System.Collections.Generic.List<string>();

            if (lowCount > 0)
                parts.Add(string.Format("{0} product{1} need reordering", lowCount, lowCount == 1 ? "" : "s"));

            var pendingReviews = CashUpService.CountUnreviewedVariances();
            if (pendingReviews > 0)
                parts.Add(string.Format("{0} cash-up variance{1} waiting on you",
                                        pendingReviews, pendingReviews == 1 ? "" : "s"));

            var openOrders = PurchaseOrderService.CountOpenOrders();
            if (openOrders > 0)
                parts.Add(string.Format("{0} purchase order{1} still open", openOrders, openOrders == 1 ? "" : "s"));

            if (parts.Count == 0) return "Nothing needs your attention right now - the shelf, the till and the orders all balance.";

            return char.ToUpper(parts[0][0]) + parts[0].Substring(1)
                   + (parts.Count > 1 ? ", and " + string.Join(", ", parts.Skip(1)) : string.Empty) + ".";
        }

        private void BindLowStock()
        {
            var products = InventoryService.GetLowStockProducts(8);
            rptLowStock.DataSource = products;
            rptLowStock.DataBind();

            phNoLowStock.Visible = products.Count == 0;
            phLowTag.Visible = products.Count > 0;
        }

        private void BindExpiring()
        {
            var batches = ReportService.ExpiringBatches(7).Take(8).ToList();
            rptExpiring.DataSource = batches;
            rptExpiring.DataBind();
            phNoExpiry.Visible = batches.Count == 0;
        }

        private void BindTopSellers()
        {
            var rows = ReportService.TopSellers(DateTime.Today.AddDays(-30), DateTime.Today, 6);

            // Bars are scaled against the best seller so the chart reads as a comparison.
            _topSellerMax = rows.Count == 0 ? 1m : Math.Max(1m, rows.Max(r => r.Revenue));

            rptTopSellers.DataSource = rows;
            rptTopSellers.DataBind();
            phNoSales.Visible = rows.Count == 0;
        }

        private void BindQueues()
        {
            var pending = CashUpService.CountUnreviewedVariances();
            litQueueCount.Text = pending.ToString("N0");

            var last = CashUpService.GetMostRecent();
            litLastCashier.Text = last == null || last.User == null
                ? "—"
                : Server.HtmlEncode(last.User.FullName);
            litQueueVariance.Text = last == null
                ? "—"
                : (last.Variance > 0 ? "+" : string.Empty) + Money(last.Variance);

            litOpenPos.Text = PurchaseOrderService.CountOpenOrders().ToString("N0");
            litSuppliers.Text = CatalogService.GetSuppliers(true).Count.ToString("N0");
        }

        // ---------------- Binding helpers ----------------

        protected string ImageFor(object dataItem)
        {
            var product = (Product)dataItem;
            if (product.ImageData == null && !string.IsNullOrWhiteSpace(product.ImageUrl))
                return product.ImageUrl;
            return ResolveUrl("~/Handlers/ProductImage.ashx?id=" + product.ProductId);
        }

        protected string PillCss(object dataItem)
        {
            var product = (Product)dataItem;
            return StockStatus.CssFor(product.QuantityOnHand, product.ReorderLevel);
        }

        protected string PillText(object dataItem)
        {
            var product = (Product)dataItem;
            return StockStatus.TextFor(product.QuantityOnHand, product.ReorderLevel);
        }

        protected string ExpiryPillCss(object dataItem)
        {
            var row = (ExpiryRow)dataItem;
            return row.DaysToExpiry < 0 ? "pill-red" : row.DaysToExpiry <= 3 ? "pill-amber" : "pill-grey";
        }

        protected string ExpiryText(object dataItem)
        {
            var row = (ExpiryRow)dataItem;
            if (row.DaysToExpiry < 0) return "Expired " + Math.Abs(row.DaysToExpiry) + "d ago";
            if (row.DaysToExpiry == 0) return "Today";
            return "In " + row.DaysToExpiry + "d";
        }

        protected string BarWidth(object dataItem)
        {
            var row = (TopSellerRow)dataItem;
            var pct = (int)Math.Round(row.Revenue / _topSellerMax * 100m);
            return Math.Max(4, Math.Min(100, pct)).ToString();
        }

        /// <summary>Keeps the bar-chart labels on one line without truncating mid-word where possible.</summary>
        protected static string Shorten(string name)
        {
            return name != null && name.Length > 18 ? name.Substring(0, 17).TrimEnd() + "…" : name;
        }
    }
}
