using System;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Admin
{
    /// <summary>
    /// A read-only map of which role reaches which screen. It mirrors the &lt;location&gt; rules in
    /// Web.config and the AllowedRoles on each page - if those change, update this list with them.
    /// </summary>
    public partial class RolePermissions : SecurePageBase
    {
        protected override string[] AllowedRoles { get { return new[] { RoleNames.Admin }; } }

        private class Row
        {
            public string Screen { get; set; }
            public string Purpose { get; set; }
            public bool Cashier { get; set; }
            public bool Manager { get; set; }
            public bool Supplier { get; set; }
            public bool Admin { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "Role permissions";
            master.PageSubtitle = "Who can reach what";

            if (IsPostBack) return;

            rptMatrix.DataSource = new[]
            {
                new Row { Screen = "POS Terminal",     Purpose = "Ring up a sale, take payment, print a receipt",     Cashier = true,  Manager = true },
                new Row { Screen = "My Sales",         Purpose = "A cashier's own trading history for the shift",     Cashier = true,  Manager = true },
                new Row { Screen = "Cash-Up",          Purpose = "Count the drawer and close the shift",              Cashier = true,  Manager = true },
                new Row { Screen = "Dashboard",        Purpose = "Store position at a glance",                        Manager = true },
                new Row { Screen = "Products",         Purpose = "Catalogue, pricing and categories",                 Manager = true },
                new Row { Screen = "Categories",       Purpose = "How the catalogue is grouped",                      Manager = true },
                new Row { Screen = "Inventory",        Purpose = "Stock levels, corrections and waste write-offs",    Manager = true },
                new Row { Screen = "Stock Movements",  Purpose = "The full audit trail behind every stock figure",    Manager = true },
                new Row { Screen = "Suppliers",        Purpose = "Supplier records and lead times",                   Manager = true },
                new Row { Screen = "Purchase Orders",  Purpose = "Raise, send and receive against orders",            Manager = true },
                new Row { Screen = "Cash-Up Review",   Purpose = "Reconcile and sign off shift variances",            Manager = true },
                new Row { Screen = "Reports",          Purpose = "The four key reports, plus CSV export",             Manager = true },
                new Row { Screen = "My Orders",        Purpose = "A supplier's own orders: confirm and dispatch",     Supplier = true },
                new Row { Screen = "User Accounts",    Purpose = "Create accounts, assign roles, reset passwords",    Admin = true },
                new Row { Screen = "Role Permissions", Purpose = "This page",                                          Admin = true }
            };
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
