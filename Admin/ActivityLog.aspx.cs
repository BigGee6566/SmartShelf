using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Admin
{
    /// <summary>
    /// The admin's view of everything the system has recorded. Read-only by design: this screen
    /// is the place you come to find out what happened, so it must never be able to change it.
    /// </summary>
    public partial class ActivityLog : SecurePageBase
    {
        protected override string[] AllowedRoles
        {
            get { return new[] { RoleNames.Admin }; }
        }

        /// <summary>Per-source row cap in ActivityService; used to tell the user when a view is truncated.</summary>
        private const int PerSourceCap = 400;

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "System activity";
            master.PageSubtitle = "Everything the system has recorded, newest first";

            if (IsPostBack) return;

            BindFilters();
            txtFrom.Text = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
            txtTo.Text = DateTime.Today.ToString("yyyy-MM-dd");
            BindTotals();
            BindGrid();
        }

        private void BindFilters()
        {
            ddlUser.Items.Clear();
            ddlUser.Items.Add(new ListItem("Everyone", string.Empty));
            foreach (var u in UserService.GetUsers())
            {
                ddlUser.Items.Add(new ListItem(u.FullName + " (" + u.Role.Name + ")", u.UserId.ToString()));
            }

            ddlCategory.Items.Clear();
            ddlCategory.Items.Add(new ListItem("All activity", string.Empty));
            foreach (var c in ActivityService.Categories)
            {
                ddlCategory.Items.Add(new ListItem(c, c));
            }
        }

        private void BindTotals()
        {
            try
            {
                var totals = ActivityService.GetTotals();
                litSales.Text = totals.Sales.ToString("N0");
                litMovements.Text = totals.StockMovements.ToString("N0");
                litCashUps.Text = totals.CashUps.ToString("N0");
                litVariances.Text = totals.UnreviewedVariances == 0
                    ? "All variances reviewed"
                    : totals.UnreviewedVariances.ToString("N0") + " variance(s) awaiting review";
            }
            catch (Exception)
            {
                litSales.Text = litMovements.Text = litCashUps.Text = "—";
                litVariances.Text = string.Empty;
            }
        }

        private void BindGrid()
        {
            DateTime? from, to;
            if (!TryReadDate(txtFrom.Text, "\"from\"", out from)) return;
            if (!TryReadDate(txtTo.Text, "\"to\"", out to)) return;

            if (from.HasValue && to.HasValue && from > to)
            {
                alert.ShowError("The \"from\" date is after the \"to\" date. Swap them and try again.");
                return;
            }

            int parsedUser;
            var userId = int.TryParse(ddlUser.SelectedValue, out parsedUser) ? (int?)parsedUser : null;
            var category = ddlCategory.SelectedValue;

            List<ActivityEntry> entries;
            try
            {
                entries = ActivityService.GetActivity(from, to, userId, category, PerSourceCap);
            }
            catch (Exception)
            {
                alert.ShowError("The activity log couldn't be loaded just now. Please try again.");
                return;
            }

            rptActivity.DataSource = entries;
            rptActivity.DataBind();

            litCount.Text = entries.Count.ToString("N0");
            phNone.Visible = entries.Count == 0;

            // Say so when a source hit its cap, rather than quietly showing a partial timeline.
            var capped = entries
                .GroupBy(x => x.Category)
                .Any(g => g.Count() >= PerSourceCap);

            phCapped.Visible = capped;
            if (capped)
            {
                litCapped.Text = "This view is capped at " + PerSourceCap.ToString("N0") +
                                 " events per activity type. Narrow the date range to see everything in it.";
            }
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            alert.Hide();
            BindGrid();
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            alert.Hide();
            txtFrom.Text = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
            txtTo.Text = DateTime.Today.ToString("yyyy-MM-dd");
            ddlUser.SelectedIndex = 0;
            ddlCategory.SelectedIndex = 0;
            BindGrid();
        }

        protected void btnExport_Click(object sender, EventArgs e)
        {
            DateTime? from, to;
            if (!TryReadDate(txtFrom.Text, "\"from\"", out from)) return;
            if (!TryReadDate(txtTo.Text, "\"to\"", out to)) return;

            int parsedUser;
            var userId = int.TryParse(ddlUser.SelectedValue, out parsedUser) ? (int?)parsedUser : null;

            try
            {
                var entries = ActivityService.GetActivity(from, to, userId, ddlCategory.SelectedValue, PerSourceCap);

                var csv = CsvWriter.Build(
                    new[] { "When", "Who", "Role", "Type", "Action", "Detail", "Reference" },
                    entries.Select(x => new object[]
                    {
                        x.When.ToString("yyyy-MM-dd HH:mm:ss"),
                        x.Who, x.Role, x.Category, x.Action, x.Detail, x.Reference
                    }));

                CsvWriter.Send(Response, "smartshelf-activity-" + DateTime.Today.ToString("yyyy-MM-dd") + ".csv", csv);
            }
            catch (System.Threading.ThreadAbortException)
            {
                throw; // Response.End inside CsvWriter.Send - let it through.
            }
            catch (Exception)
            {
                alert.ShowError("The export couldn't be produced just now. Please try again.");
            }
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
    }
}
