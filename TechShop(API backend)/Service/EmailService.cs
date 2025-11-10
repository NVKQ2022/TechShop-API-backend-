using System.Net.Mail;
using System.Net;
using TechShop_API_backend_.Data.Authenticate;
using TechShop.API.Models;
using TechShop.API.Repositories;
using TechShop_API_backend_.Helpers;

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

        UserRepository _userRepository;
        VerificationCodeRepository _verificationCodeRepository;


        public EmailService(UserRepository userRepository, VerificationCodeRepository verificationCodeRepository )
        {
            _userRepository = userRepository;
            _verificationCodeRepository = verificationCodeRepository;
        }



        public static void SendOtpEmail(string targetEmail, string otp)
        {
            string subject = "Your One-Time Password (OTP)";
            // ✅ Path to your HTML template (adjust if needed)
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Email-templates", "otp.html");

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



        public  async Task<(bool, string)> SendVerificationEmail(string targetEmail, string verifyToken)
        {
            string subject = "Verify Your Email Address";
            string message = string.Empty; // To hold the result message

            // Path to your HTML template (adjust if needed)
            //string templatePath = Path.GetFullPath(@"Source\Email-templates\verify.html");
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Email-templates", "verify.html");

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








        //public static async Task<(bool, string)> SendVerifyEmailProcessAsync(string targetEmail)
        //{
        //    try
        //    {
        //        // Step 1: Check if user exists
        //        var user = await _userRepository.GetUserByEmailAsync(targetEmail);
        //        if (user == null)
        //        {
        //            return (false, "User not found with this email.");
        //        }

        //        // Optional: Rate limiting to prevent spamming (uncomment if needed)
        //        // var existVerificationCode = await _verificationCodeRepository.GetLatestAsync(targetEmail, "EMAIL_VERIFY");
        //        // if (existVerificationCode != null)
        //        // {
        //        //     var timeSinceCreation = DateTime.Now - existVerificationCode.CreatedAt;
        //        //     if (timeSinceCreation.TotalMinutes < 1) // 1 minute cooldown
        //        //     {
        //        //         return (false, "Please wait before requesting another verification email.");
        //        //     }
        //        // }











        //        // Step 2: Generate the verification token
        //        var token = SecurityHelper.GenerateVerificationToken(targetEmail);

        //        // Step 3: Send the verification email and get the result
        //        var (isSuccess, message) = await SendVerificationEmail(targetEmail, token);
        //        if (!isSuccess)
        //        {
        //            return (false, $"step3 : {message}"); // If sending the email failed, return the failure message
        //        }

        //        // Step 4: Save the verification code in the database
        //        var verificationCode = new VerificationCode
        //        {
        //            UserId = user.Id,
        //            Email = targetEmail,
        //            Code = token,
        //            Type = "EMAIL_VERIFY",
        //            ExpiresAt = DateTime.UtcNow.AddHours(1),
        //            IsUsed = false,
        //            CreatedAt = DateTime.UtcNow
        //        };

        //        await _verificationCodeRepository.CreateAsync(verificationCode);

        //        // Step 5: Return success
        //        return (true, "Verification email sent successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the error (optional)
        //        return (false, $" exception:  {ex.Message}");
        //    }
        //}










        public  async Task<(bool, string)> SendVerifyEmailProcessAsync(string targetEmail)
        {
            try
            {
                // Step 1: Check if _userRepository is properly initialized
                if (_userRepository == null)
                {
                    throw new InvalidOperationException("_userRepository is null. Please ensure it is initialized.");
                }

                // Step 1: Check if user exists
                Console.WriteLine("Step 1: Checking if user exists for email: " + targetEmail);
                var user = await _userRepository.GetUserByEmailAsync(targetEmail);

                // Log if user is found or not
                if (user == null)
                {
                    Console.WriteLine("User not found with email: " + targetEmail);
                    return (false, "User not found with this email.");
                }

                Console.WriteLine("User found: " + user.Email);

                // Optional: Rate limiting to prevent spamming (uncomment if needed)
                // var existVerificationCode = await _verificationCodeRepository.GetLatestAsync(targetEmail, "EMAIL_VERIFY");
                // if (existVerificationCode != null)
                // {
                //     var timeSinceCreation = DateTime.Now - existVerificationCode.CreatedAt;
                //     if (timeSinceCreation.TotalMinutes < 1) // 1 minute cooldown
                //     {
                //         return (false, "Please wait before requesting another verification email.");
                //     }
                // }

                // Step 2: Generate the verification token
                Console.WriteLine("Step 2: Generating verification token.");
                var token = SecurityHelper.GenerateVerificationToken(targetEmail);

                // Log the token (be cautious not to expose sensitive information in production)
                Console.WriteLine("Generated Token: " + token);

                if (string.IsNullOrEmpty(token))
                {
                    return (false, "Failed to generate a verification token.");
                }

                // Step 3: Send the verification email and get the result
                Console.WriteLine("Step 3: Sending verification email.");
                var (isSuccess, message) = await SendVerificationEmail(targetEmail, token);

                // Log the result of email sending
                if (!isSuccess)
                {
                    Console.WriteLine("Email sending failed: " + message);
                    return (false, $"Step 3: {message}"); // If sending the email failed, return the failure message
                }

                Console.WriteLine("Verification email sent successfully.");

                // Step 4: Save the verification code in the database
                Console.WriteLine("Step 4: Saving verification code in the database.");
                var verificationCode = new VerificationCode
                {
                    UserId = user.Id,
                    Email = targetEmail,
                    Code = token,
                    Type = "EMAIL_VERIFY",
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                };

                // Log verification code details
                Console.WriteLine($"Verification Code: {verificationCode.Code}, ExpiresAt: {verificationCode.ExpiresAt}");

                // Ensure _verificationCodeRepository is initialized
                if (_verificationCodeRepository == null)
                {
                    throw new InvalidOperationException("Verification code repository is null.");
                }

                await _verificationCodeRepository.CreateAsync(verificationCode);

                // Step 5: Return success
                Console.WriteLine("Step 5: Process completed successfully.");
                return (true, "Verification email sent successfully.");
            }
            catch (Exception ex)
            {
                // Log the error message with exception details
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return (false, $"Exception: {ex.Message}");
            }
        }


    }
}
