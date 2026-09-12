using System;
using System.Web.UI;
using SmartShelf.Services;

namespace SmartShelf
{
    /// <summary>Public shell: marketing nav plus footer. Anonymous visitors are expected here.</summary>
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Someone already signed in shouldn't be invited to "try the demo" - send them to work.
            if (!CurrentUser.IsSignedIn) return;

            phSignedOut.Visible = false;
            phSignedIn.Visible = true;
            lnkWorkspace.NavigateUrl = ResolveUrl(AuthService.HomePageFor(CurrentUser.RoleName));
            lnkWorkspace.Text = "Go to my workspace &rarr;";
        }
    }
}
