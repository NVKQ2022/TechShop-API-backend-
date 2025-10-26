namespace TechShop_API_backend_.DTOs.Review
{
    public class CreateReviewRequestDto
    {
        public string ProductId { get; set; }
        public int Stars { get; set; }
        public string Comment { get; set; }
        public List<IFormFile>? MediaFiles { get; set; }
    }
}
