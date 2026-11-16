using System;
using System.Security.Cryptography;

namespace AutoRent.Helpers
{
    // PBKDF2: lưu salt + hash, không bao giờ lưu mật khẩu gốc.
    public static class PasswordHelper
    {
        public static string Hash(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
            {
                return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(pbkdf2.GetBytes(32));
            }
        }
        public static bool Verify(string password, string stored)
        {
            try
            {
                var parts = stored.Split('.');
                if (parts.Length != 2) return false;
                var salt = Convert.FromBase64String(parts[0]);
                var expected = Convert.FromBase64String(parts[1]);
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
                {
                    var actual = pbkdf2.GetBytes(expected.Length);
                    int diff = actual.Length ^ expected.Length;
                    for (int i = 0; i < actual.Length && i < expected.Length; i++) diff |= actual[i] ^ expected[i];
                    return diff == 0;
                }
            }
            catch { return false; }
        }
    }
}
