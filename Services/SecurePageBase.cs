using System;
using System.Globalization;
using System.Web.UI;

namespace SmartShelf.Services
{
    /// <summary>
    /// Base for every page behind the login. The &lt;location&gt; rules in Web.config already gate
    /// these folders; this second, in-code check means a mis-edited config can never quietly
    /// expose a screen to the wrong role.
    /// </summary>
    public abstract class SecurePageBase : Page
    {
        /// <summary>Roles allowed on this page. An empty array means "any signed-in user".</summary>
        protected abstract string[] AllowedRoles { get; }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if (!CurrentUser.IsSignedIn)
            {
                Response.Redirect("~/Login.aspx?returnUrl=" + Server.UrlEncode(Request.RawUrl), true);
                return;
            }

            var roles = AllowedRoles;
            if (roles != null && roles.Length > 0 && !CurrentUser.IsInAnyRole(roles))
            {
                Response.Redirect("~/AccessDenied.aspx", true);
            }
        }

        /// <summary>Formats money the way every screen in the system shows it.</summary>
        protected static string Money(decimal value)
        {
            return "R " + value.ToString("N2");
        }

        /// <summary>
        /// Parses an amount typed into an &lt;input type="number"&gt;. Browsers always post those
        /// invariant ("1234.50"), never in the page's culture, so parsing them with the current
        /// culture would read "100.00" as ten thousand under a comma-decimal locale. The current
        /// culture is tried as a fallback for anything typed into a plain text box.
        /// </summary>
        protected static bool TryParseAmount(string input, out decimal value)
        {
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
                return true;

            return decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out value);
        }
    }
}
