using System;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Cashier
{
    /// <summary>
    /// End-of-shift reconciliation (Focus E). The expected total is displayed but never posted:
    /// the only value that leaves the browser is the counted cash, and CashUpService recomputes
    /// the expectation from the sales themselves before working out the variance.
    /// </summary>
    public partial class CashUp : SecurePageBase
    {
        protected override string[] AllowedRoles
        {
            get { return new[] { RoleNames.Cashier, RoleNames.Manager }; }
        }

        protected string VarianceColour { get; private set; }

        private ShiftSummary _shift;

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "Cash-Up";
            master.PageSubtitle = AppMaster.TodayLabel() + " · " + CurrentUser.FullName;

            VarianceColour = "var(--color-muted)";

            // Loaded on every request, including the submitting postback - btnSubmit_Click needs
            // the shift id, and it runs after this.
            _shift = CashUpService.GetOpenShiftSummary(CurrentUser.UserId);

            if (_shift == null)
            {
                phForm.Visible = false;
                phNoShift.Visible = true;
                return;
            }

            BindShift();
            UpdateVariancePreview();
        }

        private void BindShift()
        {
            litTill.Text = Server.HtmlEncode(_shift.TillNumber);
            litOpened.Text = _shift.OpenedAt.ToString("dd MMM yyyy, HH:mm");
            litSaleCount.Text = _shift.SaleCount.ToString("N0");
            litFloat.Text = Money(_shift.OpeningFloat);
            litCash.Text = Money(_shift.CashSales);
            litCard.Text = Money(_shift.CardSales);
            litMobile.Text = Money(_shift.MobileSales);
            litExpected.Text = Money(_shift.ExpectedCash);
        }

        protected void txtCounted_TextChanged(object sender, EventArgs e)
        {
            UpdateVariancePreview();
        }

        /// <summary>
        /// Live preview only. The variance that gets stored is calculated again inside
        /// CashUpService, so this figure has no authority.
        /// </summary>
        private void UpdateVariancePreview()
        {
            // There is nothing to compare against once the shift is closed. This runs from the
            // TextChanged event too, which still fires on a repeated submit after the drawer has
            // already been cashed up - guarding here covers every caller rather than one of them.
            if (_shift == null) return;

            decimal counted;
            if (!TryParseAmount(txtCounted.Text, out counted))
            {
                litVariance.Text = "—";
                litVarianceHint.Text = "Enter your count to see the difference.";
                return;
            }

            var variance = decimal.Round(counted - _shift.ExpectedCash, 2);
            litVariance.Text = (variance > 0 ? "+" : string.Empty) + Money(variance);

            if (variance == 0m)
            {
                VarianceColour = "var(--color-primary)";
                litVarianceHint.Text = "The drawer balances exactly.";
            }
            else
            {
                VarianceColour = "var(--color-accent-dark)";
                litVarianceHint.Text = variance < 0
                    ? "The drawer is short. Recount, then add a note explaining what you found."
                    : "The drawer is over. Recount, then add a note explaining what you found.";
            }
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            Page.Validate("CashUp");
            if (!Page.IsValid) return;

            // The shift is already closed if this is a second submit - a double-click, a browser
            // resend, or the back button. Say so instead of dereferencing a null shift.
            if (_shift == null)
            {
                phForm.Visible = false;
                phNoShift.Visible = true;
                alert.ShowInfo("This shift has already been cashed up. Nothing further is needed.");
                return;
            }

            decimal counted;
            if (!TryParseAmount(txtCounted.Text, out counted))
            {
                alert.ShowError("Enter the counted cash as a number, for example 4362.50");
                return;
            }

            try
            {
                var result = CashUpService.CompleteCashUp(_shift.ShiftId, counted, txtNotes.Text, CurrentUser.UserId);
                ShowConfirmation(result);
            }
            catch (BusinessRuleException ex)
            {
                alert.ShowError(ex.Message);
            }
        }

        private void ShowConfirmation(Models.CashUp cashUp)
        {
            phForm.Visible = false;
            phNoShift.Visible = false;
            phDone.Visible = true;

            litDoneExpected.Text = Money(cashUp.ExpectedCash);
            litDoneCounted.Text = Money(cashUp.CountedCash);
            litDoneVariance.Text = (cashUp.Variance > 0 ? "+" : string.Empty) + Money(cashUp.Variance);

            litDoneMessage.Text = cashUp.Variance == 0m
                ? "Your drawer balanced and the shift is closed. Nothing further is needed."
                : "This variance has been recorded against your name and sent to the manager for review.";
        }
    }
}
