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
        private readonly IMongoCollection<ProductSaleEvent> _saleEvents;

        private readonly IMongoCollection<Product> _product;
        private readonly IMongoCollection<UserDetail> _userDetail;

        public AdminRepository(AuthenticateDbContext context, IOptions<MongoDbSettings> settings, OrderRepository orderRepository, ProductRepository productRepository,
                                UserDetailRepository userDetailRepository)
        {
            _context = context;
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _order = database.GetCollection<Order>(settings.Value.OrderCollectionName);
            _saleEvents = database.GetCollection<ProductSaleEvent>(settings.Value.ProductSaleEventCollectionName);
            _product = database.GetCollection<Product>(settings.Value.ProductCollectionName);
            _userDetail = database.GetCollection<UserDetail>(settings.Value.UserDetailCollectionName);
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


            decimal totalSpent = orders
                .Where(o => o.Status == "Delivered")
                .Sum(o => (decimal)o.TotalAmount);

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

        public async Task<List<Product>> GetProductsPagedAsync(
            int page,
            int pageSize,
            string? keyword = null,
            string? category = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 12;

            var products = await _productRepository.GetAllAsync();
            var query = products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(p =>
                    !string.IsNullOrEmpty(p.Name) &&
                    p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category == category);
            }

            var skip = (page - 1) * pageSize;

            return query
                .OrderBy(p => p.Name)
                .Skip(skip)
                .Take(pageSize)
                .ToList();
        }

        public async Task<int> GetProductsCountAsync(string? keyword = null, string? category = null)
        {
            var products = await _productRepository.GetAllAsync();
            var query = products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(p =>
                    !string.IsNullOrEmpty(p.Name) &&
                    p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category == category);
            }

            return query.Count();
        }

        public async Task<AdminProductOverviewDto?> GetProductOverviewAsync(string productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return null;

            var filter = Builders<Order>.Filter.ElemMatch(o => o.Items, i => i.ProductID == productId);
            var orders = await _order.Find(filter).ToListAsync();

            int totalOrders = orders.Count;
            int totalQuantity = 0;
            int totalRevenue = 0;

            int notConfirmOrders = 0;
            int pendingOrders = 0;
            int confirmedOrders = 0;
            int processingOrders = 0;
            int shippedOrders = 0;
            int deliveredOrders = 0;
            int cancelledOrders = 0;

            foreach (var order in orders)
            {
                var item = order.Items.FirstOrDefault(i => i.ProductID == productId);
                if (item != null)
                {
                    totalQuantity += item.Quantity;
                    totalRevenue += item.Quantity * item.UnitPrice;
                }

                switch (order.Status)
                {
                    case "NotConfirm": notConfirmOrders++; break;
                    case "Pending": pendingOrders++; break;
                    case "Confirmed": confirmedOrders++; break;
                    case "Processing": processingOrders++; break;
                    case "Shipped": shippedOrders++; break;
                    case "Delivered": deliveredOrders++; break;
                    case "Cancelled": cancelledOrders++; break;
                }
            }

            // Rating breakdown
            int r1 = product.Rating != null && product.Rating.ContainsKey("rate_1") ? product.Rating["rate_1"] : 0;
            int r2 = product.Rating != null && product.Rating.ContainsKey("rate_2") ? product.Rating["rate_2"] : 0;
            int r3 = product.Rating != null && product.Rating.ContainsKey("rate_3") ? product.Rating["rate_3"] : 0;
            int r4 = product.Rating != null && product.Rating.ContainsKey("rate_4") ? product.Rating["rate_4"] : 0;
            int r5 = product.Rating != null && product.Rating.ContainsKey("rate_5") ? product.Rating["rate_5"] : 0;

            double ratingAvg = CalculateAverageRating(product);

            var dto = new AdminProductOverviewDto
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                Category = product.Category,
                Price = product.Price,
                Color = product.Color ?? new List<string>(),
                Stock = product.Stock,
                Sold = deliveredOrders,

                RatingAverage = ratingAvg,
                Rating1 = r1,
                Rating2 = r2,
                Rating3 = r3,
                Rating4 = r4,
                Rating5 = r5,

                IsOnSale = product.Sale != null && product.Sale.IsActive,
                SalePercent = product.Sale?.Percent,
                SaleStart = product.Sale?.StartDate,
                SaleEnd = product.Sale?.EndDate,

                ImageURL = product.ImageURL ?? new List<string>(),

                TotalOrders = totalOrders,
                TotalQuantityOrdered = totalQuantity,
                TotalRevenue = totalRevenue,

                NotConfirmOrders = notConfirmOrders,
                PendingOrders = pendingOrders,
                ConfirmedOrders = confirmedOrders,
                ProcessingOrders = processingOrders,
                ShippedOrders = shippedOrders,
                DeliveredOrders = deliveredOrders,
                CancelledOrders = cancelledOrders
            };


            return dto;
        }

        public async Task<AdminProductStatsDto> GetProductStatsAsync()
        {
            var products = await _productRepository.GetAllAsync();

            var stats = new AdminProductStatsDto
            {
                TotalProducts = products.Count,
                TotalCategories = products.Select(p => p.Category).Distinct().Count(),
                TotalStock = products.Sum(p => p.Stock),
                TotalSold = products.Sum(p => p.Sold)
            };

            return stats;
        }

        public async Task<List<AdminTopCustomerDto>> GetTopCustomersByTotalSpentAsync(int top = 5)
        {
            if (top <= 0) top = 5;

            // Lấy tất cả order (hoặc có thể filter theo CreatedAt nếu muốn giới hạn thời gian)
            var orders = await _order.Find(_ => true).ToListAsync();

            var grouped = orders
                .GroupBy(o => o.UserID)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalOrders = g.Count(),
                    DeliveredOrders = g.Count(o => o.Status == "Delivered"),
                    CancelledOrders = g.Count(o => o.Status == "Cancelled"),
                    TotalSpent = g
                        .Where(o => o.Status == "Delivered")
                        .Sum(o => (decimal)o.TotalAmount)
                })
                .Where(x => x.TotalSpent > 0) // bỏ user chưa chi tiêu
                .OrderByDescending(x => x.TotalSpent)
                .Take(top)
                .ToList();

            var userIds = grouped.Select(x => x.UserId).ToList();

            // Lấy thông tin user từ SQL
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            // Lấy detail từ Mongo (UserDetail)
            var details = await _userDetail
                .Find(d => userIds.Contains(d.UserId))   // nếu property tên UserId thì sửa lại
                .ToListAsync();

            var result = (
                from g in grouped
                join u in users on g.UserId equals u.Id into uj
                from u in uj.DefaultIfEmpty()
                join d in details on g.UserId equals d.UserId into dj   // sửa UserID/UserId cho đúng model
                from d in dj.DefaultIfEmpty()
                select new AdminTopCustomerDto
                {
                    UserId = g.UserId,
                    Email = u?.Email,
                    Username = u?.Username,
                    Name = d?.Name,
                    Avatar = d?.Avatar,
                    TotalOrders = g.TotalOrders,
                    DeliveredOrders = g.DeliveredOrders,
                    CancelledOrders = g.CancelledOrders,
                    TotalSpent = g.TotalSpent
                })
                .OrderByDescending(x => x.TotalSpent)
                .ToList();

            return result;
        }


        public async Task<bool> DeleteProductAsync(string productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return false;

            await _productRepository.DeleteAsync(productId);
            return true;
        }

        public async Task<int> DeleteManyProductsAsync(IEnumerable<string> productIds)
        {
            if (productIds == null) return 0;

            var ids = productIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            if (ids.Count == 0) return 0;

            int deleted = 0;

            foreach (var id in ids)
            {
                var product = await _productRepository.GetByIdAsync(id);
                if (product == null) continue;

                await _productRepository.DeleteAsync(id);
                deleted++;
            }

            return deleted;
        }

        public async Task<bool> UpdateProductSaleAsync(string productId, SaleInfo saleInfo)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return false;

            await _productRepository.UpdateSaleAsync(productId, saleInfo);
            return true;
        }

        public async Task<List<Product>> GetActiveSaleProductsAsync()
        {
            return await _productRepository.GetActiveSalesAsync();
        }

        public async Task ApplyRandomSalesToProductsAsync(int numberOfProducts)
        {
            await _productRepository.ApplyRandomSalesAsync(numberOfProducts);
        }

        public async Task<Product> CreateProductAsync(CreateProductDto dto)
        {
            var rating = new Dictionary<string, int>
            {
                ["rate_1"] = 0,
                ["rate_2"] = 0,
                ["rate_3"] = 0,
                ["rate_4"] = 0,
                ["rate_5"] = 0
            };

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description ?? string.Empty,
                Price = dto.Price,
                Color = dto.Color ?? new List<string>(),
                Rating = rating,
                ImageURL = dto.ImageURL ?? new List<string>(),
                Detail = dto.Detail ?? new Dictionary<string, string>(),
                Category = dto.Category,
                Sold = 0,
                Stock = dto.Stock,
                Sale = dto.Sale ?? new SaleInfo
                {
                    Percent = 0.0,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow,
                    IsActive = false
                }
            };

            await _productRepository.AddAsync(product);

            return product;
        }

        public async Task<Product?> UpdateProductAsync(string productId, UpdateProductDto dto)
        {
            var existing = await _productRepository.GetByIdAsync(productId);
            if (existing == null)
                return null;

            existing.Name = dto.Name;
            existing.Description = dto.Description ?? string.Empty;
            existing.Price = dto.Price;
            existing.Color = dto.Color ?? new List<string>();
            existing.ImageURL = dto.ImageURL ?? new List<string>();
            existing.Detail = dto.Detail ?? new Dictionary<string, string>();
            existing.Category = dto.Category;
            existing.Stock = dto.Stock;

            if (dto.Sale != null)
            {
                existing.Sale = dto.Sale;
            }

            await _productRepository.UpdateAsync(productId, existing);
            return existing;
        }
        // GET: all sale events -> DTO
        public async Task<List<AdminSaleEventDto>> GetAllSaleEventsAsync()
        {
            var events = await _saleEvents
                .Find(Builders<ProductSaleEvent>.Filter.Empty)
                .SortBy(e => e.StartDate)
                .ToListAsync();

            return events.Select(e => new AdminSaleEventDto
            {
                Id = e.Id,
                Title = e.Title,
                Color = e.Color,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Percent = e.Percent,
                ProductIds = e.ProductIds ?? new List<string>()
            }).ToList();
        }

        // CREATE: nhận CreateAdminSaleEventDto, map sang entity, trả DTO trả về
        public async Task<AdminSaleEventDto> CreateSaleEventAsync(CreateAdminSaleEventDto dto)
        {
            var evt = new ProductSaleEvent
            {
                Title = dto.Title,
                Color = dto.Color,
                StartDate = dto.StartDate.ToUniversalTime(),
                EndDate = dto.EndDate.ToUniversalTime(),
                Percent = dto.Percent,      // đã là 0–1
                ProductIds = dto.ProductIds ?? new List<string>()
            };

            await _saleEvents.InsertOneAsync(evt);

            return new AdminSaleEventDto
            {
                Id = evt.Id,
                Title = evt.Title,
                Color = evt.Color,
                StartDate = evt.StartDate,
                EndDate = evt.EndDate,
                Percent = evt.Percent,
                ProductIds = evt.ProductIds
            };
        }

        // UPDATE: nhận UpdateAdminSaleEventDto, update các field
        public async Task<bool> UpdateSaleEventAsync(string id, UpdateAdminSaleEventDto dto)
        {
            var filter = Builders<ProductSaleEvent>.Filter.Eq(e => e.Id, id);

            var update = Builders<ProductSaleEvent>.Update
                .Set(e => e.Title, dto.Title)
                .Set(e => e.Color, dto.Color)
                .Set(e => e.StartDate, dto.StartDate.ToUniversalTime())
                .Set(e => e.EndDate, dto.EndDate.ToUniversalTime())
                .Set(e => e.Percent, dto.Percent) // 0–1
                .Set(e => e.ProductIds, dto.ProductIds ?? new List<string>());

            var result = await _saleEvents.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        // DELETE: có thể giữ nguyên (chỉ làm việc với entity)
        public async Task<long> DeleteSaleEventsAsync(IEnumerable<string> ids)
        {
            var distinctIds = ids?.Distinct().ToList() ?? new();
            if (distinctIds.Count == 0) return 0;

            var filter = Builders<ProductSaleEvent>.Filter.In(e => e.Id, distinctIds);
            var result = await _saleEvents.DeleteManyAsync(filter);
            return result.DeletedCount;
        }

        public async Task<List<AdminDailyOrderStatDto>> GetDailyOrderStatsAsync(int days)
        {
            if (days <= 0) days = 30;

            var fromDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            var orders = await _order
                .Find(o => o.CreatedAt >= fromDate)
                .ToListAsync();

            var grouped = orders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new AdminDailyOrderStatDto
                {
                    Date = g.Key,
                    TotalOrders = g.Count(),
                    DeliveredOrders = g.Count(o => o.Status == "Delivered"),
                    CancelledOrders = g.Count(o => o.Status == "Cancelled"),
                    Revenue = g
                        .Where(o => o.Status == "Delivered")
                        .Sum(o => (decimal)o.TotalAmount),
                })
                .OrderBy(x => x.Date)
                .ToList();

            return grouped;
        }

        // 2) Orders – status distribution (pie chart)
        public async Task<List<AdminOrderStatusDistributionDto>> GetOrderStatusDistributionAsync()
        {
            var orders = await _order.Find(_ => true).ToListAsync();

            var result = orders
                .GroupBy(o => o.Status ?? "Unknown")
                .Select(g => new AdminOrderStatusDistributionDto
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            return result;
        }

        // 3) Orders – payment methods (pie / bar chart)
        public async Task<List<AdminPaymentMethodStatDto>> GetPaymentMethodStatsAsync()
        {
            var orders = await _order.Find(_ => true).ToListAsync();

            var result = orders
                .GroupBy(o => string.IsNullOrEmpty(o.PaymentMethod) ? "Unknown" : o.PaymentMethod)
                .Select(g => new AdminPaymentMethodStatDto
                {
                    PaymentMethod = g.Key,
                    OrdersCount = g.Count(),
                    Revenue = g
                        .Where(o => o.Status == "Delivered")
                        .Sum(o => (decimal)o.TotalAmount)
                })
                .OrderByDescending(x => x.OrdersCount)
                .ToList();

            return result;
        }

        // 4) Products – category stats (bar chart)
        public async Task<List<AdminProductCategoryStatDto>> GetProductCategoryStatsAsync()
        {
            var products = await _product.Find(_ => true).ToListAsync();

            var result = products
                .GroupBy(p => p.Category ?? "Unknown")
                .Select(g => new AdminProductCategoryStatDto
                {
                    Category = g.Key,
                    ProductsCount = g.Count(),
                    TotalStock = g.Sum(p => p.Stock),
                    TotalSold = g.Sum(p => p.Sold)
                })
                .OrderByDescending(x => x.TotalSold)
                .ToList();

            return result;
        }

        // 5) Products – rating distribution (toàn shop, để vẽ bar 1–5 sao)
        public async Task<List<AdminRatingDistributionDto>> GetRatingDistributionAsync()
        {
            var products = await _product.Find(_ => true).ToListAsync();

            int GetCount(Product p, string key)
            {
                if (p.Rating == null) return 0;
                return p.Rating.TryGetValue(key, out var value) ? value : 0;
            }

            var rate1 = products.Sum(p => GetCount(p, "rate_1"));
            var rate2 = products.Sum(p => GetCount(p, "rate_2"));
            var rate3 = products.Sum(p => GetCount(p, "rate_3"));
            var rate4 = products.Sum(p => GetCount(p, "rate_4"));
            var rate5 = products.Sum(p => GetCount(p, "rate_5"));

            var list = new List<AdminRatingDistributionDto>
        {
            new AdminRatingDistributionDto { RatingValue = 1, Count = rate1 },
            new AdminRatingDistributionDto { RatingValue = 2, Count = rate2 },
            new AdminRatingDistributionDto { RatingValue = 3, Count = rate3 },
            new AdminRatingDistributionDto { RatingValue = 4, Count = rate4 },
            new AdminRatingDistributionDto { RatingValue = 5, Count = rate5 },
        };

            return list;
        }

        // 6) Users – demographics (gender + age buckets)
        public async Task<AdminDemographicsDto> GetUserDemographicsAsync()
        {
            var users = await _userDetail.Find(_ => true).ToListAsync();
            var result = new AdminDemographicsDto();

            // Gender
            var genderGroups = users
                .GroupBy(u => string.IsNullOrEmpty(u.Gender) ? "Unknown" : u.Gender)
                .Select(g => new AdminGenderDistributionDto
                {
                    Gender = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            result.Gender = genderGroups;

            // Age buckets
            var today = DateTime.UtcNow.Date;

            int? GetAge(DateTime? birthday)
            {
                if (birthday == null) return null;
                var b = birthday.Value.Date;
                var age = today.Year - b.Year;
                if (b > today.AddYears(-age)) age--;
                return age < 0 ? null : age;
            }

            string GetBucket(int age) =>
                age < 18 ? "<18"
                : age <= 24 ? "18–24"
                : age <= 34 ? "25–34"
                : age <= 44 ? "35–44"
                : age <= 54 ? "45–54"
                : "55+";

            var ageBuckets = users
                .Select(u => GetAge(u.Birthday)) // Birthday kiểu DateTime? trong UserDetail
                .Where(age => age.HasValue)
                .Select(age => GetBucket(age!.Value))
                .GroupBy(bucket => bucket)
                .Select(g => new AdminAgeBucketDto
                {
                    Range = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x =>
                {
                    // sắp xếp range
                    return x.Range switch
                    {
                        "<18" => 0,
                        "18–24" => 1,
                        "25–34" => 2,
                        "35–44" => 3,
                        "45–54" => 4,
                        "55+" => 5,
                        _ => 99
                    };
                })
                .ToList();

            result.Age = ageBuckets;

            return result;
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
