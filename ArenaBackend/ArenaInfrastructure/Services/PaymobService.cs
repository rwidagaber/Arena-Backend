using ArenaApplication.Dtos.Payment;
using ArenaApplication.IServices.Payment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace ArenaInfrastructure.Services
{
    public class PaymobService: IPaymentGatewayService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<PaymobService> _logger;

        private readonly string _apiKey;
        private readonly int _integrationId;
        private readonly int _iframeId;

        public PaymobService(HttpClient httpClient, IConfiguration config, ILogger<PaymobService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;

            _apiKey = config["PaymobSettings:ApiKey"]!;
            _integrationId = int.Parse(config["PaymobSettings:IntegrationId"]!);
            _iframeId = int.Parse(config["PaymobSettings:IframeId"]!);
        }

        // ── Step 1: Get Auth Token ───────────────────────────────
        private async Task<string> GetAuthTokenAsync()
        {
            _logger.LogInformation("Paymob Step 1: Requesting auth token from /api/auth/tokens");

            var response = await _httpClient.PostAsJsonAsync(
                "https://accept.paymob.com/api/auth/tokens",
                new { api_key = _apiKey });

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Paymob auth token request failed. StatusCode={StatusCode}, Response={Response}",
                    (int)response.StatusCode, json);
                throw new InvalidOperationException(
                    $"Paymob authentication failed (HTTP {(int)response.StatusCode}). Response: {json}");
            }

            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("token", out var tokenElement))
            {
                _logger.LogError(
                    "Paymob auth response does not contain 'token' property. Response={Response}", json);
                throw new InvalidOperationException(
                    $"Paymob auth response missing 'token'. Full response: {json}");
            }

            var token = tokenElement.GetString();
            _logger.LogInformation("Paymob Step 1 complete: Auth token obtained successfully.");
            return token!;
        }

        // ── Step 2: Create Order ─────────────────────────────────
        private async Task<int> CreateOrderAsync(string authToken, decimal amount)
        {
            // PayMob بيشتغل بـ cents — نضرب في 100
            int amountCents = (int)(amount * 100);

            _logger.LogInformation(
                "Paymob Step 2: Creating order. AmountCents={AmountCents}", amountCents);

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

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Paymob create order failed. StatusCode={StatusCode}, Response={Response}",
                    (int)response.StatusCode, json);
                throw new InvalidOperationException(
                    $"Paymob order creation failed (HTTP {(int)response.StatusCode}). Response: {json}");
            }

            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("id", out var idElement))
            {
                _logger.LogError(
                    "Paymob order response does not contain 'id' property. Response={Response}", json);
                throw new InvalidOperationException(
                    $"Paymob order response missing 'id'. Full response: {json}");
            }

            var orderId = idElement.GetInt32();
            _logger.LogInformation("Paymob Step 2 complete: Order created. OrderId={OrderId}", orderId);
            return orderId;
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

            _logger.LogInformation(
                "Paymob Step 3: Requesting payment key. OrderId={OrderId}, AmountCents={AmountCents}, IntegrationId={IntegrationId}",
                orderId, amountCents, _integrationId);

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
                    integration_id = _integrationId,
                    redirection_url = GetFrontendHomeUrl(
                        _config["EmailSettings:FrontendUrl"] ?? "http://localhost:4200") + "/checkout"
                });

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Paymob payment key request failed. StatusCode={StatusCode}, Response={Response}",
                    (int)response.StatusCode, json);
                throw new InvalidOperationException(
                    $"Paymob payment key request failed (HTTP {(int)response.StatusCode}). Response: {json}");
            }

            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("token", out var tokenElement))
            {
                _logger.LogError(
                    "Paymob payment key response does not contain 'token' property. Response={Response}", json);
                throw new InvalidOperationException(
                    $"Paymob payment key response missing 'token'. Full response: {json}");
            }

            var paymentKey = tokenElement.GetString();
            _logger.LogInformation("Paymob Step 3 complete: Payment key obtained successfully.");
            return paymentKey!;
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

        private static string GetFrontendHomeUrl(string frontendUrl)
        {
            var trimmedUrl = frontendUrl.TrimEnd('/');

            if (System.Uri.TryCreate(trimmedUrl, System.UriKind.Absolute, out var uri))
            {
                return uri.GetLeftPart(System.UriPartial.Authority).TrimEnd('/');
            }

            return trimmedUrl;
        }
    }
}
