using System;
using System.Security.Cryptography;

namespace SmartShelf.Services
{
    /// <summary>
    /// PBKDF2 password hashing. Plain-text passwords are never stored, logged or rendered -
    /// only the salt and the derived hash go to the database.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltBytes = 16;
        private const int HashBytes = 32;
        private const int Iterations = 100000;

        public static string CreateSalt()
        {
            var salt = new byte[SaltBytes];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }

        public static string Hash(string password, string base64Salt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrEmpty(base64Salt)) throw new ArgumentNullException("base64Salt");

            var salt = Convert.FromBase64String(base64Salt);

            // HMACSHA1 is the .NET Framework default for Rfc2898DeriveBytes; the 100k iteration
            // count is what makes offline guessing expensive.
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
            {
                return Convert.ToBase64String(pbkdf2.GetBytes(HashBytes));
            }
        }

        /// <summary>Compares in fixed time so a wrong password can't be narrowed down by timing.</summary>
        public static bool Verify(string password, string base64Salt, string expectedHash)
        {
            if (string.IsNullOrEmpty(base64Salt) || string.IsNullOrEmpty(expectedHash)) return false;

            string actual;
            try
            {
                actual = Hash(password ?? string.Empty, base64Salt);
            }
            catch (FormatException)
            {
                // Corrupt salt in the database - treat as a failed login rather than crashing.
                return false;
            }

            return FixedTimeEquals(actual, expectedHash);
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
