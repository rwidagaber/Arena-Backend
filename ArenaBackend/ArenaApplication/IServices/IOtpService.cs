using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices
{
    public interface IOtpService
    {
        Task<string> GenerateAndSaveOtpAsync(Guid userId);
        Task<bool> ValidateOtpAsync(Guid userId, string otp);
        public Task<bool> CanResendOtpAsync(Guid userId);

    }
}
