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

        

        // Test: Get Verification by Email
        [HttpGet("GetByEmailWithExpire")]
        public async Task<IActionResult> GetVerificationByEmailAsync([FromQuery] string email)
        {
            var verification = await _verificationCodeRepository.GetVerificationByEmailNotExpireAsync(email);
            if (verification == null)
            {
                return NotFound("No unexpired verification found for the given email.");
            }
            return Ok(verification);
        }

        [HttpGet("GetByEmail")]
        public async Task<IActionResult> GetVerificationByEmailAllAsync([FromQuery] string email)
        {
            var verification = await _verificationCodeRepository.GetVerificationByEmailAsync(email);
            if (verification == null)
            {
                return NotFound("No verification found for the given email.");
            }
            return Ok(verification);
        }


        // Test: Get Verification by Type
        [HttpGet("GetByType")]
        public async Task<IActionResult> GetVerificationByTypeAsync([FromQuery] string type)
        {
            var verification = await _verificationCodeRepository.GetVerificationByTypeAsync(type);
            if (verification == null)
            {
                return NotFound("No unexpired verification found for the given type.");
            }
            return Ok(verification);
        }

        // Test: Get Verification by Code
        [HttpGet("GetByCode")]
        public async Task<IActionResult> GetVerificationByCodeAsync([FromQuery] string code)
        {
            var verification = await _verificationCodeRepository.GetByCodeAsync(code);
            if (verification == null)
            {
                return NotFound("No unexpired verification found for the given code.");
            }
            return Ok(verification);
        }



    }
}
