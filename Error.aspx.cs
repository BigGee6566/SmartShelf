using System;
using System.Web.UI;
using SmartShelf.Services;

namespace SmartShelf
{
    /// <summary>
    /// The friendly end of every unhandled failure. It never shows exception detail - that goes
    /// to the trace log in Global.Application_Error instead.
    /// </summary>
    public partial class ErrorPage : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var isNotFound = Request.QueryString["code"] == "404";

            if (isNotFound)
            {
                litCode.Text = "404 &mdash; Not found";
                litHeading.Text = "That page isn&#39;t here.";
                litBody.Text = "The link may be out of date, or the page may have been renamed. "
                             + "Everything you have access to is on the menu inside the app.";
            }
            else
            {
                litCode.Text = "Something went wrong";
                litHeading.Text = "We couldn&#39;t finish that request.";
                litBody.Text = "The problem has been logged. Nothing you had already saved has been lost &mdash; "
                             + "please try again, and let your administrator know if it keeps happening.";
            }

            lnkHome.Visible = CurrentUser.IsSignedIn;
            if (lnkHome.Visible)
            {
                lnkHome.NavigateUrl = AuthService.HomePageFor(CurrentUser.RoleName);
            }
        }
    }
}
