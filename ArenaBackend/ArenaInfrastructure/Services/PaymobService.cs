using ArenaApplication.Dtos.Payment;
using ArenaApplication.IServices.Payment;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

namespace ArenaInfrastructure.Services
{
    public class PaymobService: IPaymentGatewayService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        private readonly string _apiKey;
        private readonly int _integrationId;
        private readonly int _iframeId;

        public PaymobService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;

            _apiKey = config["PaymobSettings:ApiKey"]!;
            _integrationId = int.Parse(config["PaymobSettings:IntegrationId"]!);
            _iframeId = int.Parse(config["PaymobSettings:IframeId"]!);
        }

        // ── Step 1: Get Auth Token ───────────────────────────────
        private async Task<string> GetAuthTokenAsync()
        {
            var response = await _httpClient.PostAsJsonAsync(
                "https://accept.paymob.com/api/auth/tokens",
                new { api_key = _apiKey });

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            return doc.RootElement.GetProperty("token").GetString()!;
        }

        // ── Step 2: Create Order ─────────────────────────────────
        private async Task<int> CreateOrderAsync(string authToken, decimal amount)
        {
            // PayMob بيشتغل بـ cents — نضرب في 100
            int amountCents = (int)(amount * 100);

            var response = await _httpClient.PostAsJsonAsync(
                "https://accept.paymob.com/api/ecommerce/orders",
                new
                {
                    auth_token = authToken,
                    delivery_needed = false,
                    amount_cents = amountCents,
                    currency = "EGP",
                    items = Array.Empty<object>()
                });

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            return doc.RootElement.GetProperty("id").GetInt32();
        }

        // ── Step 3: Get Payment Key ──────────────────────────────
        private async Task<string> GetPaymentKeyAsync(
            string authToken,
            int orderId,
            decimal amount,
            string userEmail,
            string userName)
        {
            int amountCents = (int)(amount * 100);

            var nameParts = userName.Split(' ');
            var firstName = nameParts.FirstOrDefault() ?? "User";
            var lastName = nameParts.LastOrDefault() ?? "User";

            var response = await _httpClient.PostAsJsonAsync(
                "https://accept.paymob.com/api/acceptance/payment_keys",
                new
                {
                    auth_token = authToken,
                    amount_cents = amountCents,
                    expiration = 3600,
                    order_id = orderId,
                    billing_data = new
                    {
                        email = userEmail,
                        first_name = firstName,
                        last_name = lastName,
                        phone_number = "N/A",
                        apartment = "N/A",
                        floor = "N/A",
                        street = "N/A",
                        building = "N/A",
                        shipping_method = "N/A",
                        postal_code = "N/A",
                        city = "N/A",
                        country = "N/A",
                        state = "N/A"
                    },
                    currency = "EGP",
                    integration_id = _integrationId
                });

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            return doc.RootElement.GetProperty("token").GetString()!;
        }

        // ── Main Method: كل الـ Steps في واحدة ──────────────────
        public async Task<PaymentGatewayResponse> GetIframeUrlAsync(
            decimal amount,
            string userEmail,
            string userName)
        {
            var authToken = await GetAuthTokenAsync();
            var orderId = await CreateOrderAsync(authToken, amount);
            var paymentKey = await GetPaymentKeyAsync(
                                 authToken, orderId, amount, userEmail, userName);

            return new PaymentGatewayResponse
            {
                IframeUrl =
        $"https://accept.paymob.com/api/acceptance/iframes/{_iframeId}?payment_token={paymentKey}",

                OrderId = orderId.ToString()
            };
        }

        // ── HMAC Verification ────────────────────────────────────
        private bool VerifyHmac(string data, string? receivedHmac)
        {
            var hmacSecret = _config["PaymobSettings:HmacSecret"]!;

            if (string.IsNullOrWhiteSpace(hmacSecret) || string.IsNullOrWhiteSpace(receivedHmac))
            {
                return false;
            }

            using var hmac = new System.Security.Cryptography.HMACSHA512(
                System.Text.Encoding.UTF8.GetBytes(hmacSecret));

            var hash = hmac.ComputeHash(
                System.Text.Encoding.UTF8.GetBytes(data));

            var computed = BitConverter.ToString(hash)
                .Replace("-", "")
                .ToLower();

            return string.Equals(
            computed,
            receivedHmac,
            StringComparison.OrdinalIgnoreCase);
        }

        public bool VerifyWebhookHmac(PaymobWebhookDto webhook, string receivedHmac)
        {
            var obj = webhook.Obj;

            // Paymob بيعمل concatenate للـ fields دي بالترتيب ده بالظبط
            var data = string.Concat(
                obj.AmountCents,
                obj.CreatedAt,
                obj.Currency,
                obj.ErrorOccured.ToString().ToLower(),
                obj.HasParentTransaction.ToString().ToLower(),
                obj.Id,
                obj.IntegrationId,
                obj.Is3dSecure.ToString().ToLower(),
                obj.IsAuth.ToString().ToLower(),
                obj.IsCapture.ToString().ToLower(),
                obj.IsRefunded.ToString().ToLower(),
                obj.IsStandalonePayment.ToString().ToLower(),
                obj.IsVoided.ToString().ToLower(),
                obj.Order.Id,
                obj.Owner,
                obj.Pending.ToString().ToLower(),
                obj.SourceData.Pan,
                obj.SourceData.SubType,
                obj.SourceData.Type,
                obj.Success.ToString().ToLower()
            );

            return VerifyHmac(data, receivedHmac);
        }
    }
}
