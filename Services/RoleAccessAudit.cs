using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SmartShelf.Models;

namespace SmartShelf.Services
{
    /// <summary>One row of the role/screen matrix shown on Admin/RolePermissions.aspx.</summary>
    public class ScreenAccessRow
    {
        public string Screen { get; set; }
        public string Purpose { get; set; }
        public bool Cashier { get; set; }
        public bool Manager { get; set; }
        public bool Supplier { get; set; }
        public bool Admin { get; set; }
    }

    /// <summary>
    /// Builds the role/screen matrix from each page's own <see cref="SecurePageBase.AllowedRoles"/>
    /// override via reflection, so the checkmarks can never drift out of sync with what the
    /// application actually enforces - the exact risk RolePermissions.aspx used to carry as a
    /// hand-typed table. The Web.config &lt;location&gt; rules are a second, coarser gate (per
    /// folder rather than per page); they are not read here because they only ever need to be as
    /// permissive as the roles listed below, and rechecking a handful of folder rules by eye is a
    /// far smaller manual burden than the 15-row matrix this replaces.
    ///
    /// Only the human-readable screen name and purpose text below are still hand-maintained. A page
    /// missing from <see cref="Descriptions"/> still appears in the matrix - with its raw class name
    /// and a note to add a description - rather than silently vanishing from the audit.
    /// </summary>
    public static class RoleAccessAudit
    {
        private class Desc
        {
            public string Screen;
            public string Purpose;
        }

        private static readonly Dictionary<string, Desc> Descriptions = new Dictionary<string, Desc>
        {
            ["SmartShelf.Cashier.POS"] = new Desc { Screen = "POS Terminal", Purpose = "Ring up a sale, take payment, print a receipt" },
            ["SmartShelf.Cashier.MySales"] = new Desc { Screen = "My Sales", Purpose = "A cashier's own trading history for the shift" },
            ["SmartShelf.Cashier.CashUp"] = new Desc { Screen = "Cash-Up", Purpose = "Count the drawer and close the shift" },
            ["SmartShelf.Cashier.Receipt"] = new Desc { Screen = "Receipt", Purpose = "View or print a completed sale" },
            ["SmartShelf.Manager.Dashboard"] = new Desc { Screen = "Dashboard", Purpose = "Store position at a glance" },
            ["SmartShelf.Manager.Products"] = new Desc { Screen = "Products", Purpose = "Catalogue, pricing and categories" },
            ["SmartShelf.Manager.ProductEdit"] = new Desc { Screen = "Product Edit", Purpose = "Create or edit a single product" },
            ["SmartShelf.Manager.Categories"] = new Desc { Screen = "Categories", Purpose = "How the catalogue is grouped" },
            ["SmartShelf.Manager.Inventory"] = new Desc { Screen = "Inventory", Purpose = "Stock levels, corrections and waste write-offs" },
            ["SmartShelf.Manager.StockMovements"] = new Desc { Screen = "Stock Movements", Purpose = "The full audit trail behind every stock figure" },
            ["SmartShelf.Manager.Suppliers"] = new Desc { Screen = "Suppliers", Purpose = "Supplier records and lead times" },
            ["SmartShelf.Manager.PurchaseOrders"] = new Desc { Screen = "Purchase Orders", Purpose = "Raise, send and receive against orders" },
            ["SmartShelf.Manager.PurchaseOrderEdit"] = new Desc { Screen = "Purchase Order Edit", Purpose = "Create or edit a single purchase order" },
            ["SmartShelf.Manager.CashUpReview"] = new Desc { Screen = "Cash-Up Review", Purpose = "Reconcile and sign off shift variances" },
            ["SmartShelf.Manager.Reports"] = new Desc { Screen = "Reports", Purpose = "The four key reports, plus CSV export" },
            ["SmartShelf.Supplier.Orders"] = new Desc { Screen = "My Orders", Purpose = "A supplier's own orders: confirm and dispatch" },
            ["SmartShelf.Admin.Users"] = new Desc { Screen = "User Accounts", Purpose = "Create accounts, assign roles, reset passwords" },
            ["SmartShelf.Admin.RolePermissions"] = new Desc { Screen = "Role Permissions", Purpose = "This page" },
            ["SmartShelf.Admin.ActivityLog"] = new Desc { Screen = "System Activity", Purpose = "Cross-table activity timeline" },
            ["SmartShelf.Admin.SystemAudit"] = new Desc { Screen = "System Audit", Purpose = "Self-verification report against the System Matrix" },
        };

        public static List<ScreenAccessRow> BuildMatrix()
        {
            var allowedRolesProperty = typeof(SecurePageBase).GetProperty(
                "AllowedRoles", BindingFlags.NonPublic | BindingFlags.Instance);

            var pageTypes = typeof(SecurePageBase).Assembly.GetTypes()
                .Where(t => typeof(SecurePageBase).IsAssignableFrom(t) && !t.IsAbstract && t != typeof(SecurePageBase))
                .OrderBy(t => t.Namespace, StringComparer.Ordinal)
                .ThenBy(t => t.Name, StringComparer.Ordinal);

            var rows = new List<ScreenAccessRow>();

            foreach (var type in pageTypes)
            {
                object instance;
                try { instance = Activator.CreateInstance(type); }
                catch { continue; }

                var roles = (allowedRolesProperty.GetValue(instance) as string[]) ?? new string[0];

                Desc desc;
                if (!Descriptions.TryGetValue(type.FullName, out desc))
                {
                    desc = new Desc { Screen = type.Name, Purpose = "(no description yet - add one to RoleAccessAudit.Descriptions)" };
                }

                rows.Add(new ScreenAccessRow
                {
                    Screen = desc.Screen,
                    Purpose = desc.Purpose,
                    Cashier = roles.Contains(RoleNames.Cashier),
                    Manager = roles.Contains(RoleNames.Manager),
                    Supplier = roles.Contains(RoleNames.Supplier),
                    Admin = roles.Contains(RoleNames.Admin)
                });
            }

            return rows;
        }
    }
}
