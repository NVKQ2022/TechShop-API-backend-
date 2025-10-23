using System.Net.Mail;
using System.Net;

namespace TechShop_API_backend_.Service
{
    public class EmailService
    {

        private static readonly string serverEmail =Environment.GetEnvironmentVariable("Security__Email")
        ?? throw new InvalidOperationException("Server email environment variable is not set.");

        private static readonly string serverEmailPassword =Environment.GetEnvironmentVariable("Security__Password")
        ?? throw new InvalidOperationException("Server password environment variable is not set.");
        

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



        public static void SendVerificationEmail(string targetEmail, string verifyToken)
        {
            string subject = "Verify Your Email Address";

            // ✅ Path to your HTML template (adjust if needed)
            string templatePath = Path.GetFullPath(@"Source\Email-templates\verify.html");

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
                string verifyUrl = $"https://yourapp.com/verify?email={Uri.EscapeDataString(targetEmail)}&token={verifyToken}";
                htmlBody = htmlBody.Replace("{{VERIFY_LINK}}", verifyUrl);

                // Create and send email
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(serverEmail, "MyApp Verification");
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

                Console.WriteLine($"✅ Verification email sent to {targetEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Failed to send email: " + ex.Message);
            }
        }
    }
}
