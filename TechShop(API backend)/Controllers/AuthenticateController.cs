using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechShop_API_backend_.Models;
using System.ComponentModel;
using Microsoft.AspNetCore.Identity.Data;
using TechShop_API_backend_.Service;
using System.Security.Claims;
using TechShop_API_backend_.Models.Api;
using TechShop_API_backend_.DTOs.User;
using TechShop_API_backend_.Helpers;
using TechShop_API_backend_.Data.Authenticate;
using TechShop_API_backend_.Models.Authenticate;
using MongoDB.Driver;
using static System.Net.WebRequestMethods;
using TechShop.API.Models;
using TechShop.API.Repositories;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TechShop_API_backend_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuthenticateController : ControllerBase
    {
        UserRepository _userRepository;
        VerificationCodeRepository _verificationCodeRepository;
        JwtService _jwtService;
        EmailService emailService;
        private readonly ILogger<AuthenticateController> _logger;

        public AuthenticateController(UserRepository userRepository, VerificationCodeRepository verificationCodeRepository, JwtService jwtService, ILogger<AuthenticateController> logger)
        {
            _verificationCodeRepository = verificationCodeRepository;
            _userRepository = userRepository;
            _jwtService = jwtService;
            _logger = logger;
        }


        // GET: api/<AuthenticateController>
        //[HttpGet]
        //public async Task<List<User>> Get()
        //{
        //    List<User> users  = await _userRepository.GetAllUsersAsync();
        //    return users;
        //}

        // GET api/<AuthenticateController>/login
        //[AllowAnonymous]
        //[HttpPost("login")]
        //public async  Task<ActionResult<LoginResponse>> Login( Models.Api.LoginRequest loginRequest)
        //{
        //    var result = await _jwtService.Authenticate(loginRequest);
        //    if (result == null)
        //    {
        //        return Unauthorized();
        //    }
        //    return result;
        //}



        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(Models.Api.LoginRequest loginRequest) // DONE
        {
            // Input validation
            if (loginRequest == null)
            {
                return BadRequest("Invalid login request.");
            }

            if (string.IsNullOrWhiteSpace(loginRequest.Username) || string.IsNullOrWhiteSpace(loginRequest.Password))
            {
                return BadRequest("Username and password are required.");
            }

            try
            {
                var result = await _jwtService.Authenticate(loginRequest);

                if (result == null)
                {
                    // Log failed attempt for auditing purposes
                    _logger.LogWarning("Login failed for username: {Username}. Invalid credentials.", loginRequest.Username);
                    return Unauthorized(new { Message = "Invalid username or password." });
                }

                // Log successful login for auditing purposes
                _logger.LogInformation("Login successful for username: {Username}.", loginRequest.Username);

                return Ok(result); // Return the login response (could include token and user details)
            }
            catch (Exception ex)
            {
                // Log exception
                _logger.LogError(ex, "An error occurred while processing the login request for username: {Username}.", loginRequest.Username);
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
        }




        // POST api/<AuthenticateController>


        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserDto newUser) //DONE
        {
            // 1. Input validation
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if( newUser.Password != newUser.ConfirmPassword)
            {
                return BadRequest("Password and Confirm Password do not match");
            }

            var result = SecurityHelper.CheckPasswordStrength(newUser.Password);
            if (result.IsStrong == false)
            {
                return BadRequest($"The password  is not strong enough");
            }

            try
            {

                // verified email 




                // 3. Create the user with hashed password
                var createdUser = await _userRepository.CreateUserAsync(newUser.Email, newUser.Username, newUser.Password, false);
                if (createdUser.ErrorMessage == "Email already exists")
                {
                    _logger.LogWarning("Registration failed: Email {Email} is already in use.", newUser.Email);
                    return Conflict(new { Message = "Email is already in use." });
                }
                if (createdUser.ErrorMessage == "Username already exists")
                {
                    _logger.LogWarning("Registration failed: Username {Username} is already in use.", newUser.Username);
                    return Conflict(new { Message = "Username is already in use." });
                }
                // 4. Log successful registration
                _logger.LogInformation("User successfully registered: {Username}.", newUser.Username);
                var token = await _jwtService.Authenticate(
                new Models.Api.LoginRequest
                {
                    Username = newUser.Username,
                    Password = newUser.Password
                });

                // 5. Return CreatedAtAction for the login endpoint to indicate successful creation
                return Ok(token); // Respond with status 201 Created
            }
            catch (Exception ex)
            {
                // 6. Handle errors and log them
                _logger.LogError(ex, "An error occurred while registering the user: {Username}.", newUser.Username);
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
        }













        [AllowAnonymous]
        [HttpPost("test/EmailVerify/{targetEmail}")]
        public async Task<IActionResult> EmailVerify(string targetEmail="23521267@gm.uit.edu.vn") //DONE
        {
           

            try
            {

                // verified email 

               
                EmailService.SendVerificationEmail(targetEmail, "1234567890");

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
        }

        [Authorize]
        [HttpGet("Email/Opt")]
        public async Task<IActionResult> EmailOPT()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return BadRequest("Invalid user ID.");

                if (!int.TryParse(userId, out int parsedUserId))
                    return BadRequest("Invalid user ID format.");

                var user = await _userRepository.GetUserByIdAsync(parsedUserId);
                if (user == null)
                    return BadRequest("User not found.");

                var otp = SecurityHelper.GenerateOTP(6);

                EmailService.SendOptEmail(user.Email, otp);

                var verificationCode = new VerificationCode
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Code = otp,
                    Type = "EMAIL_VERIFY",
                    ExpiresAt = DateTime.Now.AddMinutes(10),
                    IsUsed = false,
                    CreatedAt = DateTime.Now
                };

                await _verificationCodeRepository.CreateAsync(verificationCode);

                return Ok(new
                {
                    Message = "Verification email sent successfully.",
                    Email = user.Email
                });
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
        [HttpPost("testEmail/Opt/Verify/{otp}")]
        public async Task<IActionResult> OPTVerify(string otp) //DONE
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
              

                var isMatched =await _verificationCodeRepository.VerifyAsync(int.Parse(userId!), "EMAIL_VERIFY",otp);

                if (isMatched) 
                {
                    return Ok("Verified");
                }
                else
                {
                    return BadRequest("Wrong otp code");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
        }




















        // PUT api/<AuthenticateController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<AuthenticateController>/D
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            bool isComplete = await _userRepository.DeleteUserAsync(id);

            if (isComplete)
            {
                return NoContent();
            }

            return NotFound();



        }
    }
}
