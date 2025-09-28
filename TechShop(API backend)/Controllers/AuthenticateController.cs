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

        public AuthenticateController(UserRepository userRepository, JwtService jwtService)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
        }


        // GET: api/<AuthenticateController>
        //[HttpGet]
        //public async Task<List<User>> Get()
        //{
        //    List<User> users  = await _userRepository.GetAllUsersAsync();
        //    return users;
        //}

        // GET api/<AuthenticateController>/5
        [AllowAnonymous]
        [HttpPost("login")]
        public async  Task<ActionResult<LoginResponse>> Login( Models.Api.LoginRequest loginRequest)
        {
            var result = await _jwtService.Authenticate(loginRequest);
            if (result == null)
            {
                return Unauthorized();
            }
            return result;
        }

        // POST api/<AuthenticateController>
        

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User newUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdUser = await _userRepository.CreateUserAsync(newUser.Email, newUser.Username, newUser.Password, false);



            return CreatedAtAction(nameof(Login), new { id = createdUser.Id }, createdUser); // fixed to point to Login action
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
