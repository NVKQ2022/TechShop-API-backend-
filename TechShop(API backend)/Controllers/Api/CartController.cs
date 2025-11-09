using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using TechShop_API_backend_.Data;
using TechShop_API_backend_.DTOs.CartItem;
using TechShop_API_backend_.Helpers;
using TechShop_API_backend_.Interfaces;
using TechShop_API_backend_.Models;

namespace TechShop_API_backend_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {

        UserDetailRepository _userDetailRepository;
        ProductRepository _productRepository;
        ConverterHelper converterHelper = new ConverterHelper();

        public CartController(UserDetailRepository userDetailRepository, ProductRepository productRepository)
        {
            _userDetailRepository = userDetailRepository;
            _productRepository = productRepository;
        }

        [Authorize]
        [HttpGet("check")]
        public async Task<IActionResult> CheckIsAdded(string productId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var success = await _userDetailRepository.IsProductInCartAsync(int.Parse(userId), productId);


            return Ok(success);
        }

        // ✅ Get all cart items for the current user
        [HttpGet]
        public async Task<IActionResult> GetCartItems()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            if (!int.TryParse(userIdClaim, out int userId))
                return BadRequest("Invalid user ID format.");

            var items = await _userDetailRepository.GetCartItemsAsync(userId);
            return Ok(items);
        }

        // ✅ Add a new item to cart
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] CreateCartItemRequest request)
        {
            // ✅ Step 1: Verify user token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            if (!int.TryParse(userIdClaim, out int userId))
                return BadRequest("Invalid user ID format.");

            // ✅ Step 2: Validate input
            if (request == null || string.IsNullOrEmpty(request.ProductId))
                return BadRequest("Invalid request. Product ID is required.");

            if (request.Quantity <= 0)
                return BadRequest("Quantity must be greater than 0.");

            // ✅ Step 3: Verify product existence & details
            var product = await _productRepository.GetByIdAsync(request.ProductId);
            if (product == null)
                return NotFound($"Product with ID {request.ProductId} not found.");

            if (product.Stock < request.Quantity)
                return BadRequest("Not enough stock available.");

            // ✅ Step 4: Build verified CartItem

            var cartItem = converterHelper.ConvertProductToCartItem(product);


            // ✅ Step 5: Add to cart in DB
            bool success = await _userDetailRepository.AddCartItemAsync(userId, cartItem);
            if (!success)
                return StatusCode(500, "Failed to add item to cart.");

            return Ok(new { message = "Item added to cart successfully.", item = cartItem });
        }


        // ✅ Update the quantity of an existing item
        [HttpPut("update")]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            if (!int.TryParse(userIdClaim, out int userId))
                return BadRequest("Invalid user ID format.");

            if (request == null || string.IsNullOrEmpty(request.ProductId))
                return BadRequest("Invalid cart item data.");

            var result = await _userDetailRepository.UpdateCartItemQuantityAsync(userId, request.ProductId, request.Quantity);
            if (result == null)
                return NotFound("Cart item not found.");

            return Ok(new { message = "Cart item updated successfully.", newQuantity = result });
        }

        // ✅ Remove an item from cart
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromCart(string productId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            if (!int.TryParse(userIdClaim, out int userId))
                return BadRequest("Invalid user ID format.");

            if (string.IsNullOrEmpty(productId))
                return BadRequest("Invalid product ID.");

            await _userDetailRepository.RemoveCartItemAsync(userId, productId);
            return Ok(new { message = "Item removed from cart successfully." });
        }

        // ✅ Get total number of cart items
        [HttpGet("count")]
        public async Task<IActionResult> CountCartItems()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            if (!int.TryParse(userIdClaim, out int userId))
                return BadRequest("Invalid user ID format.");

            var count = await _userDetailRepository.CountCartItems(userId);
            return Ok(new { count });
        }



    }
}
