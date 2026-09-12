using System;
using System.Data.Entity;
using System.Diagnostics;
using System.Security.Principal;
using System.Web;
using System.Web.Security;
using SmartShelf.Models;

namespace SmartShelf
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // The schema and seed data are owned by App_Data\SmartShelf_Schema.sql, so EF must
            // never try to create, drop or migrate the database behind our backs.
            Database.SetInitializer<SmartShelfContext>(null);
        }

        /// <summary>
        /// The Forms ticket's UserData is "userId|role|fullName|supplierId" (see AuthService).
        /// Lifting the role out of it and rebuilding the principal here is what makes
        /// User.IsInRole(...) and the &lt;location&gt; role rules in Web.config work without a full
        /// membership and role provider.
        /// </summary>
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            var identity = HttpContext.Current.User != null
                ? HttpContext.Current.User.Identity as FormsIdentity
                : null;

            if (identity == null || !identity.IsAuthenticated) return;

            var parts = (identity.Ticket.UserData ?? string.Empty).Split('|');
            var role = parts.Length > 1 ? parts[1] : string.Empty;

            var roles = string.IsNullOrWhiteSpace(role) ? new string[0] : new[] { role };
            HttpContext.Current.User = new GenericPrincipal(identity, roles);
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var ex = Server.GetLastError();
            if (ex == null) return;

            // Trace rather than swallow, so the cause is still recoverable from the event log
            // while the visitor only ever sees the friendly Error page.
            Trace.TraceError("Unhandled exception on {0}: {1}", Request == null ? "?" : Request.RawUrl, ex);

            var http = ex as HttpException;
            if (http != null && http.GetHttpCode() == 404)
            {
                Server.ClearError();
                Response.Redirect("~/Error.aspx?code=404", false);
                return;
            }

            // customErrors handles the redirect in Release; nothing further to do here.
        }

        protected void Session_Start(object sender, EventArgs e) { }

        protected void Application_End(object sender, EventArgs e) { }
    }
}
