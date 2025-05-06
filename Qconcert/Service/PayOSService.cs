using System.Text.Json;
using System.Text;
using Qconcert.Models;

namespace Qconcert.Services
{
    public class PayOSService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _checksumKey;
        private readonly string _domain;
        private const string PAYOS_API_URL = "https://api.payos.vn/v1/payment-requests";

        public PayOSService(IConfiguration configuration, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _clientId = configuration["63257b6b-f334-4a2b-be90-ba0e48a7574f"];
            _apiKey = configuration["3478ac9b-4539-40d7-b3f1-79819c884caa"];
            _checksumKey = configuration["7b52c060c96c0e0cb4069e24a1058e08f9f0998e6030661ca3a40a454d026032"];
        }

        public async Task<string> CreatePaymentLink(Order order)
        {
            var items = order.OrderDetails.Select(od => new PaymentItem 
            {
                Name = od.Ticket.LoaiVe,
                Quantity = od.Quantity,
                Price = (int)od.Price
            }).ToList();

            var paymentRequest = new PaymentRequest
            {
                OrderCode = order.Id.ToString(),
                Amount = (int)order.TotalAmount,
                Description = $"Thanh toán đơn hàng #{order.Id}",
                Items = items,
                ReturnUrl = $"{_domain}/Payment/Success",
                CancelUrl = $"{_domain}/Payment/Cancel"
            };

            var json = JsonSerializer.Serialize(paymentRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Thêm headers
            _httpClient.DefaultRequestHeaders.Add("63257b6b-f334-4a2b-be90-ba0e48a7574f", _clientId);
            _httpClient.DefaultRequestHeaders.Add("3478ac9b-4539-40d7-b3f1-79819c884caa", _apiKey);

            // Tạo checksum
            var checksum = CreateChecksum(paymentRequest);
            _httpClient.DefaultRequestHeaders.Add("7b52c060c96c0e0cb4069e24a1058e08f9f0998e6030661ca3a40a454d026032", checksum);

            var response = await _httpClient.PostAsync(PAYOS_API_URL, content);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"PayOS API error: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var paymentResponse = JsonSerializer.Deserialize<PaymentResponse>(responseContent);
            return paymentResponse.CheckoutUrl;
        }

        private string CreateChecksum(PaymentRequest request)
        {
            var data = $"{request.OrderCode}|{request.Amount}|{request.Description}|{_checksumKey}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    public class PaymentItem
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
    }

    public class PaymentRequest
    {
        public string OrderCode { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; }
        public List<PaymentItem> Items { get; set; }
        public string ReturnUrl { get; set; }
        public string CancelUrl { get; set; }
    }

    public class PaymentResponse
    {
        public string CheckoutUrl { get; set; }
        public string OrderCode { get; set; }
        public string Status { get; set; }
    }
}