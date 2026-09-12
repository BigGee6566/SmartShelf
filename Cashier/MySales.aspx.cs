using System;
using System.Linq;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Cashier
{
    /// <summary>
    /// A cashier's own trading history and the running state of their shift. It is the same
    /// arithmetic the cash-up screen uses, shown before the drawer is counted.
    /// </summary>
    public partial class MySales : SecurePageBase
    {
        protected override string[] AllowedRoles
        {
            get { return new[] { RoleNames.Cashier, RoleNames.Manager }; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "My Sales";
            master.PageSubtitle = AppMaster.TodayLabel();

            if (IsPostBack) return;

            BindShift();
            BindSales();
        }

        private void BindShift()
        {
            var shift = CashUpService.GetOpenShiftSummary(CurrentUser.UserId);

            if (shift == null)
            {
                litShiftCount.Text = "0";
                litShiftTill.Text = "No shift open";
                litShiftTotal.Text = Money(0m);
                litExpectedCash.Text = Money(0m);
                litShiftOpened.Text = "—";
                litShiftState.Text = "A shift opens on your first sale";
                return;
            }

            litShiftCount.Text = shift.SaleCount.ToString("N0");
            litShiftTill.Text = shift.TillNumber;
            litShiftTotal.Text = Money(shift.CashSales + shift.CardSales + shift.MobileSales);
            litExpectedCash.Text = Money(shift.ExpectedCash);
            litShiftOpened.Text = shift.OpenedAt.ToString("HH:mm");
            litShiftState.Text = "Open since " + shift.OpenedAt.ToString("dd MMM");
        }

        private void BindSales()
        {
            var sales = SaleService.GetRecentSales(CurrentUser.UserId, 50);
            rptSales.DataSource = sales;
            rptSales.DataBind();
            phNone.Visible = sales.Count == 0;
        }

        protected string ItemCount(object dataItem)
        {
            var sale = (Sale)dataItem;
            return sale.Lines == null ? "0" : sale.Lines.Sum(l => l.Quantity).ToString();
        }

        protected string StatusCss(object dataItem)
        {
            var sale = (Sale)dataItem;
            return sale.Status == SaleStatus.Completed ? "pill-green" : "pill-red";
        }
    }
}
