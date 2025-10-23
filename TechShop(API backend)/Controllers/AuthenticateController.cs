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

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TechShop_API_backend_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuthenticateController : ControllerBase
    {
        UserRepository _userRepository;
        JwtService _jwtService;
        EmailService emailService;
        private readonly ILogger<AuthenticateController> _logger;

        public AuthenticateController(UserRepository userRepository, JwtService jwtService, ILogger<AuthenticateController> logger)
        {
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
        [HttpPost("testEmail/Verify")]
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


        [AllowAnonymous]
        [HttpPost("testEmail/Opt")]
        public async Task<IActionResult> EmailOPT(string targetEmail = "23521267@gm.uit.edu.vn") //DONE
        {


            try
            {

                // verified email 


               
                EmailService.SendOptEmail(targetEmail, "654321");

                return Ok();
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
