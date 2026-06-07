using ArenaApplication.AI;
using ArenaApplication.AI.ArenaApplication.AI;
using ArenaApplication.Dtos.Booking;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
//using ArenaDomain.Interfaces;

namespace ArenaInfrastructure.AI
{
    public class BookingAIService : IBookingAIService
    {
        private readonly IBookingService _bookingService;
        private readonly IQRCodeService _qrService;
        private readonly IGenericRepository<UserSubscription, Guid> _subscriptionRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Booking, Guid> _bookingRepo;

        public BookingAIService(
            IBookingService bookingService,
            IQRCodeService qrService,
            IGenericRepository<UserSubscription, Guid> subscriptionRepo,
            IUnitOfWork unitOfWork,
            IGenericRepository<Booking, Guid> bookingRepo)
        {
            _bookingService = bookingService;
            _qrService = qrService;
            _subscriptionRepo = subscriptionRepo;
            _unitOfWork = unitOfWork;
            _bookingRepo = bookingRepo;
        }

        //        public async Task<string> HandleBookingRequestAsync(
        //       Guid memberProfileId,
        //       IntentResult intent,
        //       string userMessage)
        //        {
        //            bool isArabic = userMessage.Any(c => c >= 0x0600 && c <= 0x06FF);

        //            var subscription = _subscriptionRepo.GetAll()
        //                .FirstOrDefault(s => s.MemberProfileId == memberProfileId
        //                                  && s.Status == SubscriptionStatus.Active
        //                                  && s.EndDate > DateTime.UtcNow);

        //            if (intent.Action == "cancel")
        //                return await HandleCancelAsync(memberProfileId, intent, isArabic, subscription);

        //            // ✅ Handle reschedule separately
        //            if (intent.Action == "reschedule")
        //                return await HandleRescheduleAsync(memberProfileId, intent, isArabic);

        //            // ✅ Default: create booking
        //            return await HandleCreateAsync(memberProfileId, intent, isArabic, subscription);

        //            // ✅ Load booking prompt with member context
        //            var bookingContext = PromptLoader.GetBookingPrompt(
        //                name: "Member",
        //                hasSubscription: subscription != null,
        //                remainingSessions: subscription?.RemainingSessions ?? 0,
        //                subscriptionExpiry: subscription?.EndDate.ToString("yyyy-MM-dd") ?? "N/A");


        //            if (subscription == null)
        //                return "❌ You need an active subscription to book a session. " +
        //                       "Please subscribe to a plan first.";

        //            if (subscription.RemainingSessions <= 0)
        //                return "❌ You have no remaining sessions. " +
        //                       "Please renew your subscription.";

        //            // Step 2 — Validate date and time
        //            if (intent.Date == null || intent.Time == null)
        //                return "I'd love to book a session for you! " +
        //                       "Please tell me the date and time. " +
        //                       "Example: 'Book tomorrow at 6 PM'";

        //            if (!DateTime.TryParse(intent.Date, out var bookingDate))
        //                return "❌ I couldn't understand the date. " +
        //                       "Please say something like 'tomorrow' or '2024-12-25'";

        //            if (!TimeSpan.TryParse(intent.Time, out var startTime))
        //                return "❌ I couldn't understand the time. " +
        //                       "Please say something like '6 PM' or '18:00'";

        //            if (bookingDate.Date < DateTime.Today)
        //                return isArabic
        //                    ? "❌ مينفعش تحجز في الماضي. اختار تاريخ في المستقبل."
        //                    : "❌ You can't book a session in the past. Please choose a future date.";

        //            // Step 2.5 — Check duplicate booking
        //            var existingBooking = _bookingRepo
        //                .GetAll()
        //                .FirstOrDefault(b =>
        //                    b.MemberProfileId == memberProfileId &&
        //                    b.BookingDate.Date == bookingDate.Date &&
        //                    b.StartTime == startTime &&
        //                    b.Status != BookingStatus.Cancelled);

        //            if (existingBooking != null)
        //            {
        //                return isArabic
        //                    ? $"❌ عندك حجز بالفعل يوم {bookingDate:dddd} الساعة {startTime:hh\\:mm}\nاختار وقت تاني."
        //                    : $"❌ You already have a booking on {bookingDate:dddd, MMMM dd yyyy} at {startTime:hh\\:mm}.\nPlease choose another time.";
        //            }

