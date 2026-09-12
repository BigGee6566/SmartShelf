using System;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Admin
{
    /// <summary>
    /// A read-only map of which role reaches which screen. The checkmarks come straight from each
    /// page's AllowedRoles override via <see cref="RoleAccessAudit.BuildMatrix"/> - see that class
    /// for why this can never drift out of sync with what the application actually enforces.
    /// </summary>
    public partial class RolePermissions : SecurePageBase
    {
        protected override string[] AllowedRoles { get { return new[] { RoleNames.Admin }; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "Role permissions";
            master.PageSubtitle = "Who can reach what";

            if (IsPostBack) return;

            rptMatrix.DataSource = RoleAccessAudit.BuildMatrix();
            rptMatrix.DataBind();
        }

        protected string Tick(bool allowed)
        {
            return allowed
                ? "<span style=\"color:var(--color-primary);font-weight:700;\" title=\"Allowed\">&#10003;</span>"
                : "<span class=\"muted\" title=\"Not allowed\">&mdash;</span>";
        }
    }
}
