using System;
using System.Collections.Generic;
using System.Linq;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Admin
{
    /// <summary>
    /// The verification audit, published where the system runs. Read-only: it reports on the
    /// system rather than changing it, so it exposes no actions at all.
    /// </summary>
    public partial class SystemAudit : SecurePageBase
    {
        protected override string[] AllowedRoles
        {
            get { return new[] { RoleNames.Admin }; }
        }

        /// <summary>A single live row count, for the facts panel.</summary>
        public class Fact
        {
            public string Label { get; set; }
            public int Value { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "System audit";
            master.PageSubtitle = "Verification against the System Matrix, Overview and use case diagram";

            if (IsPostBack) return;

            var findings = AuditReport.Findings();
            var coverage = AuditReport.Coverage();

            litReviewedOn.Text = "Reviewed " + AuditReport.ReviewedOn.ToString("dd MMMM yyyy");

            var critical = findings.Count(f => f.Severity == AuditFinding.Critical);
            var major = findings.Count(f => f.Severity == AuditFinding.Major);
            var majorFixed = findings.Count(f => f.Severity == AuditFinding.Major && f.Fixed);

            litCritical.Text = critical.ToString();
            litMajor.Text = major.ToString();
            litMinor.Text = findings.Count(f => f.Severity == AuditFinding.Minor).ToString();
            litMajorFixed.Text = majorFixed + " of " + major + " fixed in this pass";
            litClean.Text = coverage.Count(c => c.IsClean).ToString();

            litVerdict.Text = Verdict(critical, major, majorFixed, coverage);

            rptCoverage.DataSource = coverage;
            rptCoverage.DataBind();

            rptFindings.DataSource = OrderBySeverity(findings);
            rptFindings.DataBind();

            rptVerified.DataSource = AuditReport.Verified();
            rptVerified.DataBind();

            BindFacts();
        }

        /// <summary>Most severe first, and within a severity the unfixed items lead.</summary>
        private static List<AuditFinding> OrderBySeverity(List<AuditFinding> findings)
        {
            var rank = new Dictionary<string, int>
            {
                { AuditFinding.Critical, 0 }, { AuditFinding.Major, 1 },
                { AuditFinding.Minor, 2 }, { AuditFinding.Polish, 3 }
            };

            return findings
                .OrderBy(f => rank.ContainsKey(f.Severity) ? rank[f.Severity] : 9)
                .ThenBy(f => f.Fixed)
                .ToList();
        }

        private void BindFacts()
        {
            var facts = AuditReport.GetFacts();

            if (!facts.Reachable)
            {
                phFacts.Visible = false;
                phFactsError.Visible = true;
                return;
            }

            rptFacts.DataSource = new List<Fact>
            {
                new Fact { Label = "Products", Value = facts.Products },
                new Fact { Label = "Categories", Value = facts.Categories },
                new Fact { Label = "Suppliers", Value = facts.Suppliers },
                new Fact { Label = "Users", Value = facts.Users },
                new Fact { Label = "Roles", Value = facts.Roles },
                new Fact { Label = "Sales", Value = facts.Sales },
                new Fact { Label = "Sale lines", Value = facts.SaleLines },
                new Fact { Label = "Shifts", Value = facts.Shifts },
                new Fact { Label = "Cash-ups", Value = facts.CashUps },
                new Fact { Label = "Stock movements", Value = facts.StockMovements },
                new Fact { Label = "Stock batches", Value = facts.StockBatches },
                new Fact { Label = "Purchase orders", Value = facts.PurchaseOrders },
                new Fact { Label = "Order lines", Value = facts.PurchaseOrderLines }
            };
            rptFacts.DataBind();
        }

        private static string Verdict(int critical, int major, int majorFixed, List<CoverageRow> coverage)
        {
            var built = coverage.Count(c => c.Implemented == CoverageRow.Yes);

            return string.Format(
                "{0} of the Matrix's {1} use cases are built and usable end to end, and the four key reports all run " +
                "live off data the system generated - the top-selling products figures were traced back to the sale " +
                "lines and matched to the cent. Role separation was tested in all sixteen role-to-area combinations and " +
                "held every time. {2} of the {3} major defects found were fixed during this pass: the cash-up crash on a " +
                "repeated submit, the supplier dispatch that changed nothing the store could see, and two places where bad " +
                "input was accepted in silence. What remains is {4} critical gaps that are absences rather than bugs - " +
                "self-service registration and password recovery were never built - plus a set of naming differences " +
                "between the Matrix and the schema. The system is sound enough to demo; the documentation is what needs " +
                "another pass before it is handed in.",
                built, coverage.Count, majorFixed, major, critical);
        }

        // ---------------- Binding helpers ----------------

        protected string GetCss(object verdict)
        {
            return CoverageRow.CssFor(Convert.ToString(verdict));
        }

        protected string GetSeverityCss(object severity)
        {
            return AuditFinding.CssFor(Convert.ToString(severity));
        }
    }
}
