using System.Text.Json.Serialization;

namespace ArenaApplication.Dtos.Payment
{
    public class PaymobWebhookDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("obj")]
        public PaymobTransactionObj Obj { get; set; } = new();
    }

    public class PaymobTransactionObj
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }          // TransactionId

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("pending")]
        public bool Pending { get; set; }

        [JsonPropertyName("order")]
        public PaymobWebhookOrder Order { get; set; } = new();

        [JsonPropertyName("data")]
        public PaymobWebhookData Data { get; set; } = new();
    }

    public class PaymobWebhookOrder
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }          // PaymentIntentId
    }

    public class PaymobWebhookData
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}