using System;
using System.Linq;
using System.Web.UI.WebControls;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Manager
{
    /// <summary>
    /// Focus E, manager side: the reconciliation queue. Every variance is traceable to the
    /// cashier who counted the drawer, and signing one off records who accepted it.
    /// </summary>
    public partial class CashUpReview : SecurePageBase
    {
        protected override string[] AllowedRoles { get { return new[] { RoleNames.Manager }; } }

        protected bool ReviewModalVisible { get; private set; }
        protected string PendingStyle { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "Cash-up review";
            master.PageSubtitle = "Reconciliation";
            PendingStyle = string.Empty;

            if (IsPostBack) return;

            BindHeadline();
            BindGrid();
        }

        private void BindHeadline()
        {
            var pending = CashUpService.CountUnreviewedVariances();
            litPending.Text = pending.ToString("N0");
            if (pending > 0) PendingStyle = "color:var(--color-accent-dark);";

            var recent = CashUpService.GetCashUps(false, 500)
                .Where(c => c.CreatedAt >= DateTime.Today.AddDays(-30))
                .ToList();

            var net = recent.Sum(c => c.Variance);
            litNetVariance.Text = (net > 0 ? "+" : string.Empty) + Money(net);
            litShiftCount.Text = recent.Count.ToString("N0");

            litBalancedRate.Text = recent.Count == 0
                ? "—"
                : (recent.Count(c => c.Variance == 0m) * 100.0 / recent.Count).ToString("N0") + "%";
        }

        private void BindGrid()
        {
            var pendingOnly = ddlFilter.SelectedValue == "pending";
            var rows = CashUpService.GetCashUps(pendingOnly, 200);

            rptCashUps.DataSource = rows;
            rptCashUps.DataBind();
            phNone.Visible = rows.Count == 0;
        }

        protected void ddlFilter_Changed(object sender, EventArgs e)
        {
            alert.Hide();
            BindGrid();
        }

        protected void rptCashUps_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "review") return;

            int cashUpId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out cashUpId)) return;

            OpenReviewModal(cashUpId);
            BindGrid();
        }

        private void OpenReviewModal(int cashUpId)
        {
            var cashUp = CashUpService.GetCashUps(false, 500).FirstOrDefault(c => c.CashUpId == cashUpId);
            if (cashUp == null)
            {
                alert.ShowError("That cash-up no longer exists.");
                return;
            }

            ReviewModalVisible = true;
            hfCashUpId.Value = cashUpId.ToString();

            litReviewCashier.Text = Server.HtmlEncode(cashUp.User != null ? cashUp.User.FullName : "Cashier");
            litReviewMeta.Text = string.Format("{0} · {1}",
                cashUp.CreatedAt.ToString("dd MMM yyyy, HH:mm"),
                cashUp.Shift != null ? cashUp.Shift.TillNumber : "—");

            litReviewExpected.Text = Money(cashUp.ExpectedCash);
            litReviewCounted.Text = Money(cashUp.CountedCash);
            litReviewVariance.Text = (cashUp.Variance > 0 ? "+" : string.Empty) + Money(cashUp.Variance);

            if (!string.IsNullOrWhiteSpace(cashUp.Notes))
            {
                phCashierNote.Visible = true;
                litCashierNote.Text = Server.HtmlEncode(cashUp.Notes);
            }

            txtReviewNote.Text = string.Empty;
        }

        protected void btnSaveReview_Click(object sender, EventArgs e)
        {
            Page.Validate("Review");

            int cashUpId;
            if (!int.TryParse(hfCashUpId.Value, out cashUpId)) return;

            if (!Page.IsValid)
            {
                OpenReviewModal(cashUpId);
                BindGrid();
                return;
            }

            try
            {
                CashUpService.ReviewCashUp(cashUpId, txtReviewNote.Text, CurrentUser.UserId);
                ReviewModalVisible = false;
                alert.ShowSuccess("Variance signed off and recorded against your name.");
            }
            catch (BusinessRuleException ex)
            {
                alert.ShowError(ex.Message);
                OpenReviewModal(cashUpId);
            }

            BindHeadline();
            BindGrid();
        }

        protected void btnCancelReview_Click(object sender, EventArgs e)
        {
            ReviewModalVisible = false;
            alert.Hide();
            BindGrid();
        }

        // ---------------- Binding helpers ----------------

        protected string CashierName(object dataItem)
        {
            var c = (Models.CashUp)dataItem;
            return Server.HtmlEncode(c.User != null ? c.User.FullName : "—");
        }

        protected string TillNumber(object dataItem)
        {
            var c = (Models.CashUp)dataItem;
            return Server.HtmlEncode(c.Shift != null ? c.Shift.TillNumber : "—");
        }

        protected string VarianceText(object dataItem)
        {
            var c = (Models.CashUp)dataItem;
            return (c.Variance > 0 ? "+R " : c.Variance < 0 ? "−R " : "R ")
                   + Math.Abs(c.Variance).ToString("N2");
        }

        protected string VarianceStyle(object dataItem)
        {
            var c = (Models.CashUp)dataItem;
            return c.Variance == 0m
                ? "color:var(--color-primary);font-weight:600;"
                : "color:var(--color-accent-dark);font-weight:700;";
        }

        protected bool NeedsReview(object dataItem)
        {
            var c = (Models.CashUp)dataItem;
            return c.Variance != 0m && !c.ReviewedAt.HasValue;
        }

        protected string ReviewCell(object dataItem)
        {
            var c = (Models.CashUp)dataItem;

            if (c.Variance == 0m) return "<span class=\"pill pill-green\">Balanced</span>";
            if (!c.ReviewedAt.HasValue) return "<span class=\"pill pill-red\">Awaiting review</span>";

            var by = c.ReviewedByUser != null ? c.ReviewedByUser.FullName : "a manager";
            return "<span class=\"pill pill-grey\">Signed off</span><br /><span class=\"muted\" style=\"font-size:12px;\">"
                   + Server.HtmlEncode(by + " · " + c.ReviewedAt.Value.ToString("dd MMM")) + "</span>";
        }
    }
}
