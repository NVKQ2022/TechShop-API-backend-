using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechShop_API_backend_.Data;
using TechShop_API_backend_.DTOs.Admin;
using TechShop_API_backend_.Service;

namespace TechShop_API_backend_.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AdminRepository _adminRepository;
        private readonly MongoMetricsService _mongoMetricsService;
        private readonly UserDetailRepository _userDetailRepository;
        private readonly OrderRepository _orderRepository;

        public AdminController(AdminRepository adminRepository, MongoMetricsService mongoMetricsService, UserDetailRepository userDetailRepository,
                                OrderRepository orderRepository)
        {
            _adminRepository = adminRepository;
            _mongoMetricsService = mongoMetricsService;
            _userDetailRepository = userDetailRepository;
            _orderRepository = orderRepository;
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

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var overview = await _mongoMetricsService.GetOverviewAsync();
            return Ok(overview);
        }

        // collStats từng collection
        [HttpGet("collection/{name}")]
        public async Task<IActionResult> GetCollectionStats(string name)
        {
            var stats = await _mongoMetricsService.GetCollectionStatsAsync(name);
            return Ok(stats);
        }

        [HttpGet("server")]
        public async Task<IActionResult> GetServerMetrics()
        {
            var metrics = await _mongoMetricsService.GetServerMetricsAsync();
            return Ok(metrics);
        }

        [HttpGet("collection/{name}/indexes")]
        public async Task<IActionResult> GetCollectionIndexStats(string name)
        {
            var stats = await _mongoMetricsService.GetIndexStatsForCollectionAsync(name);
            return Ok(stats);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? keyword = null)
        {
            var users = await _adminRepository.GetUsersPagedAsync(page, pageSize, keyword);
            var totalCount = await _adminRepository.GetUsersCountAsync(keyword);

            var data = users.Select(u => new AdminUserListItemDto
            {
                Id = u.Id,
                Email = u.Email,
                Username = u.Username,
                IsAdmin = u.IsAdmin,
                IsEmailVerified = u.IsEmailVerified
            }).ToList();

            return Ok(new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                data
            });
        }

        [HttpGet("users/{id}/overview")]
        public async Task<IActionResult> GetUserOverview(int id)
        {
            var overview = await _adminRepository.GetUserOverviewAsync(id);
            if (overview == null)
                return NotFound(new { message = $"User {id} not found." });

            return Ok(overview);
        }

        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleDto dto)
        {
            var success = await _adminRepository.UpdateUserRoleAsync(id, dto.IsAdmin);
            if (!success) return NotFound();

            return Ok();
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUserByAdmin(int id)
        {
            var success = await _adminRepository.DeleteUserByAdminAsync(id);
            if (!success) return NotFound();

            return Ok(new { message = $"User {id} deleted by admin." });
        }

        [HttpDelete("users")]
        public async Task<IActionResult> DeleteManyUsers([FromBody] List<int> userIds)
        {
            if (userIds == null || userIds.Count == 0)
                return BadRequest("No user IDs provided.");

            var deletedCount = await _adminRepository.DeleteManyUsersAsync(userIds);

            if (deletedCount == 0)
                return NotFound("No users were deleted. Check if the provided IDs are correct.");

            return Ok(new
            {
                message = "Users deleted successfully.",
                requested = userIds.Count,
                deleted = deletedCount
            });
        }

    }
}
