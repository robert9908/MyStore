using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace PaymentService.Services
{
    public class YooMoneyProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public YooMoneyProvider(IHttpClientFactory factory, IConfiguration config)
        {
            _httpClientFactory = factory;
            _configuration = config;
        }

        public async Task<string> CreatePaymentAsync(Guid internalPaymentId, decimal amount, string description)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://api.yookassa.ru/v3/");

            var shopId = _configuration["YooMoney:ShopId"];
            var secretKey = _configuration["YooMoney:SecretKey"];
            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{shopId}:{secretKey}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            var body = new
            {
                amount = new { value = amount.ToString("F2"), currency = "RUB" },
                capture = true,
                confirmation = new
                {
                    type = "redirect",
                    return_url = _configuration["YooMoney:ReturnUrl"]
                },
                metadata = new { paymentId = internalPaymentId },
                description
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("payments", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("confirmation").GetProperty("confirmation_url").GetString()!;
        }

        public async Task RefundPaymentAsync(Guid paymentId, decimal amount, string reason)
        {
            // Refund endpoint not yet integrated. Expose explicit behavior instead of leaving a stub.
            // Track implementation via issue tracker and feature flag.
            throw new NotSupportedException("YooMoney refund integration is not configured. Provide YooMoney:RefundEnabled=true and credentials, then implement API call.");
        }
    }
}
