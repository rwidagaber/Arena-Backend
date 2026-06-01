using ArenaApplication.AI;
using ArenaApplication.AI.ArenaApplication.AI;
using ArenaApplication.Dtos.Booking;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Enums;
using ArenaDomain.Interfacees;

namespace ArenaInfrastructure.AI
{
    public class BookingAIService : IBookingAIService
    {
        private readonly IBookingService _bookingService;
        private readonly IGenericRepository<UserSubscription, Guid> _subscriptionRepo;
        private readonly IUnitOfWork _unitOfWork;

        public BookingAIService(
            IBookingService bookingService,
            IGenericRepository<UserSubscription, Guid> subscriptionRepo,
            IUnitOfWork unitOfWork)
        {
            _bookingService = bookingService;
            _subscriptionRepo = subscriptionRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> HandleBookingRequestAsync(
            Guid memberProfileId,
            IntentResult intent,
            string userMessage)
        {
            // Step 1 — Check active subscription
            // ✅ GetAll() then FirstOrDefault — not FindAsync
            var subscription = _subscriptionRepo
                .GetAll()
                .FirstOrDefault(s => s.MemberProfileId == memberProfileId
                                  && s.Status == SubscriptionStatus.Active
                                  && s.EndDate > DateTime.UtcNow);

            if (subscription == null)
                return "❌ You need an active subscription to book a session. " +
                       "Please subscribe to a plan first.";

            if (subscription.RemainingSessions <= 0)
                return "❌ You have no remaining sessions. " +
                       "Please renew your subscription.";

            // Step 2 — Validate date and time
            if (intent.Date == null || intent.Time == null)
                return "I'd love to book a session for you! " +
                       "Please tell me the date and time. " +
                       "Example: 'Book tomorrow at 6 PM'";

            if (!DateTime.TryParse(intent.Date, out var bookingDate))
                return "❌ I couldn't understand the date. " +
                       "Please say something like 'tomorrow' or '2024-12-25'";

            if (!TimeSpan.TryParse(intent.Time, out var startTime))
                return "❌ I couldn't understand the time. " +
                       "Please say something like '6 PM' or '18:00'";

            if (bookingDate.Date < DateTime.UtcNow.Date)
                return "❌ You can't book a session in the past. " +
                       "Please choose a future date.";

            // Step 3 — Create booking
            var createDto = new CreateBookingDto
            {
                MemberProfileId = memberProfileId,
                BookingDate = bookingDate,
                StartTime = startTime,
                EndTime = startTime.Add(TimeSpan.FromHours(1))
            };

            var result = await _bookingService.CreateBooking(createDto);

            if (!result.IsSuccess)
                return $"❌ Failed to create booking: {string.Join(", ", result.Errors)}";

            // Step 4 — Deduct session and save
            subscription.RemainingSessions--;
            await _subscriptionRepo.UpdateAsync(subscription);
            await _unitOfWork.SaveChangesAsync();

            return $"""
                ✅ Booking confirmed!
                📅 Date: {bookingDate:dddd, MMMM dd yyyy}
                ⏰ Time: {startTime:hh\:mm} — {startTime.Add(TimeSpan.FromHours(1)):hh\:mm}
                🎫 Remaining sessions: {subscription.RemainingSessions}
                
                Your QR code will be generated shortly and available in your profile.
                """;
        }
    }
}