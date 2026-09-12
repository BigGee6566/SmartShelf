using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Security;
using SmartShelf.Models;

namespace SmartShelf.Services
{
    public class AuthenticatedUser
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string RoleName { get; set; }
        public int? SupplierId { get; set; }
    }

    public static class AuthService
    {
        /// <summary>
        /// Checks credentials and, on success, issues the Forms ticket. Failures all return the
        /// same null result: the caller must not tell the visitor whether it was the username,
        /// the password or a disabled account that was wrong.
        /// </summary>
        public static AuthenticatedUser SignIn(string username, string password, bool persistent)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password)) return null;

            username = username.Trim();

            using (var db = new SmartShelfContext())
            {
                var user = db.Users
                    .Include(u => u.Role)
                    .FirstOrDefault(u => u.Username == username);

                if (user == null || !user.IsActive) return null;
                if (!PasswordHasher.Verify(password, user.PasswordSalt, user.PasswordHash)) return null;

                user.LastLoginAt = DateTime.Now;
                db.SaveChanges();

                var result = new AuthenticatedUser
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FullName = user.FullName,
                    RoleName = user.Role.Name,
                    SupplierId = user.SupplierId
                };

                IssueTicket(result, persistent);
                return result;
            }
        }

        /// <summary>
        /// The ticket's UserData carries "userId|role|fullName|supplierId" so that every request
        /// can rebuild the principal (Global.Application_PostAuthenticateRequest) and answer
        /// "who is this and what may they see" without another database round trip.
        /// </summary>
        private static void IssueTicket(AuthenticatedUser user, bool persistent)
        {
            var userData = string.Join("|",
                user.UserId.ToString(),
                user.RoleName,
                user.FullName,
                user.SupplierId.HasValue ? user.SupplierId.Value.ToString() : string.Empty);

            var ticket = new FormsAuthenticationTicket(
                1,
                user.Username,
                DateTime.Now,
                DateTime.Now.AddMinutes(persistent ? 60 * 24 * 14 : 60),
                persistent,
                userData,
                FormsAuthentication.FormsCookiePath);

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Secure = FormsAuthentication.RequireSSL,
                Path = FormsAuthentication.FormsCookiePath
            };
            if (persistent) cookie.Expires = ticket.Expiration;

            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        public static void SignOut()
        {
            FormsAuthentication.SignOut();
            if (HttpContext.Current.Session != null) HttpContext.Current.Session.Abandon();
        }

        /// <summary>Landing page for each role, used after login and by the "home" link.</summary>
        public static string HomePageFor(string roleName)
        {
            switch (roleName)
            {
                case RoleNames.Cashier: return "~/Cashier/POS.aspx";
                case RoleNames.Manager: return "~/Manager/Dashboard.aspx";
                case RoleNames.Supplier: return "~/Supplier/Orders.aspx";
                case RoleNames.Admin: return "~/Admin/Users.aspx";
                default: return "~/Default.aspx";
            }
        }
    }
}
