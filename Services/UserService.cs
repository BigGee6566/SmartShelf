using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SmartShelf.Models;

namespace SmartShelf.Services
{
    /// <summary>Admin-side account management. Passwords only ever enter as plain text and leave as hashes.</summary>
    public static class UserService
    {
        public static List<User> GetUsers()
        {
            using (var db = new SmartShelfContext())
            {
                return db.Users
                    .Include(u => u.Role)
                    .Include(u => u.Supplier)
                    .AsNoTracking()
                    .OrderBy(u => u.Role.Name).ThenBy(u => u.FullName)
                    .ToList();
            }
        }

        public static User GetUser(int userId)
        {
            using (var db = new SmartShelfContext())
            {
                return db.Users.Include(u => u.Role).AsNoTracking()
                    .FirstOrDefault(u => u.UserId == userId);
            }
        }

        public static List<Role> GetRoles()
        {
            using (var db = new SmartShelfContext())
            {
                return db.Roles.AsNoTracking().OrderBy(r => r.RoleId).ToList();
            }
        }

        /// <summary>
        /// Creates or updates an account. <paramref name="newPassword"/> is optional on an edit -
        /// leaving it blank keeps the existing hash rather than blanking the password.
        /// </summary>
        public static int SaveUser(int userId, string username, string fullName, string email,
                                   int roleId, int? supplierId, bool isActive, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new BusinessRuleException("A username is required.");
            if (username.Trim().Length > 50)
                throw new BusinessRuleException("The username must be 50 characters or fewer.");
            if (string.IsNullOrWhiteSpace(fullName))
                throw new BusinessRuleException("A full name is required.");
            if (!string.IsNullOrWhiteSpace(email) && !email.Contains("@"))
                throw new BusinessRuleException("That does not look like a valid email address.");

            var name = username.Trim();

            using (var db = new SmartShelfContext())
            {
                var role = db.Roles.FirstOrDefault(r => r.RoleId == roleId);
                if (role == null) throw new BusinessRuleException("Choose a role for this user.");

                // A supplier login is meaningless without a company to scope it to.
                if (role.Name == RoleNames.Supplier && !supplierId.HasValue)
                    throw new BusinessRuleException("A supplier account must be linked to a supplier company.");
                if (role.Name != RoleNames.Supplier) supplierId = null;

                if (supplierId.HasValue && !db.Suppliers.Any(s => s.SupplierId == supplierId.Value))
                    throw new BusinessRuleException("That supplier company no longer exists.");

                if (db.Users.Any(u => u.Username == name && u.UserId != userId))
                    throw new BusinessRuleException("The username \"" + name + "\" is already taken.");

                User user;
                if (userId == 0)
                {
                    if (string.IsNullOrEmpty(newPassword))
                        throw new BusinessRuleException("A password is required for a new account.");

                    user = new User { CreatedAt = DateTime.Now };
                    db.Users.Add(user);
                }
                else
                {
                    user = db.Users.FirstOrDefault(u => u.UserId == userId);
                    if (user == null) throw new BusinessRuleException("That user no longer exists.");
                }

                user.Username = name;
                user.FullName = fullName.Trim();
                user.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
                user.RoleId = roleId;
                user.SupplierId = supplierId;
                user.IsActive = isActive;

                if (!string.IsNullOrEmpty(newPassword))
                {
                    ValidatePasswordStrength(newPassword);
                    user.PasswordSalt = PasswordHasher.CreateSalt();
                    user.PasswordHash = PasswordHasher.Hash(newPassword, user.PasswordSalt);
                }

                db.SaveChanges();
                return user.UserId;
            }
        }

        public static void ResetPassword(int userId, string newPassword)
        {
            ValidatePasswordStrength(newPassword);

            using (var db = new SmartShelfContext())
            {
                var user = db.Users.FirstOrDefault(u => u.UserId == userId);
                if (user == null) throw new BusinessRuleException("That user no longer exists.");

                user.PasswordSalt = PasswordHasher.CreateSalt();
                user.PasswordHash = PasswordHasher.Hash(newPassword, user.PasswordSalt);
                db.SaveChanges();
            }
        }

        /// <summary>Deactivating rather than deleting keeps the audit trail on sales and adjustments intact.</summary>
        public static void SetActive(int userId, bool isActive, int actingUserId)
        {
            if (userId == actingUserId && !isActive)
                throw new BusinessRuleException("You cannot deactivate your own account.");

            using (var db = new SmartShelfContext())
            {
                var user = db.Users.Include(u => u.Role).FirstOrDefault(u => u.UserId == userId);
                if (user == null) throw new BusinessRuleException("That user no longer exists.");

                // Locking out the last administrator would leave nobody able to fix it.
                if (!isActive && user.Role.Name == RoleNames.Admin)
                {
                    var otherAdmins = db.Users.Count(u => u.Role.Name == RoleNames.Admin
                                                          && u.IsActive && u.UserId != userId);
                    if (otherAdmins == 0)
                        throw new BusinessRuleException("This is the only active administrator - deactivating it would lock everyone out.");
                }

                user.IsActive = isActive;
                db.SaveChanges();
            }
        }

        private static void ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new BusinessRuleException("A password is required.");
            if (password.Length < 8)
                throw new BusinessRuleException("The password must be at least 8 characters long.");
            if (password.Length > 128)
                throw new BusinessRuleException("The password must be 128 characters or fewer.");
            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
                throw new BusinessRuleException("The password must contain at least one letter and one number.");
        }
    }
}
