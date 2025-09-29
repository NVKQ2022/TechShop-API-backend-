using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechShop_API_backend_.Models;
using TechShop_API_backend_.Data;
using System.ComponentModel;
using Microsoft.AspNetCore.Identity.Data;
using TechShop_API_backend_.Service;
using System.Security.Claims;
using TechShop_API_backend_.Models.Api;

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
        public async Task<ActionResult<LoginResponse>> Login(Models.Api.LoginRequest loginRequest)
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
        public async Task<IActionResult> Register([FromBody] User newUser)
        {
            // 1. Input validation
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 2. Check if the email or username already exists
            var existingUserByEmail = await _userRepository.GetUserByEmailAsync(newUser.Email);
            if (existingUserByEmail != null)
            {
                _logger.LogWarning("Registration failed: Email {Email} is already in use.", newUser.Email);
                return Conflict(new { Message = "Email is already in use." });
            }

            var existingUserByUsername = await _userRepository.GetUserByUsernameAsync(newUser.Username);
            if (existingUserByUsername != null)
            {
                _logger.LogWarning("Registration failed: Username {Username} is already in use.", newUser.Username);
                return Conflict(new { Message = "Username is already in use." });
            }

            try
            {
                // 3. Create the user with hashed password
                var createdUser = await _userRepository.CreateUserAsync(newUser.Email, newUser.Username, newUser.Password, false);

                // 4. Log successful registration
                _logger.LogInformation("User successfully registered: {Username}.", newUser.Username);

                // 5. Return CreatedAtAction for the login endpoint to indicate successful creation
                return CreatedAtAction(nameof(Login), new { id = createdUser.Id }, createdUser); // Respond with status 201 Created
            }
            catch (Exception ex)
            {
                // 6. Handle errors and log them
                _logger.LogError(ex, "An error occurred while registering the user: {Username}.", newUser.Username);
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
        }

        // PUT api/<AuthenticateController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<AuthenticateController>/5
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
