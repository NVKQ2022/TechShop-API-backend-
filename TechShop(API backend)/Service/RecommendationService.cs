using System.Collections.Concurrent;
using System.Threading.Tasks;
using TechShop_API_backend_.Data;
using TechShop_API_backend_.Models;

namespace TechShop_API_backend_.Service
{
    public  class RecommendationService
    {
        private static OrderRepository _orderRepository;
        private static ProductRepository _productRepository;


        // Lưu ma trận giống như trước
        public static ConcurrentDictionary<string, ConcurrentDictionary<string, int>> SimilarityMatrix
            = new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>();

        public RecommendationService(
            OrderRepository orderRepository,
            ProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;

            Console.WriteLine("🔥 RecommendationService constructor called!");
            BuildMatrix();
        }

        // ----------------------------------------------
        // BUILD CO-PURCHASE MATRIX
        // ----------------------------------------------
        public static async Task BuildMatrix()
        {
            SimilarityMatrix.Clear();

            var orders = await _orderRepository.GetAllOrdersAsync();
            Console.WriteLine($"🔍 Total orders: {orders.Count}");

            foreach (var order in orders)
            {
                // Print order info
                Console.WriteLine($"\n📦 OrderID: {order.OrderID}");

                var items = order.Items.Select(i => i.ProductID).Distinct().ToList();

                Console.WriteLine("🛒 Items in this order:");
                foreach (var id in items)
                {
                    Console.WriteLine($"   👉 ProductID: {id}");
                }

                if (items.Count < 2)
                {
                    Console.WriteLine("⚠️ Skipped (only 1 item)");
                    continue;
                }

                Console.WriteLine("✔ Creating pairs...");

                for (int i = 0; i < items.Count; i++)
                {
                    for (int j = i + 1; j < items.Count; j++)
                    {
                        Console.WriteLine($"   Pair + {items[i]} ↔ {items[j]}");

                        AddPair(items[i], items[j]);
                        AddPair(items[j], items[i]);
                    }
                }
            }

            Console.WriteLine("\n✅ Matrix Build Done");
            Console.WriteLine("📊 Matrix Content:");

            // Print full matrix
            foreach (var a in SimilarityMatrix)
            {
                Console.Write($"👉 {a.Key}: ");
                foreach (var b in a.Value)
                {
                    Console.Write($"{b.Key}({b.Value}) ");
                }
                Console.WriteLine();
            }
        }


        private static void AddPair(string a, string b)
        {
            if (!SimilarityMatrix.ContainsKey(a))
                SimilarityMatrix[a] = new ConcurrentDictionary<string, int>();

            if (!SimilarityMatrix[a].ContainsKey(b))
                SimilarityMatrix[a][b] = 0;

            SimilarityMatrix[a][b]++;
        }

        // ----------------------------------------------
        // Raw List<productId>
        // ----------------------------------------------
        public static List<string> RecommendProductIds(string productId, int limit = 5)
        {
            if (!SimilarityMatrix.ContainsKey(productId))
                return new List<string>();

            return SimilarityMatrix[productId]
                .OrderByDescending(x => x.Value)
                .Take(limit)
                .Select(x => x.Key)
                .ToList();
        }

        // ----------------------------------------------
        // Final output: List<Product>
        // ----------------------------------------------
        public static async Task<List<Product>> RecommendProducts(string productId, int limit = 5)
        {
            var ids = RecommendProductIds(productId, limit * 3);
            if (ids.Count == 0)
                return new List<Product>();

            var baseProduct = await _productRepository.GetByIdAsync(productId);
            if (baseProduct == null)
                return new List<Product>();

            // Lấy danh sách sản phẩm liên quan
            var result = new List<Product>();
            foreach (var id in ids)
            {
                var p = await _productRepository.GetByIdAsync(id);
                if (p == null) continue;

                // lọc theo category để tăng chất lượng
                if (p.Category == baseProduct.Category)
                {
                    result.Add(p);
                }

                if (result.Count == limit)
                    break;
            }

            return result;
        }

        // ----------------------------------------------
        // Gọi khi một Order mới được tạo
        // ----------------------------------------------
        public static void RefreshMatrix()
        {
            BuildMatrix();
        }
    }
}
