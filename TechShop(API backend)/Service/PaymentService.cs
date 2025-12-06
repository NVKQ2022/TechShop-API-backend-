using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using VNPAY;
namespace TechShop_API_backend_.Service
{
    public class PaymentService
    {

        public IConfiguration _config;  
        public PaymentService(IConfiguration configuration) 
        {
            _config = configuration;
        }

        public string CreateQrPaymentUrl(HttpContext context, int amount, string orderId)
        {
            string vnp_Returnurl = _config["VnPay:ReturnUrl"];
            string vnp_Url = _config["VnPay:BaseUrl"];
            string vnp_TmnCode = _config["VnPay:TmnCode"];
            string vnp_HashSecret = _config["VnPay:HashSecret"];

            var vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_PayMethod", "QR");   // 💥 Thêm cái này để tạo QR
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", (amount * 100).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", context.Connection.RemoteIpAddress.ToString());
            vnpay.AddRequestData("vnp_OrderInfo", "QR Payment");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", orderId);

            return vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
        }

    }











    public class VietQrRequest
    {
        public string bankCode { get; set; }
        public string bankAccount { get; set; }
        public string userBankName { get; set; }
        public string content { get; set; }
        public int qrType { get; set; }
        public long? amount { get; set; }
        public string orderId { get; set; }
        public string transType { get; set; }
        public string terminalCode { get; set; }
        public string serviceCode { get; set; }
        public string urlLink { get; set; }
        public string note { get; set; }
    }


    public class VnPayLibrary
    {
        private SortedList<string, string> requestData = new SortedList<string, string>();
        private SortedList<string, string> responseData = new SortedList<string, string>();

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                responseData.Add(key, value);
            }
        }

        public string CreateRequestUrl(string baseUrl, string secretKey)
        {
            var data = string.Join("&",
                requestData.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}")
            );

            string signData = string.Join("&", requestData.Select(kv => $"{kv.Key}={kv.Value}"));
            string hash = HmacSHA512(secretKey, signData);

            return $"{baseUrl}?{data}&vnp_SecureHash={hash}";
        }

        public bool ValidateSignature(string secureHash, string secretKey)
        {
            string signData = string.Join("&",
                responseData
                .Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                .Select(kv => $"{kv.Key}={kv.Value}")
            );

            string hash = HmacSHA512(secretKey, signData);

            return hash.Equals(secureHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private static string HmacSHA512(string key, string input)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

}
