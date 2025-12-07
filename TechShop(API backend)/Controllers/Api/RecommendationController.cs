using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechShop_API_backend_.Helpers;
using TechShop_API_backend_.Service;

namespace TechShop_API_backend_.Controllers.Api
{
    [ApiController]
    [Route("api/recommend")]
    public class RecommendationController : ControllerBase
    {
        private readonly RecommendationService _service;
        private readonly ILogger _logger;
        private readonly ProductRepository _productRepository;
        ConverterHelper _converter;
        public RecommendationController(RecommendationService service, ProductRepository productRepository, ConverterHelper converterHelper)
        {
            _service = service;
            _converter = converterHelper;
            _productRepository = productRepository;
        }

        // ----------------------------------------------------
        // 1. Recommend by Product
        // ----------------------------------------------------
        [AllowAnonymous]
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> RecommendByProduct(string productId, [FromQuery] int limit = 5)
        {
            var productIds = RecommendationService.RecommendProductIds(productId, limit);

            var products = await _productRepository.GetByIdsAsync(productIds);


            Console.WriteLine("=== Recommended Products ===");
            foreach (var p in products)
            {
                Console.WriteLine($"{p.ProductId} - {p.Name}");
            }
            if (products.Count > 0)
            {
                var products_zip = _converter.ConvertProductListToProductZipList(products);
                return Ok(products_zip);
            }
            else
            {
                return NotFound(new { message = "No recommendations found" });
            }
        }

        // ----------------------------------------------------
        // 2. Recommend IDs only (useful for debugging)
        // ----------------------------------------------------
        [AllowAnonymous]
        [HttpGet("ids/{productId}")]
        public async Task<IActionResult> RecommendIds(string productId, [FromQuery] int limit = 5)
        {
            var ids =  RecommendationService.RecommendProductIds(productId, limit);

            Console.WriteLine("=== Recommended IDs ===");
            foreach (var id in ids)
            {
                Console.WriteLine(id);
            }
            if (ids.Count == 0)
            {
                return NotFound(new { message = "No recommendations found" });
            }
            return Ok(ids);
        }

        // ----------------------------------------------------
        // 3. Force rebuild matrix manually (admin use)
        // ----------------------------------------------------
        [AllowAnonymous]
        [HttpPost("rebuild")]
        public IActionResult RebuildMatrix()
        {
            RecommendationService.RefreshMatrix();
            return Ok(new { message = "Matrix rebuilt successfully" });
        }

        [AllowAnonymous]
        [HttpPost("build")]
        public async Task<IActionResult> BuildMatrix()
        {
            await RecommendationService.BuildMatrix();
            return Ok(new { message = "Matrix built successfully" });
        }

        [HttpGet("matrix")]
        public IActionResult GetMatrix()
        {
            return Ok(RecommendationService.SimilarityMatrix);
        }

    }
}
