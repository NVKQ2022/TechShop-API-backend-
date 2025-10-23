
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace TechShop_API_backend_.Helpers
{
    public class SecurityHelper
    {
        private static readonly string myPepper = Environment.GetEnvironmentVariable("Security__Pepper");
        private static readonly string serverEmail = Environment.GetEnvironmentVariable("Security__Email");
        private static readonly string serverEmailPassword = Environment.GetEnvironmentVariable("Security__Password");
        public SecurityHelper(/*IConfiguration configuration*/)
        {
            //myPepper = configuration["Security:Pepper"];
        }

        public static string GenerateSessionId(int length = 32) // use when login
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var data = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(data);
            }

            var result = new StringBuilder(length);
            foreach (byte b in data)
            {
                result.Append(chars[b % chars.Length]);
            }

            return result.ToString();
        }

        public static string GenerateSalt(int length = 10) // use in register
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var saltBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            var saltChars = new char[length];
            for (int i = 0; i < length; i++)
            {
                saltChars[i] = chars[saltBytes[i] % chars.Length];
            }

            return new string(saltChars);
        }

        public static string HashPassword(string password, string salt)
        {
            string combined = password + salt + myPepper;
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
                //return password;///need to fix
                return Convert.ToBase64String(hashBytes);
            }
        }

        public static bool VerifyPassword(string inputPassword, string storedSalt, string storedHash) // use when login
        {
            string newHash = HashPassword(inputPassword, storedSalt);
            return storedHash == newHash;
        }
        public static (bool IsStrong, string Rating) CheckPasswordStrength(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Weak");

            bool hasMinimumLength = password.Length >= 8;
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

            int score = 0;
            if (hasMinimumLength) score++;
            if (hasUpper) score++;
            if (hasLower) score++;
            if (hasDigit) score++;
            if (hasSpecial) score++;

            // Determine if password is strong
            bool isStrong = hasMinimumLength && hasUpper && hasLower && hasDigit && hasSpecial;

            // Determine rating
            string rating = score switch
            {
                <= 2 => "Weak",
                3 or 4 => "Medium",
                5 => "Strong",
                _ => "Weak"
            };

            return (isStrong, rating);
        }
       



    }
}
