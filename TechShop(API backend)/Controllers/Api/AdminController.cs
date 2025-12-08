using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechShop_API_backend_.Data;
using TechShop_API_backend_.DTOs.Admin;
using TechShop_API_backend_.Models;
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

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 12,
    [FromQuery] string? keyword = null,
    [FromQuery] string? category = null)
        {
            var products = await _adminRepository.GetProductsPagedAsync(page, pageSize, keyword, category);
            var totalCount = await _adminRepository.GetProductsCountAsync(keyword, category);

            // Tính rating average ngay ở đây cho gọn
            double CalcAvgRating(Product p)
            {
                if (p.Rating == null || p.Rating.Count == 0) return 0;

                int r1 = p.Rating.ContainsKey("rate_1") ? p.Rating["rate_1"] : 0;
                int r2 = p.Rating.ContainsKey("rate_2") ? p.Rating["rate_2"] : 0;
                int r3 = p.Rating.ContainsKey("rate_3") ? p.Rating["rate_3"] : 0;
                int r4 = p.Rating.ContainsKey("rate_4") ? p.Rating["rate_4"] : 0;
                int r5 = p.Rating.ContainsKey("rate_5") ? p.Rating["rate_5"] : 0;

                var totalVotes = r1 + r2 + r3 + r4 + r5;
                if (totalVotes == 0) return 0;

                var sum = 1 * r1 + 2 * r2 + 3 * r3 + 4 * r4 + 5 * r5;
                return Math.Round((double)sum / totalVotes, 1);
            }

            var data = products.Select(p => new AdminProductListItemDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Category = p.Category,
                Price = p.Price,
                Stock = p.Stock,
                Sold = p.Sold,
                RatingAverage = CalcAvgRating(p),
                IsOnSale = p.Sale != null && p.Sale.IsActive,
                SalePercent = p.Sale != null && p.Sale.IsActive ? p.Sale.Percent : (double?)null,
                ImageURL = p.ImageURL ?? new List<string>()        // 👈 THÊM
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

        [HttpGet("products/{id}/overview")]
        public async Task<IActionResult> GetProductOverview(string id)
        {
            var overview = await _adminRepository.GetProductOverviewAsync(id);
            if (overview == null) return NotFound(new { message = "Product not found." });

            return Ok(overview);
        }

        [HttpGet("products/statistics")]
        public async Task<IActionResult> GetProductStats()
        {
            var stats = await _adminRepository.GetProductStatsAsync();
            return Ok(stats);
        }

        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProductByAdmin(string id)
        {
            var success = await _adminRepository.DeleteProductAsync(id);
            if (!success)
                return NotFound(new { message = "Product not found." });

            return Ok(new { message = "Product deleted successfully." });
        }

        [HttpDelete("products")]
        public async Task<IActionResult> DeleteManyProducts([FromBody] List<string> productIds)
        {
            if (productIds == null || productIds.Count == 0)
                return BadRequest("No product IDs provided.");

            var deletedCount = await _adminRepository.DeleteManyProductsAsync(productIds);

            if (deletedCount == 0)
                return NotFound("No products were deleted. Check if the provided IDs are correct.");

            return Ok(new
            {
                message = "Products deleted successfully.",
                requested = productIds.Count,
                deleted = deletedCount
            });
        }

        [HttpPut("products/{id}/sale")]
        public async Task<IActionResult> UpdateProductSale(string id, [FromBody] UpdateProductSaleDto dto)
        {
            if (dto == null)
                return BadRequest("Sale info is required.");

            if (dto.Percent < 0 || dto.Percent > 1)
                return BadRequest("Percent should be between 0 and 1 (e.g. 0.2 for 20%).");

            if (dto.EndDate <= dto.StartDate)
                return BadRequest("EndDate must be greater than StartDate.");

            var saleInfo = new SaleInfo
            {
                Percent = dto.Percent,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive
            };

            var success = await _adminRepository.UpdateProductSaleAsync(id, saleInfo);
            if (!success)
                return NotFound(new { message = "Product not found." });

            return Ok(new { message = "Sale updated successfully." });
        }

        [HttpGet("products/active-sales")]
        public async Task<IActionResult> GetActiveSaleProducts()
        {
            var products = await _adminRepository.GetActiveSaleProductsAsync();
            return Ok(products);
        }

        [HttpPost("products/random-sales")]
        public async Task<IActionResult> ApplyRandomSales([FromQuery] int numberOfProducts = 5)
        {
            if (numberOfProducts <= 0)
                return BadRequest("numberOfProducts must be greater than 0.");

            await _adminRepository.ApplyRandomSalesToProductsAsync(numberOfProducts);

            return Ok(new { message = $"Random sales applied to up to {numberOfProducts} products." });
        }

        [HttpPost("products")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = await _adminRepository.CreateProductAsync(dto);

            return CreatedAtAction(
                nameof(GetProductOverview),
                new { id = product.ProductId },
                product
            );
        }

        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(string id, [FromBody] UpdateProductDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = await _adminRepository.UpdateProductAsync(id, dto);
            if (product == null)
            {
                return NotFound(new { message = "Product not found." });
            }

            return Ok(product);
        }

        [HttpGet("sales-events")]
        public async Task<IActionResult> GetSaleEvents()
        {
            var events = await _adminRepository.GetAllSaleEventsAsync();
            return Ok(events); // List<AdminSaleEventDto>
        }

        [HttpPost("sales-events")]
        public async Task<IActionResult> CreateSaleEvent([FromBody] CreateAdminSaleEventDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid sale event data.");

            if (dto.StartDate == default || dto.EndDate == default)
                return BadRequest("StartDate and EndDate are required.");

            if (dto.EndDate < dto.StartDate)
                return BadRequest("EndDate must be after StartDate.");

            if (dto.Percent < 0 || dto.Percent > 1)
                return BadRequest("Percent must be between 0 and 1.");

            var created = await _adminRepository.CreateSaleEventAsync(dto);
            return Ok(created); // AdminSaleEventDto
        }

        [HttpPut("sales-events/{id}")]
        public async Task<IActionResult> UpdateSaleEvent(string id, [FromBody] UpdateAdminSaleEventDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid sale event data.");

            if (dto.StartDate == default || dto.EndDate == default)
                return BadRequest("StartDate and EndDate are required.");

            if (dto.EndDate < dto.StartDate)
                return BadRequest("EndDate must be after StartDate.");

            if (dto.Percent < 0 || dto.Percent > 1)
                return BadRequest("Percent must be between 0 and 1.");

            var success = await _adminRepository.UpdateSaleEventAsync(id, dto);
            if (!success) return NotFound("Sale event not found.");

            return Ok();
        }

        [HttpDelete("sales-events")]
        public async Task<IActionResult> DeleteSaleEvents([FromBody] List<string> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest("No ids provided.");

            var deleted = await _adminRepository.DeleteSaleEventsAsync(ids);

            return Ok(new
            {
                requested = ids.Count,
                deleted
            });
        }

        [HttpGet("orders/daily-stats")]
        public async Task<ActionResult<List<AdminDailyOrderStatDto>>> GetDailyOrderStats(
            [FromQuery] int days = 30)
        {
            var result = await _adminRepository.GetDailyOrderStatsAsync(days);
            return Ok(result);
        }

        // 2) Orders – status distribution (pie chart)
        // GET: api/Admin/orders/status-distribution
        [HttpGet("orders/status-distribution")]
        public async Task<ActionResult<List<AdminOrderStatusDistributionDto>>> GetOrderStatusDistribution()
        {
            var result = await _adminRepository.GetOrderStatusDistributionAsync();
            return Ok(result);
        }

        // 3) Orders – payment methods (pie/bar chart)
        // GET: api/Admin/orders/payment-methods
        [HttpGet("orders/payment-methods")]
        public async Task<ActionResult<List<AdminPaymentMethodStatDto>>> GetPaymentMethodStats()
        {
            var result = await _adminRepository.GetPaymentMethodStatsAsync();
            return Ok(result);
        }

        // 4) Products – category stats (bar chart)
        // GET: api/Admin/products/category-stats
        [HttpGet("products/category-stats")]
        public async Task<ActionResult<List<AdminProductCategoryStatDto>>> GetProductCategoryStats()
        {
            var result = await _adminRepository.GetProductCategoryStatsAsync();
            return Ok(result);
        }

        // 5) Products – rating distribution (bar 1–5 sao)
        // GET: api/Admin/products/rating-distribution
        [HttpGet("products/rating-distribution")]
        public async Task<ActionResult<List<AdminRatingDistributionDto>>> GetRatingDistribution()
        {
            var result = await _adminRepository.GetRatingDistributionAsync();
            return Ok(result);
        }

        // 6) Users – demographics (gender + age)
        // GET: api/Admin/users/demographics
        [HttpGet("users/demographics")]
        public async Task<ActionResult<AdminDemographicsDto>> GetUserDemographics()
        {
            var result = await _adminRepository.GetUserDemographicsAsync();
            return Ok(result);
        }

    }
}
