namespace ZYRAHRM.IntegrationApp.Helper
{
    using System.Security.Cryptography;
    using System.Text;

    public static class PasswordHelper
    {
        public static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key; // unique key acts as salt
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        public static bool VerifyPasswordHash(string password, string storedHashBase64, string storedSaltBase64)
        {
            var storedHash = Convert.FromBase64String(storedHashBase64);
            var storedSalt = Convert.FromBase64String(storedSaltBase64);

            using (var hmac = new HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                return storedHash.SequenceEqual(computedHash);
            }
        }
    }

}
