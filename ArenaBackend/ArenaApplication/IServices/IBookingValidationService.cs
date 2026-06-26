using ArenaDomain.Shared;
using System;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface IBookingValidationService
    {
        /// <summary>
        /// Validates that a proposed booking is at least 5 hours away from the
        /// member's other non-cancelled bookings on the same day. Returns a failure
        /// with a member-facing reason when the spacing rule is violated.
        /// </summary>
        Task<Result<bool>> ValidateSpacingAsync(Guid memberProfileId, DateTime date, TimeSpan startTime);
    }
}
