using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using TechShop_API_backend_.Data;
using TechShop_API_backend_.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TechShop_API_backend_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WishlistController : ControllerBase
    {
        UserDetailRepository _userDetailRepository;
        public WishlistController(UserDetailRepository userDetailRepository)
        {
            _userDetailRepository = userDetailRepository;
        }
        /// <summary>
        /// Get all wishlist items for a user.
        /// </summary>
        [Authorize]
        [HttpGet()]
        public async Task<IActionResult> GetWishlist()
        { 
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userDetailRepository.GetUserByUserId(int.Parse(userId));
            if (user == null)
                return NotFound("User not found.");

            return Ok(user.Wishlist ?? new List<WishlistItem>());
        }

        /// <summary>
        /// Add a new item to the user's wishlist.
        /// </summary>
        [Authorize]
        [HttpPost("add")]
        public async Task<IActionResult> AddWishlistItem([FromBody] WishlistItem wishlistItem)
        {
            if (wishlistItem == null || string.IsNullOrEmpty(wishlistItem.ProductId))
                return BadRequest("Invalid wishlist item data.");

            
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var success = await _userDetailRepository.AddWishlistItemAsync(int.Parse(userId), wishlistItem);
            if (!success)
                return Conflict("Item already exists in wishlist or user not found.");

            return Ok("Item added to wishlist successfully.");
        }

        /// <summary>
        /// Remove an item from the user's wishlist.
        /// </summary>
        [Authorize]
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveWishlistItem(string productId)
        {

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


            var success = await _userDetailRepository.RemoveWishlistItemAsync(int.Parse(userId), productId);
            if (!success)
                return NotFound("Wishlist item not found or user not found.");

            return Ok("Item removed from wishlist.");
        }

        /// <summary>
        /// Move an item from wishlist to cart.
        /// </summary>
        [HttpPost("move/{productId}")]
        public async Task<IActionResult> MoveWishlistItemToCart(string productId)
        {

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var success = await _userDetailRepository.MoveWishlistItemToCartAsync(int.Parse(userId), productId);
            if (!success)
                return BadRequest("Failed to move item from wishlist to cart.");

            return Ok("Item moved to cart successfully.");
        }
    }
}

