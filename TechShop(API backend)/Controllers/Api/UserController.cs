using Microsoft.AspNetCore.Mvc;
using TechShop_API_backend_.Models;
using TechShop_API_backend_.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TechShop_API_backend_.DTOs.User;
using TechShop_API_backend_.Helpers;
using TechShop_API_backend_.Models.Authenticate;
using TechShop_API_backend_.Data.Authenticate;
using TechShop_API_backend_.DTOs;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TechShop_API_backend_.Controllers.Api
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
        public async Task<IActionResult> Create([FromBody] CreateUserDto newUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdUser = await _userRepository.CreateUserAsync(newUser.Email, newUser.Username, newUser.Password, string.Empty, false);



            return CreatedAtAction(nameof(Get), new { id = createdUser.CreatedUser.Id }, createdUser);
        }


        // GET api/<UserController>/5
        [Authorize]
        [HttpGet("Info/me")] // DONE
        public async Task<IActionResult> Info()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


            User? user = await _userRepository.GetUserByIdAsync(int.Parse(userId));

            if (user == null)
            {
                return NotFound();
            }
            UserResponseDto userResponseDto = new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                IsAdmin = user.IsAdmin
            };
            return Ok(userResponseDto);
        }


        [Authorize]
        [HttpPost("Info/Profile/Add-receive-info")]
        public async Task<IActionResult> AddReceiveInfo([FromBody] ReceiveInfo receiveInfo)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userDetails = await _userDetailRepository.GetUserDetailAsync(int.Parse(userId));
            if (userDetails == null)
            {
                return NotFound();
            }
            userDetails.ReceiveInfo.Add(receiveInfo);
            var updateResult = await _userDetailRepository.UpdateUserDetailAsync(int.Parse(userId), userDetails);
            if (!updateResult)
            {
                return BadRequest("Failed to update receive info.");
            }
            return Ok("Receive info updated successfully.");
        }


        [Authorize]
        [HttpDelete("Info/Profile/Delete-receive-info")]
        public async Task<IActionResult> DeleteReceiveInfo([FromBody] ReceiveInfo receiveInfo) { 
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userDetails = await _userDetailRepository.GetUserDetailAsync(int.Parse(userId));
            if (userDetails == null)
            {
                return NotFound();
            }
            var infoToRemove = userDetails.ReceiveInfo.FirstOrDefault(info => 
                info.Name == receiveInfo.Name && 
                info.Phone == receiveInfo.Phone && 
                info.Address == receiveInfo.Address);
            if (infoToRemove == null)
            {
                return NotFound("Receive info not found.");
            }
            userDetails.ReceiveInfo.Remove(infoToRemove);
            var updateResult = await _userDetailRepository.UpdateUserDetailAsync(int.Parse(userId), userDetails);
            if (!updateResult)
            {
                return BadRequest("Failed to update receive info.");
            }
            return Ok("Receive info deleted successfully.");
        }

        [Authorize]
        [HttpPut("Info/Profile/add-personal-info")]
        public async Task<IActionResult> AddPersonalInfo([FromBody] PersonalInfoRequest personalInfo)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userDetails = await _userDetailRepository.GetUserDetailAsync(int.Parse(userId));
            if (userDetails == null)
            {
                return NotFound();
            }
            userDetails.Name = personalInfo.Name;
            userDetails.Gender = personalInfo.Gender;
            userDetails.Avatar = personalInfo.Avatar;
            userDetails.PhoneNumber = personalInfo.PhoneNumber;
            userDetails.Birthday = personalInfo.Birthday;

            var updateResult = await _userDetailRepository.UpdateUserDetailAsync(int.Parse(userId), userDetails);
            if (!updateResult)
            {
                return BadRequest("Failed to update personal info.");
            }
            return Ok("Personal info update successful");
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





        // PUT api/<UserController>/Update/Password
        [Authorize]
        [HttpPut("Account/Update/Password")]
        public async Task<IActionResult> Update([FromBody] UpdateUserDto updateUserDto)  //// DONE
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userUpdate = await _userRepository.GetUserByIdAsync(int.Parse(userId));
            if (userUpdate == null)
            {
                return Unauthorized();
            }
            var result = SecurityHelper.CheckPasswordStrength(updateUserDto.Password);
            if (result.IsStrong == false)
            {
                return BadRequest($"The password  is not strong enough");
            }


            if (SecurityHelper.HashPassword(updateUserDto.Password, userUpdate.Salt) == userUpdate.Password)
            {
                throw new Exception("New password must be different from the old password");
            }


            await _userRepository.UpdateUserAsync(userUpdate);
            return Ok();
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



        // DELETE api/User/Account/Delete
        [Authorize]
        [HttpDelete("Account/Delete")]
        public async Task<IActionResult> Delete() //DONE
        {
            // 1. Get userId from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("Invalid or missing user ID in token.");
            }

            // 2. Delete user and details (wrap in transaction-like behavior)
            try
            {
                var userDeleted = await _userRepository.DeleteUserAsync(userId);
                var detailsDeleted = await _userDetailRepository.DeleteUserDetailAsync(userId);

                if (userDeleted || detailsDeleted)
                {
                    return Ok(new { message = "Account deleted successfully." });
                }

                return NotFound(new { message = $"No account found for userId = {userId}" });
            }
            catch (Exception ex)
            {
                // log exception (Serilog, ILogger, etc.)
                return StatusCode(500, new { message = "An error occurred while deleting account.", error = ex.Message });
            }
        }

    }
}
