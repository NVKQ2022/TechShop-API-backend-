using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TechShop.API.Models;
using TechShop.API.Repositories;
using TechShop_API_backend_.Data;
using TechShop_API_backend_.Data.Authenticate;
using TechShop_API_backend_.Helpers;
using TechShop_API_backend_.Service;

namespace TechShop_API_backend_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IConfiguration _config;
        UserRepository _userRepository;
        VerificationCodeRepository _verificationCodeRepository;
        UserDetailRepository _userDetailRepository;
        ProductRepository _productRepository;
        SecurityHelper _securityHelper;
        JwtService _jwtService;
        EmailService emailService;
        private readonly ILogger<AuthenticateController> _logger;
        private readonly AuthProviderRepository _authProviderRepository;
        private string _googleClientId = Environment.GetEnvironmentVariable("GoogleOAuth__ClientId") ?? "";


        public TestController(
                            IConfiguration config,
                            UserRepository userRepository,
                            UserDetailRepository userDetailRepository,
                            ProductRepository productRepository,
                            JwtService jwtService,
                            ILogger<AuthenticateController> logger,
                            AuthProviderRepository authProviderRepository,
                            VerificationCodeRepository verificationCodeRepository,
                            EmailService emailService,
                            SecurityHelper securityHelper
                            )

        {
            _config = config;
            _productRepository = productRepository;
            _userDetailRepository = userDetailRepository;
            _userRepository = userRepository;
            _jwtService = jwtService;
            _logger = logger;
            _authProviderRepository = authProviderRepository;
            _verificationCodeRepository = verificationCodeRepository;
            this.emailService = emailService;
            _securityHelper = securityHelper;
        }



        [AllowAnonymous]
        [HttpPost("test")]
        public async Task<IActionResult> TestOTP() //DONE
        {
            try
            {
                var verificationCode = new VerificationCode
                {
                    UserId = 10096,
                    Email = "123124",
                    Code = "12314",
                    Type = "EMAIL_VERIFY",
                    ExpiresAt = DateTime.Now.AddMinutes(10),
                    IsUsed = false,
                    CreatedAt = DateTime.Now
                };

                await _verificationCodeRepository.CreateAsync(verificationCode);
                return Ok("OTP code has been sent.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while processing your request.",
                    Error = ex.Message,
                    Inner = ex.InnerException?.Message
                });
            }
        }


        [AllowAnonymous]
        [HttpPost("wishlist")]

        public async Task<IActionResult> TestWishlist() //DONE
        {
            _userDetailRepository.EnsureWishlistFieldExists();
            return Ok("Wishlist field ensured.");

        }



        [AllowAnonymous]
        [HttpPost("AddRandomStockForAllProduct")]
        public async Task<IActionResult> AddRandomStock() //DONE
        {
            try
            {

                await _productRepository.AddRandomStockToAllProductsAsync();
                return Ok("Random stock values added to all products.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while processing your request.",
                    Error = ex.Message,
                    Inner = ex.InnerException?.Message
                });
            }
        }


        [AllowAnonymous]
        [HttpPost("ensureHaveSaleInfo")]
        public async Task<IActionResult> EnsureSaleInfo()
        {
            await _productRepository.EnsureAllProductsHaveSaleInfoAsync();
            return Ok();
        }


        [AllowAnonymous]
        [HttpPost("RandomSale/{number}")]

        public async Task<IActionResult> RandomSale(int number)
        {
            await _productRepository.ApplyRandomSalesAsync(number);
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("EmailVerify/Send")]
        public async Task<IActionResult> EmailVerify([FromBody] string targetEmail)
        {
            try
            {
                // Step 1: Check if user exists
                var user = await _userRepository.GetUserByEmailAsync(targetEmail);

                if (user == null)
                {
                    return BadRequest("User not found with this email.");
                }

                // Step 2: Generate the verification token
                var token = SecurityHelper.GenerateVerificationToken(targetEmail);

                // Step 3: Send the verification email and get the result
                var (isSuccess, message) = await EmailService.SendVerificationEmail(targetEmail, token);

                // Step 4: Check if email was successfully sent
                if (!isSuccess)
                {
                    // If sending the email failed, return the failure message
                    return BadRequest(message);
                }

                // Step 5: Save the verification code in the database
                var verificationCode = new VerificationCode
                {
                    UserId = user.Id, // User ID is available now
                    Email = targetEmail,
                    Code = token,
                    Type = "EMAIL_VERIFY",
                    ExpiresAt = DateTime.Now.AddHours(1),
                    IsUsed = false,
                    CreatedAt = DateTime.Now
                };

                await _verificationCodeRepository.CreateAsync(verificationCode);

                // Step 6: Return success
                return Ok(new { Message = "Verification email sent successfully." });
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
        }




    }
}
