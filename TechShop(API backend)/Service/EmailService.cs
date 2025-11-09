using System.Net.Mail;
using System.Net;
using TechShop_API_backend_.Data.Authenticate;

namespace TechShop_API_backend_.Service
{
    public class EmailService
    {

        private static readonly string serverEmail =Environment.GetEnvironmentVariable("Security__Email")
        ?? throw new InvalidOperationException("Server email environment variable is not set.");

        private static readonly string serverEmailPassword =Environment.GetEnvironmentVariable("Security__Password")
        ?? throw new InvalidOperationException("Server password environment variable is not set.");

        private static readonly string baseUrl = Environment.GetEnvironmentVariable("BaseUrl")
        ?? throw new InvalidOperationException("Base Url environment variable is not set.");

        static UserRepository _userRepository;


        public EmailService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }



        public static void SendOptEmail(string targetEmail, string otp)
        {
            string subject = "Your One-Time Password (OTP)";
            // ✅ Path to your HTML template (adjust if needed)
            string templatePath = Path.GetFullPath(@"Source\Email-templates\otp.html");
            try
            {
                if (!File.Exists(templatePath))
                {
                    Console.WriteLine($"❌ Template not found: {templatePath}");
                    return;
                }
                // Read the HTML file
                string htmlBody = File.ReadAllText(templatePath);
                // Replace placeholders
                htmlBody = htmlBody.Replace("{{OTP_CODE}}", otp);
                // Create and send email
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(serverEmail, "MyApp OTP Service");
                mail.To.Add(targetEmail);
                mail.Subject = subject;
                mail.Body = htmlBody;
                mail.IsBodyHtml = true;
                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential(serverEmail, serverEmailPassword);
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
                Console.WriteLine($"✅ OTP email sent to {targetEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Failed to send email: " + ex.Message);
            }
        }



        public static async Task<(bool, string)> SendVerificationEmail(string targetEmail, string verifyToken)
        {
            string subject = "Verify Your Email Address";
            string message = string.Empty; // To hold the result message

            // Path to your HTML template (adjust if needed)
            string templatePath = Path.GetFullPath(@"Source\Email-templates\verify.html");

            try
            {
                if (!File.Exists(templatePath))
                {
                    message = $"❌ Template not found: {templatePath}";
                    return (false, message);
                }

                // Read the HTML file
                string htmlBody = File.ReadAllText(templatePath);

                // Get user by email
                var user = await _userRepository.GetUserByEmailAsync(targetEmail);
                Console.WriteLine($"Searching for user with email: {targetEmail}");

                if (user == null)
                {
                    message = "❌ User not found with this targetEmail in Email service.";
                    return (false, message);  // Exit and return false if no user is found
                }

                Console.WriteLine($"Found user: {user.Username}");

                // Ensure baseUrl is set correctly
                string BaseUrl = baseUrl; // Replace with actual base URL

                string verifyUrl = $"{BaseUrl}/api/authenticate/email/verify?email={Uri.EscapeDataString(targetEmail)}&token={verifyToken}";

                // Replace placeholders
                htmlBody = htmlBody.Replace("{{VERIFY_LINK}}", verifyUrl);
                htmlBody = htmlBody.Replace("{{USERNAME}}", user.Username);

                // Create and send email
                if (string.IsNullOrEmpty(serverEmail) || string.IsNullOrEmpty(serverEmailPassword))
                {
                    message = "❌ Server email or password is null or empty.";
                    return (false, message);
                }

                MailMessage mail = new MailMessage
                {
                    From = new MailAddress(serverEmail, "TechShop Verification"),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mail.To.Add(targetEmail);

                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential(serverEmail, serverEmailPassword);
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }

                message = $"✅ Verification email sent to {targetEmail}";
                return (true, message);  // Return success with message
            }
            catch (Exception ex)
            {
                message = "❌ Failed to send email: " + ex.Message;
                return (false, message);  // Return false and the exception message
            }
        }


    }
}
