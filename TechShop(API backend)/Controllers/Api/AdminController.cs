using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechShop_API_backend_.Data;
using TechShop_API_backend_.DTOs.Admin;

namespace TechShop_API_backend_.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AdminRepository _adminRepository;

        public AdminController(AdminRepository adminRepository)
        {
            _adminRepository = adminRepository;
        }

        [HttpGet("total-users")]
        public async Task<IActionResult> GetTotalUsers()
        {
            var total = await _adminRepository.GetTotalUsersAsync();
            return Ok(new { totalUsers = total });
        }

        [HttpGet("total-orders")]
        public async Task<IActionResult> GetTotalOrders()
        {
            var total = await _adminRepository.GetTotalOrdersAsync();
            return Ok(new { totalOrders = total });
        }

        [HttpGet("top-sellers")]
        public async Task<ActionResult<List<TopProductOrderDto>>> GetTopProductsMostOrdered()
        {
            var result = await _adminRepository.GetTop5MostOrderedProductsAsync();
            return Ok(result);
        }

        [HttpGet("recent-orders")]
        public async Task<ActionResult<List<TopProductOrderDto>>> GetRecentOrder()
        {
            var result = await _adminRepository.GetLatestOrdersAsync();
            return Ok(result);
        }

        [HttpGet("sorted-orders")]
        public async Task<IActionResult> GetOrdersPaged([FromQuery] int page = 1)
        {
            const int pageSize = 12;

            var orders = await _adminRepository.GetOrdersPagedAsync(page, pageSize);
            var totalCount = await _adminRepository.GetOrdersCountAsync();

            return Ok(new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                data = orders
            });
        }

    }
}
