using MongoDB.Driver;
using Microsoft.Extensions.Options;
using TechShop_API_backend_.Models;
using TechShop_API_backend_.Interfaces;
using MongoDB.Bson;

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
public class ProductRepository
{
    private readonly IMongoCollection<Product> _products;

    public ProductRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _products = database.GetCollection<Product>(settings.Value.ProductCollectionName);
    }




    // Ensure all Product have sale Info
    public async Task EnsureAllProductsHaveSaleInfoAsync()
    {
        var filter = Builders<Product>.Filter.Eq(p => p.Sale, null);
        var productsWithoutSale = await _products.Find(filter).ToListAsync();

        if (productsWithoutSale.Count == 0)
        {
            Console.WriteLine("✅ All products already have SaleInfo.");
            return;
        }

        var defaultSale = new SaleInfo
        {
            Percent = 0.0,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            IsActive = false
        };

        foreach (var product in productsWithoutSale)
        {
            var update = Builders<Product>.Update.Set(p => p.Sale, defaultSale);
            await _products.UpdateOneAsync(p => p.ProductId == product.ProductId, update);
        }

        Console.WriteLine($"✅ Added default SaleInfo to {productsWithoutSale.Count} products.");
    }






    public async Task ApplyRandomSalesAsync(int numberOfProducts = 5)
    {
        // 1️⃣ Get all products
        var products = await _products.Find(_ => true).ToListAsync();
        if (products.Count == 0)
        {
            Console.WriteLine("⚠️ No products found in the database.");
            return;
        }

        // 2️⃣ Randomly choose N unique products
        var random = new Random();
        var selectedProducts = products.OrderBy(x => random.Next()).Take(numberOfProducts).ToList();

        // 3️⃣ Apply random sales
        foreach (var product in selectedProducts)
        {
            var percent = Math.Round(random.NextDouble() * 0.4 + 0.1, 2); // between 10%–50%
            var days = random.Next(3, 8); // sale lasts 3–7 days

            var saleInfo = new SaleInfo
            {
                Percent = percent,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(days),
                IsActive = true
            };

            var update = Builders<Product>.Update.Set(p => p.Sale, saleInfo);
            await _products.UpdateOneAsync(p => p.ProductId == product.ProductId, update);

            Console.WriteLine($"🎉 {product.Name} now has {percent * 100}% off for {days} days!");
        }

        Console.WriteLine($"✅ Applied random sales to {selectedProducts.Count} products.");
    }





    //  Get products currently on sale
    public async Task<List<Product>> GetActiveSalesAsync()
    {
        var now = DateTime.UtcNow;
        var filter = Builders<Product>.Filter.Where(p =>
            p.Sale != null &&
            p.Sale.IsActive &&
            p.Sale.StartDate <= now &&
            p.Sale.EndDate >= now);

        var products = await _products.Find(filter).ToListAsync();
        return products;
    }


    public async Task UpdateSaleAsync(string productId, SaleInfo saleInfo)
    {
        var update = Builders<Product>.Update.Set(p => p.Sale, saleInfo);
        await _products.UpdateOneAsync(p => p.ProductId == productId, update);
    }



    public async Task AddRandomStockToAllProductsAsync()
    {
        var random = new Random();

        // Get all products
        var products = await _products.Find(_ => true).ToListAsync();

        foreach (var product in products)
        {
            // Generate a random stock, e.g., between 10 and 100
            product.Stock = random.Next(10, 101);

            // Update the product in MongoDB
            var filter = Builders<Product>.Filter.Eq(p => p.ProductId, product.ProductId);
            var update = Builders<Product>.Update.Set(p => p.Stock, product.Stock);

            await _products.UpdateOneAsync(filter, update);
        }
    }


    public async Task<List<Product>> GetSuggestions(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            //return Ok(new List<ProductSuggestionDto>());
            return null;
        }
        var filter = Builders<Product>.Filter.Regex(p => p.Name, new BsonRegularExpression(new Regex(query, RegexOptions.IgnoreCase | RegexOptions.Multiline)));

        //Lấy tối đa 7 kết quả gợi ý
        var productsFromDb = await _products.Find(filter)
                                                     .Limit(7)
                                                     .Project(p => new Product
                                                     {
                                                         ProductId = p.ProductId,
                                                         Name = p.Name,
                                                         Price = p.Price,
                                                         ImageURL = p.ImageURL
                                                     })
                                                     .ToListAsync();
        return productsFromDb;
    }
    public async Task<List<Product>> GetAllAsync()
    {
        return await _products.Find(p => true).ToListAsync();
    }



    public async Task<List<Product>> GetRandomProductAsync(int number, List<string> categories)
    {
        // Create the base pipeline
        var pipeline = new List<BsonDocument>();

        // If categories is not null and not empty, filter by categories
        if (categories != null && categories.Any())
        {
            pipeline.Add(new BsonDocument
            {
                { "$match", new BsonDocument { { "Category", new BsonDocument { { "$in", new BsonArray(categories) } } } } }
            });
        }

        // Add the $sample stage to get random products (after any filtering)
        pipeline.Add(new BsonDocument
        {
            { "$sample", new BsonDocument { { "size", number } } }
        });

        // Run the aggregation with the constructed pipeline
        var result = await _products.AggregateAsync<Product>(pipeline);

        // Return the random products, either filtered by category or not
        return await result.ToListAsync();
    }


    public async Task<List<Product>> GetByIdsAsync(List<string> productIds)
    {
        if (productIds == null || productIds.Count == 0)
            return new List<Product>();

        // Filter for all products whose ProductId is in the list
        var filter = Builders<Product>.Filter.In(p => p.ProductId, productIds);

        var products = await _products.Find(filter).ToListAsync();
        return products;
    }


    public async Task<string?> GetCategoryByProductIdAsync(string productId)
    {
        var filter = Builders<Product>.Filter.Eq(p => p.ProductId, productId);
        var projection = Builders<Product>.Projection.Include(p => p.Category);

        var result = await _products
            .Find(filter)
            .Project<Product>(projection)
            .FirstOrDefaultAsync();

        return result?.Category;
    }

    public async Task<Product> GetByIdAsync(string id)
    {
        return await _products.Find(p => p.ProductId == id).FirstOrDefaultAsync();
    }

    public async Task<Product> GetByNameAsync(string name)
    {
        return await _products.Find(p => p.Name ==name).FirstOrDefaultAsync();
    }

    public async Task<List<Product>> GetByCategoryAsync(string category)
    {
        return await _products.Find(p => p.Category == category).ToListAsync();
    }

    public async Task<List<string>> GetAllCategoriesAsync()
    {
        return await _products.Distinct<string>("Category", FilterDefinition<Product>.Empty).ToListAsync();
    }


    public async Task AddAsync(Product product)
    {
        await _products.InsertOneAsync(product);
    }

    public async Task UpdateAsync(string id, Product updatedProduct)
    {
        await _products.ReplaceOneAsync(p => p.ProductId == id, updatedProduct);
    }

    public async Task DeleteAsync(string id)
    {
        await _products.DeleteOneAsync(p => p.ProductId == id);
    }


    public async Task IncrementProductRatingAsync(string productId, int stars)
    {
        if (stars < 1 || stars > 5)
            throw new ArgumentException("Stars must be between 1 and 5.");

        var filter = Builders<Product>.Filter.Eq(p => p.ProductId, productId);

        // Create the field name dynamically, e.g., "Rating.rate_4"
        var update = Builders<Product>.Update.Inc($"Rating.rate_{stars}", 1);

        await _products.UpdateOneAsync(filter, update);
    }




    public async Task<bool> DecreaseProductStockAsync(string productId, int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0.");

        var filter = Builders<Product>.Filter.And(
            Builders<Product>.Filter.Eq(p => p.ProductId, productId),
            Builders<Product>.Filter.Gte(p => p.Stock, amount) // ensure enough stock
        );

        var update = Builders<Product>.Update.Combine(
            Builders<Product>.Update.Inc(p => p.Stock, -amount),
            Builders<Product>.Update.Inc(p => p.Sold, amount) // optional: track sold quantity
        );

        var result = await _products.UpdateOneAsync(filter, update);

        return result.ModifiedCount > 0; // true if stock was decreased successfully
    }


    public async Task<bool> IncreaseProductStockAsync(string productId, int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0.");

        var filter = Builders<Product>.Filter.Eq(p => p.ProductId, productId);

        var update = Builders<Product>.Update.Inc(p => p.Stock, amount);

        var result = await _products.UpdateOneAsync(filter, update);

        return result.ModifiedCount > 0; // true if stock was increased successfully
    }



    public async Task ConvertKhoToStringAsync()
    {
        var collection = _products.Database.GetCollection<BsonDocument>(_products.CollectionNamespace.CollectionName);

        var pipeline = new EmptyPipelineDefinition<BsonDocument>()
            .AppendStage<BsonDocument, BsonDocument, BsonDocument>(
                new BsonDocument("$set", new BsonDocument("Detail.Kho", new BsonDocument("$toString", "$Detail.Kho")))
            );

        var filter = Builders<BsonDocument>.Filter.Exists("Detail.Kho");

        await collection.UpdateManyAsync(filter, pipeline);
    }
    public async Task EnsureColorIsArrayAsync()
    {
        var collection = _products.Database.GetCollection<BsonDocument>(_products.CollectionNamespace.CollectionName);

        var filter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists("Color", false), // Color does not exist
            Builders<BsonDocument>.Filter.Not(
                Builders<BsonDocument>.Filter.Type("Color", BsonType.Array) // Color is not an array
            )
        );

        var update = Builders<BsonDocument>.Update.Set("Color", new BsonArray());

        await collection.UpdateManyAsync(filter, update);
    }

    public async Task AddSoldFieldWithRandomValueAsync()
    {
        var collection = _products.Database.GetCollection<BsonDocument>(_products.CollectionNamespace.CollectionName);

        var allDocuments = await collection.Find(new BsonDocument()).ToListAsync();

        var random = new Random();

        var tasks = new List<Task>();

        foreach (var doc in allDocuments)
        {
            var id = doc["_id"];
            int soldValue = random.Next(0, 101); // 0 to 100 inclusive

            var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
            var update = Builders<BsonDocument>.Update.Set("Sold", soldValue);

            tasks.Add(collection.UpdateOneAsync(filter, update));
        }

        await Task.WhenAll(tasks);
    }
    public async Task<List<Product>> SearchAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return new List<Product>();

        // Regex chỉ áp dụng cho trường Name
        var regex = new BsonRegularExpression($".*{Regex.Escape(keyword)}.*", "i");

        var filter = Builders<Product>.Filter.Regex(p => p.Name, regex);

        return await _products.Find(filter).ToListAsync();
    }


    // Search sản phẩm - Phong
    public async Task<List<Product>> SearchByNameAsync(string query)
    {
        var filter = Builders<Product>.Filter.Regex(p => p.Name, new BsonRegularExpression(query, "i"));
        return await _products.Find(filter).ToListAsync();
    }

}
