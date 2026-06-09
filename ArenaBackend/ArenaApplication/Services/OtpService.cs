// Services/OtpService.cs
using ArenaApplication.IServices;
using Microsoft.Extensions.Caching.Memory;

public class OtpService : IOtpService
{
    private readonly IMemoryCache _cache;
    private const int OtpExpiryMinutes = 10;

    public OtpService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<string> GenerateAndSaveOtpAsync(Guid userId)
    {
        var otp = Random.Shared.Next(100000, 999999).ToString();

        var cacheKey = GetCacheKey(userId);
        var cooldownKey = GetCooldownKey(userId);

        var otpOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(OtpExpiryMinutes));

        var cooldownOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(60)); // ⬅️ resend delay

        _cache.Set(cacheKey, otp, otpOptions);
        _cache.Set(cooldownKey, true, cooldownOptions);

        return Task.FromResult(otp);
    }

    public Task<bool> ValidateOtpAsync(Guid userId, string otp)
    {
        var cacheKey = GetCacheKey(userId);

        if (!_cache.TryGetValue(cacheKey, out string? storedOtp))
            return Task.FromResult(false); // انتهت المدة أو مش موجود

        if (storedOtp != otp)
            return Task.FromResult(false); // OTP غلط

        _cache.Remove(cacheKey); // بنمسحه فور ما يتأكد ✓
        return Task.FromResult(true);
    }

    private static string GetCacheKey(Guid userId) => $"otp:{userId}";

    private static string GetCooldownKey(Guid userId) => $"otp_cooldown:{userId}";

    public Task<bool> CanResendOtpAsync(Guid userId)
    {
        var cacheKey = GetCacheKey(userId);
        var cooldownKey = GetCooldownKey(userId);

        // لو في cooldown → ممنوع resend
        if (_cache.TryGetValue(cooldownKey, out _))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

}