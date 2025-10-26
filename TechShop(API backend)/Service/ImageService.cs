using System.Text;
using Newtonsoft.Json;

namespace TechShop_API_backend_.Service
{
    public class ImageService
    {
        private readonly string _imageKitPrivateKey = Environment.GetEnvironmentVariable("ImageKit__PrivateKey")
        ?? throw new InvalidOperationException("Server email environment variable is not set.");
        private readonly string _imageKitPublicKey = Environment.GetEnvironmentVariable("ImageKit__PublicKey")
        ?? throw new InvalidOperationException("Server email environment variable is not set.");
        private readonly string _imageKitUrlEndpoint = Environment.GetEnvironmentVariable("ImageKit__UrlEndpoint")
        ?? throw new InvalidOperationException("Server email environment variable is not set.");

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file uploaded");
            }

            // Upload the image to ImageKit
            var imageUrl = await UploadToImageKit(file);

            return imageUrl;
        }

        private async Task<string> UploadToImageKit(IFormFile file)
        {
            using (var client = new HttpClient())
            {
                // Use PRIVATE KEY for authentication
                var credentials = $"{_imageKitPrivateKey}:";
                var base64Credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));
                client.DefaultRequestHeaders.Add("Authorization", "Basic " + base64Credentials);

                // Prepare form data with required fields
                var formData = new MultipartFormDataContent
        {
            { new StreamContent(file.OpenReadStream()), "file", file.FileName },
            { new StringContent(file.FileName), "fileName" } // ✅ REQUIRED
        };

                var response = await client.PostAsync("https://upload.imagekit.io/api/v1/files/upload", formData);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return result.url; // CDN URL
                }
                else
                {
                    throw new Exception($"Image upload failed: {responseContent}");
                }
            }
        }





    }
}
