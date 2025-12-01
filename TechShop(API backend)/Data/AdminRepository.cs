using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TechShop_API_backend_.Data.Context;
using TechShop_API_backend_.DTOs.Admin;
using TechShop_API_backend_.Models;
using TechShop_API_backend_.Models.Authenticate;

namespace TechShop_API_backend_.Data
{
    public class AdminRepository
    {
        private readonly AuthenticateDbContext _context;
        private readonly IMongoCollection<Order> _order;
        private readonly OrderRepository _orderRepository;
        private readonly ProductRepository _productRepository;
        private readonly UserDetailRepository _userDetailRepository;

        public AdminRepository(AuthenticateDbContext context, IOptions<MongoDbSettings> settings, OrderRepository orderRepository, ProductRepository productRepository,
                                UserDetailRepository userDetailRepository)
        {
            _context = context;
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _order = database.GetCollection<Order>(settings.Value.OrderCollectionName);
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _userDetailRepository = userDetailRepository;
        }

        public async Task<int> GetTotalUsersAsync()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<long> GetTotalOrdersAsync()
        {
            return await _order.CountDocumentsAsync(Builders<Order>.Filter.Empty);
        }

        public async Task<List<TopProductOrderDto>> GetTop5MostOrderedProductsAsync()
        {
            var allOrders = await _orderRepository.GetAllOrdersAsync();

            var productStats = allOrders
                .SelectMany(o => o.Items.Select(i => new { o.OrderID, o.Status, i.ProductID }))
                .GroupBy(x => x.ProductID)
                .Select(g => new
                {
                    ProductId = g.Key,

                    TotalOrderCount = g
                        .Select(x => x.OrderID)
                        .Distinct()
                        .Count(),

                    DeliveredOrderCount = g
                        .Where(x => x.Status == "Delivered")
                        .Select(x => x.OrderID)
                        .Distinct()
                        .Count()
                })

                .OrderByDescending(x => x.DeliveredOrderCount)
                .Take(5)
                .ToList();

            var productIds = productStats.Select(x => x.ProductId).ToList();

            var products = await _productRepository.GetByIdsAsync(productIds);

            var result = (
                from g in productStats
                join p in products on g.ProductId equals p.ProductId
                orderby g.DeliveredOrderCount descending
                select new TopProductOrderDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.Name,
                    Image = p.ImageURL[0],
                    Category = p.Category,
                    Rating = CalculateAverageRating(p),

                    SelledCount = g.DeliveredOrderCount,
                    OrderCount = g.TotalOrderCount
                }).ToList();

            return result;
        }

        public async Task<List<Order>> GetOrdersPagedAsync(int page, int pageSize = 12)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 12;

            var skip = (page - 1) * pageSize;

            return await _order
                .Find(Builders<Order>.Filter.Empty)
                .SortByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<List<Order>> GetLatestOrdersAsync(int count = 10)
        {
            if (count <= 0) count = 10;

            return await _order
                .Find(Builders<Order>.Filter.Empty)
                .SortByDescending(o => o.CreatedAt)
                .Limit(count)
                .ToListAsync();
        }

        public async Task<long> GetOrdersCountAsync()
        {
            return await _order.CountDocumentsAsync(Builders<Order>.Filter.Empty);
        }

        public async Task<List<User>> GetUsersPagedAsync(int page, int pageSize, string? keyword = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 12;

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(u =>
                    u.Email.Contains(keyword) ||
                    u.Username.Contains(keyword));
            }

            var skip = (page - 1) * pageSize;

            return await query
                .OrderBy(u => u.Id)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetUsersCountAsync(string? keyword = null)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(u =>
                    u.Email.Contains(keyword) ||
                    u.Username.Contains(keyword));
            }

            return await query.CountAsync();
        }

        public async Task<AdminUserOverviewDto?> GetUserOverviewAsync(int userId)
        {

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            var detail = await _userDetailRepository.GetUserDetailAsync(userId);

            var orders = await _orderRepository.GetOrdersByUserAsync(userId);

            var totalOrders = orders.Count;

            int notConfirmOrders = orders.Count(o => o.Status == "NotConfirm");
            int pendingOrders = orders.Count(o => o.Status == "Pending");
            int confirmedOrders = orders.Count(o => o.Status == "Confirmed");
            int processingOrders = orders.Count(o => o.Status == "Processing");
            int shippedOrders = orders.Count(o => o.Status == "Shipped");
            int deliveredOrders = orders.Count(o => o.Status == "Delivered");
            int cancelledOrders = orders.Count(o => o.Status == "Cancelled");


            int totalSpent = orders
                .Where(o => o.Status == "Delivered")
                .Sum(o => o.TotalAmount);

            DateTime? firstOrderAt = null;
            DateTime? lastOrderAt = null;

            if (orders.Any())
            {
                firstOrderAt = orders.Min(o => o.CreatedAt);
                lastOrderAt = orders.Max(o => o.CreatedAt);
            }

            var dto = new AdminUserOverviewDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                IsAdmin = user.IsAdmin,
                IsEmailVerified = user.IsEmailVerified,

                Name = detail?.Name,
                Avatar = detail?.Avatar,
                PhoneNumber = detail?.PhoneNumber,
                Gender = detail?.Gender,

                Birthday = (detail == null || detail.Birthday == default)
                    ? null
                    : detail.Birthday,

                CartItemCount = detail?.Cart?.Count ?? 0,
                WishlistCount = detail?.Wishlist?.Count ?? 0,

                TotalOrders = totalOrders,
                NotConfirmOrders = notConfirmOrders,
                PendingOrders = pendingOrders,
                ConfirmedOrders = confirmedOrders,
                ProcessingOrders = processingOrders,
                ShippedOrders = shippedOrders,
                DeliveredOrders = deliveredOrders,
                CancelledOrders = cancelledOrders,

                TotalSpent = totalSpent,
                FirstOrderAt = firstOrderAt,
                LastOrderAt = lastOrderAt
            };

            return dto;
        }

        public async Task<bool> UpdateUserRoleAsync(int userId, bool isAdmin)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            user.IsAdmin = isAdmin;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserByAdminAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            await _userDetailRepository.DeleteUserDetailAsync(userId);
            // await _orderRepository.DeleteOrdersByUserIdAsync(userId);

            return true;
        }

        public async Task<int> DeleteManyUsersAsync(IEnumerable<int> userIds)
        {
            if (userIds == null)
                return 0;

            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
                return 0;

            var users = await _context.Users
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            if (users.Count == 0)
                return 0;

            _context.Users.RemoveRange(users);
            await _context.SaveChangesAsync();

            var deleteDetailTasks = ids
                .Select(id => _userDetailRepository.DeleteUserDetailAsync(id));
            await Task.WhenAll(deleteDetailTasks);

            return users.Count;
        }



        //HELPERS
        private double CalculateAverageRating(Product p)
        {
            var r1 = p.Rating["rate_1"];
            var r2 = p.Rating["rate_2"];
            var r3 = p.Rating["rate_3"];
            var r4 = p.Rating["rate_4"];
            var r5 = p.Rating["rate_5"];

            var totalVotes = r1 + r2 + r3 + r4 + r5;
            if (totalVotes == 0) return 0;

            var sum = 1 * r1 + 2 * r2 + 3 * r3 + 4 * r4 + 5 * r5;

            var avg = (double)sum / totalVotes;

            return Math.Round(avg, 1);
        }
    }
}
