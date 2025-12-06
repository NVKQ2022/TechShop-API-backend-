using MongoDB.Driver;
using TechShop_API_backend_.Models;

namespace TechShop_API_backend_.Service
{
    public class RecommendationService
    {
        private readonly IMongoCollection<Order> _orders;

        public Dictionary<string, Dictionary<string, int>> SimilarityMatrix
            = new Dictionary<string, Dictionary<string, int>>();

        public RecommendationService(IMongoCollection<Order> orders)
        {
            _orders = orders;
            BuildMatrix();   // bạn có thể chạy mỗi lần start server hoặc dùng cache
        }

        public void BuildMatrix()
        {
            var orders = _orders.Find(_ => true).ToList();

            foreach (var order in orders)
            {
                var items = order.Items.Select(i => i.ProductID).Distinct().ToList();
                if (items.Count < 2) continue;

                for (int i = 0; i < items.Count; i++)
                {
                    for (int j = i + 1; j < items.Count; j++)
                    {
                        string a = items[i];
                        string b = items[j];

                        AddPair(a, b);
                        AddPair(b, a);
                    }
                }
            }
        }

        private void AddPair(string a, string b)
        {
            if (!SimilarityMatrix.ContainsKey(a))
                SimilarityMatrix[a] = new Dictionary<string, int>();

            if (!SimilarityMatrix[a].ContainsKey(b))
                SimilarityMatrix[a][b] = 0;

            SimilarityMatrix[a][b]++;
        }
        public List<string> Recommend(string productId, int topK = 5)
        {
            if (!SimilarityMatrix.ContainsKey(productId))
                return new List<string>();

            return SimilarityMatrix[productId]
                .OrderByDescending(pair => pair.Value)
                .Take(topK)
                .Select(pair => pair.Key)
                .ToList();
        }


        public List<Product> RecommendProducts(
                                                Product product,
                                                List<Product> allProducts,
                                                int topK = 5)
        {
            var ids = Recommend(product.ProductId, topK * 3); // lấy rộng hơn

            var related = allProducts
                .Where(p => ids.Contains(p.ProductId))
                .Where(p => p.Category == product.Category)  // lọc theo category
                .Take(topK)
                .ToList();

            return related;
        }

    }

}
