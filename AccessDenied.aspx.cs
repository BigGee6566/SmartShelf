using System;
using System.Web.UI;
using SmartShelf.Services;

namespace SmartShelf
{
    public partial class AccessDenied : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!CurrentUser.IsSignedIn)
            {
                Response.Redirect("~/Login.aspx", true);
                return;
            }

            litWho.Text = Server.HtmlEncode(CurrentUser.FullName + " (" + CurrentUser.RoleName + ")");
            lnkHome.NavigateUrl = AuthService.HomePageFor(CurrentUser.RoleName);
        }

        protected void btnSignOut_Click(object sender, EventArgs e)
        {
            AuthService.SignOut();
            Response.Redirect("~/Login.aspx", true);
        }
    }
}
