using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using TechShop_API_backend_.Data;
using TechShop_API_backend_.Interfaces;
using TechShop_API_backend_.Models;
using TechShop_API_backend_.Helpers;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TechShop_API_backend_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {


        public readonly ProductRepository productRepository;
        public readonly UserDetailRepository userDetailRepository;

        private readonly IWebHostEnvironment _environment;
        private readonly ReviewRepository reviewRepository;
        private readonly ConverterHelper converterHelper = new ConverterHelper();
        public ProductController(ProductRepository productRepository, UserDetailRepository userDetailRepository, ReviewRepository reviewRepository, IWebHostEnvironment environment)
        {
            this.productRepository = productRepository;

            this.userDetailRepository = userDetailRepository;
            this.reviewRepository = reviewRepository;
            _environment = environment;
        }




        // GET: api/<ProductController>
        [AllowAnonymous]  // Allows anonymous users to access this endpoint
        [HttpGet("Fetch")]
        public async Task<IActionResult> GetListProducts(int number)
        {
            var isLogging = User.Identity.IsAuthenticated; // Check if the user is authenticated (logged in)
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            List<Product> products;

            if (isLogging)
            {
                // User is logged in, fetch products without any category filter
                products = await productRepository.GetRandomProductAsync(number, null);
            }
            else
            {
                // User is not logged in, fetch products with predefined categories for anonymous users
                List<string> categories = new List<string> { "Laptop", "Drones" }; // Example categories for anonymous users
                products = await productRepository.GetRandomProductAsync(number, categories);
            }

            // Convert the list of products into the required format (e.g., a zip list or any transformation)
            var productZip = converterHelper.ConvertProductListToProductZipList(products);

            return Ok(productZip); // Return the transformed list to the client
        }


        // GET: api/<ProductController>
        [AllowAnonymous]  // Allows anonymous users to access this endpoint
        [HttpGet("Fetch/{category}/{number}")]
        public async Task<IActionResult> GetListProductsƯithCategory(int number, string category)
        {
            

            List<Product> products;

            products = await productRepository.GetByCategoryAsync(category);
           

            // Convert the list of products into the required format (e.g., a zip list or any transformation)
            var productZip = converterHelper.ConvertProductListToProductZipList(products);

            return Ok(productZip); // Return the transformed list to the client
        }





        [AllowAnonymous]
        [HttpGet("Search/{keyword}")]
        public async Task<IActionResult> Search(string keyword)
        {
            ConverterHelper converterHelper = new ConverterHelper();

            // Fetch search results based on the keyword
            var searchResults = await productRepository.SearchAsync(keyword);

            // Convert product list to Product_zip list
            List<Product_zip> product_Zips = converterHelper.ConvertProductListToProductZipList(searchResults);
            return Ok(product_Zips);
        }







        // GET api/<ProductController>/Details/5
        [AllowAnonymous]
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> GetDetails(string id)
        {
            try
            {
                var product = await productRepository.GetByIdAsync(id.ToString());

                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                return Ok(product); // Automatically returns product as JSON with 200 OK status
            }
            catch (Exception ex)
            {
                // Log the exception (or handle it as needed)
                return StatusCode(500, new { message = "An error occurred while processing your request", error = ex.Message });
            }
        }


        // POST api/<ProductController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<ProductController>/5
        [HttpPut("{id}")]
        public void Update(int id, [FromBody] string value)
        {

        }

        // DELETE api/<ProductController>/Delete/5
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var product = await productRepository.GetByIdAsync(id.ToString());

                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }


                await productRepository.DeleteAsync(id.ToString());
                return (Ok(" the product has been deleted"));

                
            }
            catch (Exception ex)
            {
                // Log the exception (or handle it as needed)
                return StatusCode(500, new { message = "An error occurred while processing your request", error = ex.Message });
            }
        }
    }
}
