using ArenaApplication.AI;
using ArenaApplication.AI.ArenaApplication.AI;
using ArenaApplication.Dtos.Booking;
using ArenaApplication.IServices;
using ArenaDomain.Entities;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;


namespace ArenaInfrastructure.AI
{
    public class BookingAIService : IBookingAIService
    {
        private readonly IBookingService _bookingService;
        private readonly IQRCodeService _qrService;
        private readonly IGenericRepository<UserSubscription, Guid> _subscriptionRepo;
        private readonly IGenericRepository<Booking, Guid> _bookingRepo;
        private readonly IGenericRepository<MemberProfile, Guid> _memberRepo;
        private readonly IUnitOfWork _unitOfWork;

        private readonly record struct QrCodeReply(string? Code)
        {
            public static QrCodeReply NotAvailableYet => new(null);
        }

        public BookingAIService(
            IBookingService bookingService,
            IQRCodeService qrService,
            IGenericRepository<UserSubscription, Guid> subscriptionRepo,
            IGenericRepository<Booking, Guid> bookingRepo,
            IGenericRepository<MemberProfile, Guid> memberRepo,
            IUnitOfWork unitOfWork)
        {
            _bookingService = bookingService;
            _qrService = qrService;
            _subscriptionRepo = subscriptionRepo;
            _bookingRepo = bookingRepo;
            _memberRepo = memberRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> HandleBookingRequestAsync(
            Guid memberProfileId,
            IntentResult intent,
            string userMessage,
            string memberName = "Member")
        {
            bool isArabic = userMessage.Any(c => c >= 0x0600 && c <= 0x06FF);

            // ✅ Get name from DB
            var profile = _memberRepo.GetAll()
                .FirstOrDefault(p => p.Id == memberProfileId
                                  || p.UserId == memberProfileId);

            var name = !string.IsNullOrEmpty(profile?.FirstName)
                ? profile.FirstName
                : memberName; // fallback to passed name

            var subscription = _subscriptionRepo.GetAll()
                .FirstOrDefault(s => s.MemberProfileId == memberProfileId
                                  && s.Status == SubscriptionStatus.Active
                                  && s.EndDate > DateTime.UtcNow);

            if (intent.Action == "cancel")
                return await HandleCancelAsync(memberProfileId, intent, isArabic, subscription, name);

            if (intent.Action == "reschedule")
                return await HandleRescheduleAsync(memberProfileId, intent, isArabic, name);

            return await HandleCreateAsync(memberProfileId, intent, isArabic, subscription, name);
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
            var localTime = DateTime.UtcNow.AddHours(3);

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

            if (bookingDate.Date < localTime.Date)
                return isArabic
                    ? "❌ مينفعش تحجز في الماضي."
                    : "❌ You can't book in the past.";

            if (bookingDate.Date == localTime.Date && startTime <= localTime.TimeOfDay)
                return isArabic
                    ? "❌ الوقت ده عدى خلاص النهارده. اختار وقت لسه جاي."
                    : "❌ That time has already passed today. Please choose a future time.";

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

            // Same-day sessions must be at least 5 hours apart.
            var sameDayStarts = _bookingRepo.GetAll()
                .Where(b => b.MemberProfileId == memberProfileId
                         && b.BookingDate.Date == bookingDate.Date
                         && b.Status != BookingStatus.Cancelled)
                .Select(b => b.StartTime)
                .ToList();

            foreach (var existingStart in sameDayStarts)
            {
                if (Math.Abs((startTime - existingStart).TotalHours) < 5)
                    return isArabic
                        ? $"❌ عندك حجز الساعة {existingStart:hh\\:mm} في نفس اليوم. لازم يكون بين الجلستين 5 ساعات على الأقل."
                        : $"❌ You already have a session at {existingStart:hh\\:mm} that day. Same-day sessions must be at least 5 hours apart.";
            }

            var createDto = new CreateBookingDto
            {
                MemberProfileId = memberProfileId,
                BookingDate = bookingDate,
                StartTime = startTime,
                EndTime = startTime.Add(TimeSpan.FromHours(1)),
                Source = BookingSource.Chatbot
            };

            var result = await _bookingService.CreateBooking(createDto);
            if (!result.IsSuccess)
                return $"❌ Failed to create booking: {string.Join(", ", result.Errors)}";

            QrCodeReply qrReply = bookingDate.Date == localTime.Date
                ? new QrCodeReply((await _qrService.GenerateAsync(result.Value.Id)).Code)
                : QrCodeReply.NotAvailableYet;


            await _subscriptionRepo.UpdateAsync(subscription);
            await _unitOfWork.SaveChangesAsync();

            return isArabic
     ? $"""
        ✅ تم تأكيد الحجز يا {memberName}!
        📅 التاريخ: {bookingDate:dddd, MMMM dd yyyy}
        ⏰ الوقت: {startTime:hh\:mm} — {startTime.Add(TimeSpan.FromHours(1)):hh\:mm}
        🎫 الجلسات المتبقية: {subscription.RemainingSessions}
        {FormatQrReply(qrReply, isArabic)}
        """
     : $"""
        ✅ Booking confirmed, {memberName}!
        📅 Date: {bookingDate:dddd, MMMM dd yyyy}
        ⏰ Time: {startTime:hh\:mm} — {startTime.Add(TimeSpan.FromHours(1)):hh\:mm}
        🎫 Remaining sessions: {subscription.RemainingSessions}
        {FormatQrReply(qrReply, isArabic)}
        """;
        }

        private static string FormatQrReply(QrCodeReply qrReply, bool isArabic)
        {
            if (!string.IsNullOrWhiteSpace(qrReply.Code))
                return $"🔑 QR Code: {qrReply.Code}";

            return isArabic
                ? "🔑 الـ QR سيظهر في نفس يوم الحجز فقط."
                : "🔑 QR code will be available only on the booking day.";
        }
    }
}