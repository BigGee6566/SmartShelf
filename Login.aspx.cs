using System;
using System.Web.UI;
using SmartShelf.Services;

namespace SmartShelf
{
    public partial class Login : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            if (!CurrentUser.IsSignedIn) return;

            // Already signed in. Arriving here with a returnUrl means Forms auth bounced them off
            // a page their role can't reach - say so plainly instead of silently looping them
            // back to their own home page.
            if (!string.IsNullOrEmpty(Request.QueryString["returnUrl"]))
            {
                Response.Redirect("~/AccessDenied.aspx", true);
                return;
            }

            Response.Redirect(AuthService.HomePageFor(CurrentUser.RoleName), true);
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            // Client-side validators are a convenience; this is the check that counts.
            Page.Validate("Login");
            if (!Page.IsValid) return;

            try
            {
                var user = AuthService.SignIn(txtUsername.Text, txtPassword.Text, chkRemember.Checked);

                if (user == null)
                {
                    // Deliberately vague: a wrong username, a wrong password and a disabled
                    // account all look identical from outside.
                    ShowError("That username and password combination wasn't recognised.");
                    return;
                }

                Response.Redirect(SafeReturnUrl() ?? AuthService.HomePageFor(user.RoleName), true);
            }
            catch (System.Threading.ThreadAbortException)
            {
                throw; // Response.Redirect - let it through.
            }
            catch (Exception)
            {
                ShowError("Sign-in is temporarily unavailable. Please try again in a moment.");
            }
        }

        /// <summary>
        /// Only accepts a same-site, app-relative return URL, so a crafted link cannot bounce a
        /// freshly signed-in user off to another host.
        /// </summary>
        private string SafeReturnUrl()
        {
            var raw = Request.QueryString["returnUrl"];
            if (string.IsNullOrWhiteSpace(raw)) return null;
            if (!raw.StartsWith("/") || raw.StartsWith("//")) return null;
            return raw;
        }

        private void ShowError(string message)
        {
            litError.Text = Server.HtmlEncode(message);
            phError.Visible = true;
            txtPassword.Text = string.Empty;
        }
    }
}
