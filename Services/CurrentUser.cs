using System;
using System.Security.Principal;
using System.Web;
using System.Web.Security;

namespace SmartShelf.Services
{
    /// <summary>
    /// Reads the signed-in user's details straight off the encrypted Forms ticket that
    /// AuthService issued. No database call, and nothing here can be altered by the browser.
    /// </summary>
    public static class CurrentUser
    {
        public static bool IsSignedIn
        {
            get
            {
                var u = HttpContext.Current == null ? null : HttpContext.Current.User;
                return u != null && u.Identity != null && u.Identity.IsAuthenticated;
            }
        }

        public static int UserId { get { return GetPart(0, 0); } }

        public static string Username
        {
            get
            {
                return IsSignedIn ? HttpContext.Current.User.Identity.Name : string.Empty;
            }
        }

        public static string RoleName { get { return GetString(1); } }

        public static string FullName
        {
            get
            {
                var n = GetString(2);
                return string.IsNullOrEmpty(n) ? Username : n;
            }
        }

        /// <summary>Non-null only for supplier logins; scopes the supplier portal to one company.</summary>
        public static int? SupplierId
        {
            get
            {
                var raw = GetString(3);
                int id;
                return int.TryParse(raw, out id) ? (int?)id : null;
            }
        }

        public static bool IsInRole(string role)
        {
            return IsSignedIn && HttpContext.Current.User.IsInRole(role);
        }

        public static bool IsInAnyRole(params string[] roles)
        {
            if (!IsSignedIn) return false;
            foreach (var r in roles)
            {
                if (HttpContext.Current.User.IsInRole(r)) return true;
            }
            return false;
        }

        private static string[] TicketParts()
        {
            var identity = IsSignedIn ? HttpContext.Current.User.Identity as FormsIdentity : null;
            if (identity == null) return null;
            var data = identity.Ticket.UserData ?? string.Empty;
            return data.Split('|');
        }

        private static string GetString(int index)
        {
            var parts = TicketParts();
            return parts != null && parts.Length > index ? parts[index] : string.Empty;
        }

        private static int GetPart(int index, int fallback)
        {
            int value;
            return int.TryParse(GetString(index), out value) ? value : fallback;
        }
    }
}
