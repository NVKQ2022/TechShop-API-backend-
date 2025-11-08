
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace TechShop_API_backend_.Helpers
{
    public class SecurityHelper
    {
        private static readonly string myPepper = Environment.GetEnvironmentVariable("Security__Pepper");

        public SecurityHelper(/*IConfiguration configuration*/)
        {
        
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

        public static bool VerifyPassword(string inputPassword, string storedSalt, string passwordHash) // use when login
        {
            string newHash = HashPassword(inputPassword, storedSalt);
            return passwordHash == newHash;
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

        public static string GenerateOTP(int length = 6)
        {
            if (length <= 0 || length > 10)
                throw new ArgumentOutOfRangeException(nameof(length), "OTP length must be between 1 and 10.");

            // Use a cryptographically secure random number generator
            using (var rng = RandomNumberGenerator.Create())
            {
                // Each digit of the OTP
                var otp = new char[length];

                byte[] randomBytes = new byte[length];

                rng.GetBytes(randomBytes);

                for (int i = 0; i < length; i++)
                {
                    // Convert each byte to a digit (0–9)
                    otp[i] = (char)('0' + (randomBytes[i] % 10));
                }

                return new string(otp);
            }
        }

        public static string GenerateVerificationToken(string email)
        {
            // Combine the email with a timestamp for uniqueness
            string rawToken = email + DateTime.UtcNow.Ticks.ToString();

            // Create an HMACSHA256 hash from the raw token and a secret key
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(myPepper)))
            {
                byte[] tokenBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
                return Convert.ToBase64String(tokenBytes); // Base64 encode the token
            }
        }


    }
}