        //            // Step 3 — Create booking
        //            var createDto = new CreateBookingDto
        //            {
        //                MemberProfileId = memberProfileId,
        //                BookingDate = bookingDate,
        //                StartTime = startTime,
        //                EndTime = startTime.Add(TimeSpan.FromHours(1))
        //            };

        //            var result = await _bookingService.CreateBooking(createDto);

        //            if (!result.IsSuccess)
        //                return $"❌ Failed to create booking: {string.Join(", ", result.Errors)}";


        //            var qr = await _qrService.GenerateAsync(result.Value.Id);



        //            await _subscriptionRepo.UpdateAsync(subscription);
        //            await _unitOfWork.SaveChangesAsync();

        //            return $"""
        //    ✅ Booking confirmed!
        //    📅 Date: {bookingDate:dddd, MMMM dd yyyy}
        //    ⏰ Time: {startTime:hh\:mm} — {startTime.Add(TimeSpan.FromHours(1)):hh\:mm}
        //    🎫 Remaining sessions: {subscription.RemainingSessions}
        //    🔑 QR Code: {qr.Code}
        //    ⏳ QR expires at: {qr.ExpirationTime:hh\:mm}
        //    """;
        //        }
        //        }
        //}


        public async Task<string> HandleBookingRequestAsync(
    Guid memberProfileId,
    IntentResult intent,
    string userMessage,
    string memberName = "Member"
    )
        {
            bool isArabic = userMessage.Any(c => c >= 0x0600 && c <= 0x06FF);

            var subscription = _subscriptionRepo.GetAll()
                .FirstOrDefault(s => s.MemberProfileId == memberProfileId
                                  && s.Status == SubscriptionStatus.Active
                                  && s.EndDate > DateTime.UtcNow);

            // ✅ Handle cancel separately
            if (intent.Action == "cancel")
                return await HandleCancelAsync(memberProfileId, intent, isArabic, subscription);

            // ✅ Handle reschedule separately
            if (intent.Action == "reschedule")
                return await HandleRescheduleAsync(memberProfileId, intent, isArabic);

            // ✅ Default: create booking
            return await HandleCreateAsync(memberProfileId, intent, isArabic, subscription);
        }

        private async Task<string> HandleCancelAsync(
            Guid memberProfileId,
            IntentResult intent,
            bool isArabic,
            UserSubscription? subscription,
            string memberName = "Member")
        {
            if (intent.Date == null || intent.Time == null)
                return isArabic
                    ? "قولي تاريخ ووقت الحجز اللي عايز تلغيه."
                    : "Please tell me the date and time of the booking you want to cancel.";

            if (!DateTime.TryParse(intent.Date, out var bookingDate))
                return isArabic ? "❌ التاريخ مش واضح." : "❌ Couldn't understand the date.";

            if (!TimeSpan.TryParse(intent.Time, out var startTime))
                return isArabic ? "❌ الوقت مش واضح." : "❌ Couldn't understand the time.";

            var booking = _bookingRepo.GetAll()
                .FirstOrDefault(b =>
                    b.MemberProfileId == memberProfileId &&
                    b.BookingDate.Date == bookingDate.Date &&
                    b.StartTime == startTime &&
                    b.Status != BookingStatus.Cancelled);

            if (booking == null)
                return isArabic
                    ? $"❌ مفيش حجز يوم {bookingDate:dddd} الساعة {startTime:hh\\:mm}."
                    : $"❌ No booking found on {bookingDate:dddd, MMMM dd yyyy} at {startTime:hh\\:mm}.";

            // ✅ Cancel it
            var cancelResult = await _bookingService.CancelBooking(booking.Id);

            if (!cancelResult.IsSuccess)
                return isArabic
                    ? "❌ حصل مشكلة في الإلغاء."
                    : $"❌ Failed to cancel: {string.Join(", ", cancelResult.Errors)}";

            // ✅ Refund session
            if (subscription != null)
            {
               
                await _subscriptionRepo.UpdateAsync(subscription);
                await _unitOfWork.SaveChangesAsync();
            }

            return isArabic
     ? $"""
        ✅ تم إلغاء الحجز يا {memberName}!
        📅 كان يوم: {bookingDate:dddd} الساعة {startTime:hh\:mm}
        🎫 الجلسات المتبقية: {subscription?.RemainingSessions ?? 0}
        """
     : $"""
        ✅ Booking cancelled, {memberName}!
        📅 Was on: {bookingDate:dddd, MMMM dd yyyy} at {startTime:hh\:mm}
        🎫 Remaining sessions: {subscription?.RemainingSessions ?? 0}
        """; ;
        }

