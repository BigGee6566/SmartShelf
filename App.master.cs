using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf
{
    /// <summary>One nav entry. A null Url renders a section heading instead of a link.</summary>
    public class NavItem
    {
        public string Text { get; set; }
        public string Url { get; set; }
        public string GroupLabel { get; set; }

        public static NavItem Link(string text, string url) { return new NavItem { Text = text, Url = url }; }
        public static NavItem Group(string label) { return new NavItem { GroupLabel = label }; }
    }

    public partial class AppMaster : MasterPage
    {
        /// <summary>Set by each page in Page_Load; drives the top bar.</summary>
        public string PageTitle { set { litTitle.Text = Server.HtmlEncode(value ?? string.Empty); } }
        public string PageSubtitle { set { litSubtitle.Text = Server.HtmlEncode(value ?? string.Empty); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            litUser.Text = Server.HtmlEncode(CurrentUser.FullName) + " &middot; " +
                           Server.HtmlEncode(FriendlyRole(CurrentUser.RoleName));

            rptNav.DataSource = BuildNav(CurrentUser.RoleName);
            rptNav.DataBind();
        }

        private static string FriendlyRole(string role)
        {
            return role == RoleNames.Manager ? "Store Manager" : role;
        }

        private static List<NavItem> BuildNav(string role)
        {
            var items = new List<NavItem>();

            switch (role)
            {
                case RoleNames.Cashier:
                    items.Add(NavItem.Link("POS Terminal", "~/Cashier/POS.aspx"));
                    items.Add(NavItem.Link("My Sales", "~/Cashier/MySales.aspx"));
                    items.Add(NavItem.Link("Cash-Up", "~/Cashier/CashUp.aspx"));
                    break;

                case RoleNames.Manager:
                    items.Add(NavItem.Link("Dashboard", "~/Manager/Dashboard.aspx"));
                    items.Add(NavItem.Group("Catalogue"));
                    items.Add(NavItem.Link("Products", "~/Manager/Products.aspx"));
                    items.Add(NavItem.Link("Categories", "~/Manager/Categories.aspx"));
                    items.Add(NavItem.Group("Stock"));
                    items.Add(NavItem.Link("Inventory", "~/Manager/Inventory.aspx"));
                    items.Add(NavItem.Link("Stock Movements", "~/Manager/StockMovements.aspx"));
                    items.Add(NavItem.Group("Supply"));
                    items.Add(NavItem.Link("Suppliers", "~/Manager/Suppliers.aspx"));
                    items.Add(NavItem.Link("Purchase Orders", "~/Manager/PurchaseOrders.aspx"));
                    items.Add(NavItem.Group("Money"));
                    items.Add(NavItem.Link("Cash-Up Review", "~/Manager/CashUpReview.aspx"));
                    items.Add(NavItem.Link("Reports", "~/Manager/Reports.aspx"));
                    items.Add(NavItem.Group("Till"));
                    items.Add(NavItem.Link("POS Terminal", "~/Cashier/POS.aspx"));
                    break;

                case RoleNames.Supplier:
                    items.Add(NavItem.Link("My Orders", "~/Supplier/Orders.aspx"));
                    break;

                case RoleNames.Admin:
                    items.Add(NavItem.Link("User Accounts", "~/Admin/Users.aspx"));
                    items.Add(NavItem.Link("Role Permissions", "~/Admin/RolePermissions.aspx"));
                    items.Add(NavItem.Group("Oversight"));
                    items.Add(NavItem.Link("System Activity", "~/Admin/ActivityLog.aspx"));
                    items.Add(NavItem.Link("System Audit", "~/Admin/SystemAudit.aspx"));
                    break;
            }

            return items;
        }

        protected void rptNav_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem) return;

            var item = (NavItem)e.Item.DataItem;
            var group = (Literal)e.Item.FindControl("litGroup");
            var link = (HyperLink)e.Item.FindControl("lnkNav");

            if (item.Url == null)
            {
                group.Text = "<div class=\"nav-group-label\">" + Server.HtmlEncode(item.GroupLabel) + "</div>";
                link.Visible = false;
                return;
            }

            link.NavigateUrl = item.Url;
            link.Text = Server.HtmlEncode(item.Text);

            // Highlight whichever entry matches the page currently being served.
            var current = ResolveUrl(item.Url);
            if (string.Equals(current, Request.AppRelativeCurrentExecutionFilePath == null
                    ? null : ResolveUrl(Request.AppRelativeCurrentExecutionFilePath),
                    StringComparison.OrdinalIgnoreCase))
            {
                link.CssClass = "active";
            }
        }

        protected void btnSignOut_Click(object sender, EventArgs e)
        {
            AuthService.SignOut();
            Response.Redirect("~/Login.aspx", true);
        }

        /// <summary>Convenience for pages that want today's date in the top bar.</summary>
        public static string TodayLabel()
        {
            // Uses the culture pinned in Web.config, so every screen reads dates the same way.
            return DateTime.Now.ToString("dddd, dd MMMM yyyy", CultureInfo.CurrentCulture);
        }
    }
}
