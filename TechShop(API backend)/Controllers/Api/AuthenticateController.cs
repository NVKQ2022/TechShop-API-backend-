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
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin.Auth;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TechShop_API_backend_.Controllers.Api
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
        EmailService _emailService;
        private readonly ILogger<AuthenticateController> _logger;
        private readonly AuthProviderRepository _authProviderRepository;
        private string _googleClientId = Environment.GetEnvironmentVariable("GoogleOAuth__ClientId") ?? "";
        public AuthenticateController(UserRepository userRepository,EmailService emailService , VerificationCodeRepository verificationCodeRepository, JwtService jwtService, ILogger<AuthenticateController> logger, IConfiguration config, AuthProviderRepository authProviderRepository)
        {

            //FirebaseApp.Create(new AppOptions()
            //{
            //    Credential = GoogleCredential.FromFile("firebase-adminsdk.json")
            //});

            //need to test firebase admin sdk


            _config = config;
            _authProviderRepository = authProviderRepository;
            _verificationCodeRepository = verificationCodeRepository;
            _userRepository = userRepository;
            _jwtService = jwtService;
            _logger = logger;
            _emailService = emailService;
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





        public class SignInTokenRequest
        {
            public string IdToken { get; set; }
        }




        [AllowAnonymous]
        [HttpPost("firebase")]
        public async Task<IActionResult> FirebaseSignIn([FromBody] SignInTokenRequest request) // not test yet
        {
            try
            {
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken);

                string uid = string.Empty;
                string email = string.Empty;
                string provider = string.Empty;
                if (decodedToken.Claims != null && decodedToken.Claims.TryGetValue("firebase", out var firebaseObj))
                {
                    // firebaseObj is usually a Dictionary<string, object>
                    if (firebaseObj is IDictionary<string, object> firebaseDict &&
                        firebaseDict.TryGetValue("sign_in_provider", out var signInProviderObj) &&
                        signInProviderObj != null)
                    {
                        uid = decodedToken.Uid;
                        email = decodedToken.Claims.ContainsKey("email") ? decodedToken.Claims["email"].ToString() : null;
                        provider = signInProviderObj.ToString(); // e.g. "google.com", "facebook.com", "apple.com"
                    }

                }


                // Find or create user in your DB
                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    var createResult = await _userRepository.CreateUserAsync(email, email.Split('@')[0], "", uid, false);
                    if (!createResult.Success && createResult.ErrorMessage != null)
                    {
                        return BadRequest(new { message = createResult.ErrorMessage });
                    }
                    user = createResult.CreatedUser;
                }

                var token = _jwtService.GenerateToken(user);

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
                return Unauthorized(new { message = ex.Message });
            }
        }



















        [AllowAnonymous]
        [HttpPost("google")]
        public async Task<IActionResult> GoogleSignIn([FromBody] SignInTokenRequest request) // done
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
                var provider = await _authProviderRepository.GetByProviderAsync("google.com", googleId);
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
                var user = await _userRepository.GetUserByUsernameAsync(loginRequest.Username);

                if (!user.IsEmailVerified)
                {
                    _logger.LogWarning("Login attempt with unverified email for username: {Username}.", loginRequest.Username);
                    return Unauthorized(new { Message = "Email not verified. Please verify your email before logging in." });
                }

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




        //[AllowAnonymous]
        //[HttpPost("EmailVerify/Resend")]
        //public async Task<IActionResult> EmailVerify([FromBody] string targetEmail) 
        //{


        //    try
        //    {
        //        // prevent spaming mail with some rate limit logic later (e.g., allow resend only once every 5 minutes)
        //        var verificationCode = await  _verificationCodeRepository.GetLatestAsync(targetEmail, "EMAIL_VERIFY");

        //        // Check if a recent code was sent (rate limiting — allow resend only every 5 minutes)
        //        if (verificationCode != null && (DateTime.Now - verificationCode.CreatedAt).TotalMinutes < 5)
        //        {
        //            var remaining = 5 - (DateTime.Now - verificationCode.CreatedAt).TotalMinutes;
        //            return BadRequest(new { Message = $"Please wait {Math.Ceiling(remaining)} more minute(s) before requesting another verification email." });
        //        }

        //        // verified email 
        //        var token = SecurityHelper.GenerateVerificationToken(targetEmail);

        //        var newVerificationCode = new VerificationCode
        //        {
        //            UserId = verificationCode.UserId, // unknown at this point
        //            Email = targetEmail,
        //            Code = token,
        //            Type = "EMAIL_VERIFY",
        //            ExpiresAt = DateTime.Now.AddHours(1),
        //            IsUsed = false,
        //            CreatedAt = DateTime.Now
        //        };

        //        await _verificationCodeRepository.CreateAsync(newVerificationCode);
        //        await EmailService.SendVerificationEmail(targetEmail, token);

        //        return Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
        //    }
        //}

       



        [AllowAnonymous]
        [HttpGet("Email/verify")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string email, [FromQuery] string token)
        {
            try
            {
                var isMatched = await _verificationCodeRepository.VerifyAsync(email,"EMAIL_VERIFY", token);
                if (isMatched)
                {
                    await _verificationCodeRepository.DeleteAsync(email, "EMAIL_VERIFY", token);
                    var user =await _userRepository.GetUserByEmailAsync(email);
                    user.IsEmailVerified = true;
                    await _userRepository.UpdateUserAsync(user);
                    return Ok(new { Message = $"{user.Username} , your email ({email}) successfully verified  ." });
                }
                else
                {
                    return BadRequest(new { Message = "Invalid verification token or email." });
                }

            }
            catch (Exception ex)
            {
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
            if (newUser.Password != newUser.ConfirmPassword)
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

                


                // 3. Create the user with hashed password
                var createdUser = await _userRepository.CreateUserAsync(newUser.Email, newUser.Username, newUser.Password, string.Empty, false, false);
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
                //var token = await _jwtService.Authenticate(
                //new Models.Api.LoginRequest
                //{
                //    Username = newUser.Username,
                //    Password = newUser.Password
                //});


                // 5. Return CreatedAtAction for the login endpoint to indicate successful creation
                return Ok("please verify your email then login again"); // Respond with status 201 Created
            }
            catch (Exception ex)
            {
                // 6. Handle errors and log them
                _logger.LogError(ex, "An error occurred while registering the user: {Username}.", newUser.Username);
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
        }



        [AllowAnonymous]
        [HttpPost("EmailVerify/Send")]
        public async Task<IActionResult> EmailVerify([FromBody] string targetEmail)
        {

            var result = await _emailService.SendVerifyEmailProcessAsync(targetEmail);
            return Ok(result.Item2);
        }


        [Authorize]
        [HttpPut("changePassword")]
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
        [HttpPut("ResetPassword")]
        public async Task<IActionResult> ChangePasswordOnForgot([FromBody] ForgotPasswordDto forgotPasswordDto) //DONE
        {
            try
            {
                var user = await _userRepository.GetUserByEmailAsync(forgotPasswordDto.email);
                if (user == null)
                    return BadRequest("User not found.");

                var isVerified = await _verificationCodeRepository.IsVerifyCodeUsed(forgotPasswordDto.email, "PASSWORD_RESET", forgotPasswordDto.Otp);
                if (!isVerified)
                {
                    return BadRequest("OTP is incorrect or has not been used for verification.");
                }

                var result = SecurityHelper.CheckPasswordStrength(forgotPasswordDto.confirmPassword);
                if (result.IsStrong == false)
                {
                    return BadRequest($"The password  is not strong enough");
                }


                // Update password
                if (forgotPasswordDto.newPassword != forgotPasswordDto.confirmPassword)
                {
                    return BadRequest("Password and Confirm Password do not match");
                }

                var salt = SecurityHelper.GenerateSalt();
                var newHashedPassword = SecurityHelper.HashPassword(forgotPasswordDto.confirmPassword, salt);
                user.Password = newHashedPassword;
                user.Salt = salt;
                bool updateResult = await _userRepository.UpdateUserAsync(user);
                if (!updateResult)
                {
                    return StatusCode(500, "Failed to update password.");
                }

                await _verificationCodeRepository.DeleteAsync(forgotPasswordDto.email, "PASSWORD_RESET", forgotPasswordDto.Otp);
                return Ok("Password changed successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
        }






        [AllowAnonymous]
        [HttpPost("Email/Opt/Sent/ForgotPassword")]
        public async Task<IActionResult> EmailOPT([FromBody] string email)
        {
            try
            {


                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                    return BadRequest("User not found.");

                var otp = SecurityHelper.GenerateOTP(6);

                EmailService.SendOtpEmail(user.Email, otp);

                var verificationCode = new VerificationCode
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Code = otp,
                    Type = "PASSWORD_RESET",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _verificationCodeRepository.CreateAsync(verificationCode);

                return Ok(new
                {
                    Message = "Verification code email sent successfully.",
                    user.Email
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
        [HttpPost("Email/Opt/Verify/PassWord")]
        public async Task<IActionResult> OPTVerify([FromBody] VerifyOtpDto verifyOtpDto) //DONE
        {
            try
            {



                var isMatched = await _verificationCodeRepository.VerifyAsync(verifyOtpDto.email, "PASSWORD_RESET", verifyOtpDto.otp);

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
