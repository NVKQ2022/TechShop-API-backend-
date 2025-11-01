using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechShop_API_backend_.Models;
using TechShop_API_backend_.DTOs.Auth;
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
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Google.Apis.Auth;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TechShop_API_backend_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuthenticateController : ControllerBase
    {
        private readonly IConfiguration _config;
        UserRepository _userRepository;
        VerificationCodeRepository _verificationCodeRepository;
        JwtService _jwtService;
        EmailService emailService;
        private readonly ILogger<AuthenticateController> _logger;
        private readonly AuthProviderRepository _authProviderRepository;
        private string _googleClientId =   Environment.GetEnvironmentVariable("GoogleOAuth__ClientId") ?? "";
        public AuthenticateController(UserRepository userRepository, VerificationCodeRepository verificationCodeRepository, JwtService jwtService, ILogger<AuthenticateController> logger, IConfiguration config, AuthProviderRepository authProviderRepository)
        {
            _config = config;
            _authProviderRepository=authProviderRepository;
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





        public class GoogleSignInRequest
        {
            public string IdToken { get; set; }
        }


        [AllowAnonymous]
        [HttpPost("google")]
        public async Task<IActionResult> GoogleSignIn([FromBody] GoogleSignInRequest request) //Not test yet
        {
            try
            {
                if (request?.IdToken == null)
                {
                    return BadRequest(new { message = "idToken is required." });
                }

                // Log the request for debugging
                Console.WriteLine($"Received idToken: {request.IdToken}");
                var idToken = request.IdToken;
                // 1️⃣ Verify Google ID Token
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _googleClientId }
                });

                var googleId = payload.Subject;
                var email = payload.Email;
                var name = payload.Name ?? email.Split('@')[0];

                // 2️⃣ Check if provider record already exists
                var provider = await _authProviderRepository.GetByProviderAsync("google", googleId);
                User? user = null;

                if (provider == null)
                {
                    // 3️⃣ If provider not found, check if user already exists by email
                    user = await _userRepository.GetUserByEmailAsync(email);

                    // 4️⃣ If user doesn’t exist → create one
                    if (user == null)
                    {
                        // Create new user
                        var createResult = await _userRepository.CreateUserAsync(email, name, string.Empty, googleId, false);
                        user = createResult.CreatedUser;
                    }

                    // 5️⃣ Create new auth provider link
                    var newProvider = new AuthProvider
                    {

                        UserId = user.Id,
                        Provider = "google",
                        ProviderUserId = googleId,
                        ProviderEmail = email,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _authProviderRepository.AddAsync(newProvider);
                }
                else
                {
                    // 6️⃣ If provider exists → fetch the associated user
                    user = provider.User;

                    if (user == null)
                        return Unauthorized("User record not found for this Google account.");
                }

                // 7️⃣ Generate JWT token
                var token = _jwtService.GenerateToken(user);

                // 8️⃣ Return consistent login response
                return Ok(new
                {
                    userId = user.Id,
                    username = user.Username,
                    token,
                    isAdmin = user.IsAdmin,
                    expiresIn = _jwtService.expireMinutes * 60
                });
            }
            catch (Exception ex)
            {
                return
                    Unauthorized(new { message = ex.Message });
            }
        }

       






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
                var createdUser = await _userRepository.CreateUserAsync(newUser.Email, newUser.Username, newUser.Password, string.Empty,false);
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






        [Authorize]
        [HttpPut("changePassword/Authenticated")]
        public async Task
            <IActionResult> ChangePasswordAuthenticated([FromBody] ChangePasswordDto changePasswordDto) //DONE
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var user = await _userRepository.GetUserByIdAsync(int.Parse(userId!));
                if (user == null)
                    return BadRequest("User not found.");
                // Verify current password
                if (!SecurityHelper.VerifyPassword(changePasswordDto.CurrentPassword, user.Salt, user.Password))
                {
                    return BadRequest("Current password is incorrect.");
                }
                var result = SecurityHelper.CheckPasswordStrength(changePasswordDto.NewPassword);
                if (result.IsStrong == false)
                {
                    return BadRequest($"The password  is not strong enough");
                }
                // Update password
                var salt = SecurityHelper.GenerateSalt();
                var hashedPassword = SecurityHelper.HashPassword(changePasswordDto.NewPassword, salt);
                user.Password = hashedPassword;
                user.Salt = salt;
                bool updateResult = await _userRepository.UpdateUserAsync(user);
                if (!updateResult)
                {
                    return StatusCode(500, "Failed to update password.");
                }
                return Ok("Password changed successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
        }





        [AllowAnonymous]
        [HttpPut("changePassword/Forgot")]
        public async Task<IActionResult> ChangePasswordOnForgot([FromBody] ChangePasswordDto changePasswordDto) //DONE
        {
            try
            {
                var user = await _userRepository.GetUserByEmailAsync(changePasswordDto.Email);
                if (user == null)
                    return BadRequest("User not found.");
                var isMatched = await _verificationCodeRepository.VerifyAsync(user.Id, "EMAIL_VERIFY", changePasswordDto.OTP);
                if (!isMatched)
                {
                    return BadRequest("Invalid or expired OTP code.");
                }
                var result = SecurityHelper.CheckPasswordStrength(changePasswordDto.NewPassword);
                if (result.IsStrong == false)
                {
                    return BadRequest($"The password  is not strong enough");
                }
                // Update password
                var salt = SecurityHelper.GenerateSalt();
                var hashedPassword = SecurityHelper.HashPassword(changePasswordDto.NewPassword, salt);
                user.Password = hashedPassword;
                user.Salt = salt;
                bool updateResult = await _userRepository.UpdateUserAsync(user);
                if (!updateResult)
                {
                    return StatusCode(500, "Failed to update password.");
                }
                return Ok("Password changed successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
        }   



        


        [AllowAnonymous]
        [HttpGet("Email/Opt/{email}")]
        public async Task<IActionResult> EmailOPT(string email)
        {
            try
            {
                

                var user = await _userRepository.GetUserByEmailAsync(email);
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
        [HttpPost("testEmail/Opt/Verify")]
        public async Task<IActionResult> OPTVerify([FromBody] string otp) //DONE
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
