using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TechShop_API_backend_.Data.Context;
using TechShop_API_backend_.DTOs.Admin;
using TechShop_API_backend_.Models;

namespace TechShop_API_backend_.Data
{
    public class AdminRepository
    {
        private readonly AuthenticateDbContext _context;
        private readonly IMongoCollection<Order> _order;
        private readonly OrderRepository _orderRepository;
        private readonly ProductRepository _productRepository;

        public AdminRepository(AuthenticateDbContext context, IOptions<MongoDbSettings> settings, OrderRepository orderRepository, ProductRepository productRepository)
        {
            _context = context;
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _order = database.GetCollection<Order>(settings.Value.OrderCollectionName);
            _orderRepository = orderRepository;
            _productRepository = productRepository;
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
