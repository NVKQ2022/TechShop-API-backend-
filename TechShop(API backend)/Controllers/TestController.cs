using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TechShop.API.Models;
using TechShop.API.Repositories;
using TechShop_API_backend_.Data;
using TechShop_API_backend_.Data.Authenticate;
using TechShop_API_backend_.Service;

namespace TechShop_API_backend_.Controllers
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
        JwtService _jwtService;
        EmailService emailService;
        private readonly ILogger<AuthenticateController> _logger;
        private readonly AuthProviderRepository _authProviderRepository;
        private string _googleClientId = Environment.GetEnvironmentVariable("GoogleOAuth__ClientId") ?? "";


        public TestController(IConfiguration config, UserRepository userRepository, UserDetailRepository userDetailRepository, ProductRepository productRepository, JwtService jwtService, ILogger<AuthenticateController> logger, AuthProviderRepository authProviderRepository, VerificationCodeRepository verificationCodeRepository, EmailService emailService)
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
    }
}
