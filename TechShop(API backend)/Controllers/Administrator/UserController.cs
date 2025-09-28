using Microsoft.AspNetCore.Mvc;
using TechShop_API_backend_.Models;
using TechShop_API_backend_.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TechShop_API_backend_.Controllers.Administrator
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        UserRepository _userRepository;
        UserDetailRepository _userDetailRepository;
        public UserController(UserRepository userRepository, UserDetailRepository userDetailRepository)
        {
            _userRepository = userRepository;
            _userDetailRepository = userDetailRepository;
        }
        // GET: api/<UserController>
        [Authorize(Roles = "Admin")]
        [HttpGet("List")]
        public async Task<List<User>> Get()
        {
            List<User> users = await _userRepository.GetAllUsersAsync();
            return users; 
        }

        // GET api/<UserController>/5
        [Authorize(Roles = "Admin")]
        [HttpGet("Info/{id}")]
        public async Task<IActionResult> Info(int id)
        {
            User? user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }


        // POST api/<UserController>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] User newUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdUser = await _userRepository.CreateUserAsync(newUser.Email, newUser.Username, newUser.Password, false);



            return CreatedAtAction(nameof(Get), new { id = createdUser.Id }, createdUser);
        }


        // GET api/<UserController>/5
        [Authorize]
        [HttpGet("Info/me")]
        public async Task<IActionResult> Info()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            

            User? user = await _userRepository.GetUserByIdAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }



        // GET api/<UserController>/Info/Details
        [Authorize]
        [HttpGet("Info/Profile")]
        public async Task<IActionResult> InfoDetails()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userDetails = await _userDetailRepository.GetUserDetailAsync(int.Parse(userId));

            if (userDetails == null)
            {
                return NotFound();
            }
            return Ok(userDetails);
        }



       

        // PUT api/<UserController>/Update
        [Authorize]
        [HttpPut("Account/Update")]
        public async Task Update( [FromBody] User userUpdate)  //// ??? need to get value from json ( Username, Email , Password)
        {
            await _userRepository.UpdateUserAsync(userUpdate);
        }



        // PUT api/<UserController>/Update/Details
        [Authorize]
        [HttpPut("Profile/Update")]
        public async Task<IActionResult> UpdateDetails([FromBody] UserDetail userDetailsUpdate)
        {
            if (userDetailsUpdate == null)
            {
                return BadRequest("Update need to have value");
            }
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (await _userDetailRepository.UpdateUserDetailAsync(int.Parse(userId), userDetailsUpdate))
            {
                return Ok();
            }
            return BadRequest();

        }



        // DELETE api/<UserController>/5
        [Authorize]
        [HttpDelete("Account/Delete")]
        public void Delete(int id)
        {
            _userRepository.DeleteUserAsync(id);
        }
    }
}