        private async Task<string> HandleRescheduleAsync(
            Guid memberProfileId,
            IntentResult intent,
            bool isArabic,
            string memberName = "Member")
        {
           
            return isArabic
                ? "عشان تغير الحجز، قولي الغي الحجز القديم وبعدين احجز وقت جديد."
                : "To reschedule, please cancel your existing booking first, then book a new time.";
        }

        private async Task<string> HandleCreateAsync(
            Guid memberProfileId,
            IntentResult intent,
            bool isArabic,
            UserSubscription? subscription,
            string memberName = "Member")
        {
            if (subscription == null)
                return isArabic
                    ? "❌ محتاج اشتراك نشط عشان تحجز."
                    : "❌ You need an active subscription to book a session.";

            if (subscription.RemainingSessions <= 0)
                return isArabic
                    ? "❌ خلصت جلساتك. جدد اشتراكك."
                    : "❌ You have no remaining sessions. Please renew your subscription.";

            if (intent.Date == null || intent.Time == null)
                return isArabic
                    ? "قولي التاريخ والوقت. مثال: احجزلي بكرة الساعة 6"
                    : "Please tell me the date and time. Example: 'Book tomorrow at 6 PM'";

            if (!DateTime.TryParse(intent.Date, out var bookingDate))
                return isArabic ? "❌ التاريخ مش واضح." : "❌ Couldn't understand the date.";

            if (!TimeSpan.TryParse(intent.Time, out var startTime))
                return isArabic ? "❌ الوقت مش واضح." : "❌ Couldn't understand the time.";

            if (bookingDate.Date < DateTime.Today)
                return isArabic
                    ? "❌ مينفعش تحجز في الماضي."
                    : "❌ You can't book in the past.";

            var existingBooking = _bookingRepo.GetAll()
                .FirstOrDefault(b =>
                    b.MemberProfileId == memberProfileId &&
                    b.BookingDate.Date == bookingDate.Date &&
                    b.StartTime == startTime &&
                    b.Status != BookingStatus.Cancelled);

            if (existingBooking != null)
                return isArabic
                    ? $"❌ عندك حجز بالفعل يوم {bookingDate:dddd} الساعة {startTime:hh\\:mm}.\nاختار وقت تاني."
                    : $"❌ You already have a booking on {bookingDate:dddd, MMMM dd yyyy} at {startTime:hh\\:mm}.\nPlease choose another time.";

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

            var qr = await _qrService.GenerateAsync(result.Value.Id);

           
            await _subscriptionRepo.UpdateAsync(subscription);
            await _unitOfWork.SaveChangesAsync();

            return isArabic
     ? $"""
        ✅ تم تأكيد الحجز يا {memberName}!
        📅 التاريخ: {bookingDate:dddd, MMMM dd yyyy}
        ⏰ الوقت: {startTime:hh\:mm} — {startTime.Add(TimeSpan.FromHours(1)):hh\:mm}
        🎫 الجلسات المتبقية: {subscription.RemainingSessions}
        🔑 QR Code: {qr.Code}
        """
     : $"""
        ✅ Booking confirmed, {memberName}!
        📅 Date: {bookingDate:dddd, MMMM dd yyyy}
        ⏰ Time: {startTime:hh\:mm} — {startTime.Add(TimeSpan.FromHours(1)):hh\:mm}
        🎫 Remaining sessions: {subscription.RemainingSessions}
        🔑 QR Code: {qr.Code}
        """;
        }
    }
}