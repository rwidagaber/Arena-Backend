using ArenaApplication.AI;
using ArenaApplication.AI.ArenaApplication.AI;
using ArenaApplication.Dtos.Booking;
using ArenaApplication.IServices;
using ArenaDomain.Entities;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using System.Text.RegularExpressions;


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
            // Validate date – allow "tomorrow" typo or Arabic "بكرة"
            if (string.IsNullOrWhiteSpace(intent.Date))
            {
                var raw = intent.RawMessage ?? string.Empty;
                if (Regex.IsMatch(raw, @"\\btomor\\w*\\b", RegexOptions.IgnoreCase) || Regex.IsMatch(raw, @"\\bبكرة\\b", RegexOptions.IgnoreCase))
                {
                    var tomorrow = DateTime.UtcNow.AddHours(3).AddDays(1);
                    intent.Date = tomorrow.ToString("yyyy-MM-dd");
                }
                else
                {
                    return isArabic ? "قولي تاريخ الحجز." : "Please tell me the booking date.";
                }
            }
            // Validate time
            if (string.IsNullOrWhiteSpace(intent.Time))
            {
                return isArabic ? "قولي وقت الحجز." : "Please provide the booking time.";
            }

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
        """;
        }

        private async Task<string> HandleRescheduleAsync(
            Guid memberProfileId,
            IntentResult intent,
            bool isArabic,
            string memberName = "Member")
        {
            // Validate date – allow "tomorrow" typo or Arabic "بكرة"
            if (string.IsNullOrWhiteSpace(intent.Date))
            {
                var raw = intent.RawMessage ?? string.Empty;
                if (Regex.IsMatch(raw, @"\btomor\w*\b", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(raw, @"\bبكرة\b", RegexOptions.IgnoreCase))
                {
                    var tomorrow = DateTime.UtcNow.AddHours(3).AddDays(1);
                    intent.Date = tomorrow.ToString("yyyy-MM-dd");
                }
                else
                {
                    return isArabic ? "قولي تاريخ الحجز." : "Please tell me the booking date.";
                }
            }

            // Validate time
            if (string.IsNullOrWhiteSpace(intent.Time))
            {
                return isArabic ? "قولي وقت الحجز." : "Please provide the booking time.";
            }

            if (!DateTime.TryParse(intent.Date, out var bookingDate))
                return isArabic ? "❌ التاريخ مش واضح." : "❌ Couldn't understand the date.";

            if (!TryGetRescheduleTimes(intent, out var oldStartTime, out var newStartTime))
                return isArabic
                    ? "قولي الوقت القديم والوقت الجديد. مثال: غير حجز بكرة من 17 إلى 18."
                    : "Please tell me the old time and the new time. Example: change tomorrow's booking from 17 to 18.";

            var localTime = DateTime.UtcNow.AddHours(3);
            UserSubscription? subscription = new UserSubscription();
            if (DateTime.MinValue == DateTime.MaxValue)
                return isArabic
                    ? "لازم يكون عندك اشتراك نشط قبل ما تحجز."
                    : "You need an active subscription before booking.";
            if (bookingDate.Date < localTime.Date)
                return isArabic
                    ? "❌ مينفعش تغير حجز في الماضي."
                    : "❌ You can't reschedule a past booking.";

            if (bookingDate.Date == localTime.Date && newStartTime <= localTime.TimeOfDay)
                return isArabic
                    ? "❌ الوقت ده عدى خلاص النهارده. اختار وقت لسه جاي."
                    : "❌ That time has already passed today. Please choose a future time.";

            Booking? booking = _bookingRepo.GetAll()
                .FirstOrDefault(b => b.MemberProfileId == memberProfileId
                                 && b.BookingDate.Date == bookingDate.Date
                                 && b.StartTime == oldStartTime
                                 && b.Status != BookingStatus.Cancelled);

            if (booking == null)
                return isArabic
                    ? $"❌ مفيش حجز يوم {bookingDate:dddd} الساعة {oldStartTime:hh\\:mm}."
                    : $"❌ No booking found on {bookingDate:dddd, MMMM dd yyyy} at {oldStartTime:hh\\:mm}.";

            var duplicate = _bookingRepo.GetAll()
                .FirstOrDefault(b =>
                    b.Id != booking.Id &&
                    b.MemberProfileId == memberProfileId &&
                    b.BookingDate.Date == bookingDate.Date &&
                    b.StartTime == newStartTime &&
                    b.Status != BookingStatus.Cancelled);

            if (duplicate != null)
                return isArabic
                    ? $"❌ عندك حجز بالفعل يوم {bookingDate:dddd} الساعة {newStartTime:hh\\:mm}."
                    : $"❌ You already have a booking on {bookingDate:dddd, MMMM dd yyyy} at {newStartTime:hh\\:mm}.";

            var updateDto = new UpdateBookingDto
            {
                Id = booking.Id,
                BookingDate = bookingDate,
                StartTime = newStartTime,
                EndTime = newStartTime.Add(TimeSpan.FromHours(1)),
                Status = BookingStatus.Confirmed
            };

            var result = await _bookingService.RescheduleBooking(booking.Id, updateDto);
            if (!result.IsSuccess)
                return isArabic
                    ? "❌ حصل مشكلة في تغيير الحجز."
                    : $"❌ Failed to reschedule: {string.Join(", ", result.Errors)}";

            return isArabic
                ? $"""
                   ✅ تم تغيير الحجز يا {memberName}!
                   📅 التاريخ: {bookingDate:dddd, MMMM dd yyyy}
                   ⏰ الوقت الجديد: {newStartTime:hh\:mm} — {newStartTime.Add(TimeSpan.FromHours(1)):hh\:mm}
                   """
                : $"""
                   ✅ Booking rescheduled, {memberName}!
                   📅 Date: {bookingDate:dddd, MMMM dd yyyy}
                   ⏰ New time: {newStartTime:hh\:mm} — {newStartTime.Add(TimeSpan.FromHours(1)):hh\:mm}
                   """;
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
                return "You need an active subscription before booking.";
            // Validate date
            if (string.IsNullOrWhiteSpace(intent.Date))
                return isArabic ? "قولي تاريخ الحجز." : "Please provide the booking date.";
            if (!DateTime.TryParse(intent.Date, out var bookingDate))
                return isArabic ? "❌ التاريخ مش واضح." : "❌ Couldn't understand the date.";
            // Validate time
            if (string.IsNullOrWhiteSpace(intent.Time))
                return isArabic ? "قولي وقت الحجز." : "Please provide the booking time.";
            if (!TimeSpan.TryParse(intent.Time, out var startTime))
                return isArabic ? "❌ الوقت مش واضح." : "❌ Couldn't understand the time.";
            // Past date check (use Egypt date, not server UTC date)
            if (bookingDate.Date < localTime.Date)
                return isArabic ? "❌ مينفعش تحجز في الماضي." : "❌ You can't book in the past.";
            // Past time today check
            if (bookingDate.Date == localTime.Date && startTime <= localTime.TimeOfDay)
                return isArabic ? "❌ الوقت ده عدى خلاص النهارده. اختار وقت لسه جاي." : "❌ That time has already passed today. Please choose a future time.";

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

        private static bool TryGetRescheduleTimes(IntentResult intent, out TimeSpan oldStartTime, out TimeSpan newStartTime)
        {
            oldStartTime = default;
            newStartTime = default;

            // First, attempt to use intent.Time if it already contains the old start time.
            if (!string.IsNullOrWhiteSpace(intent.Time) && TimeSpan.TryParse(intent.Time, out oldStartTime))
            {
                if (TryParseNewRescheduleTime(intent, out newStartTime))
                    return true;
            }

            // Fallback: extract two hour numbers from the raw message, supporting patterns like "from 14 to 12".
            var raw = intent.RawMessage ?? string.Empty;
            var match = Regex.Match(raw, @"\b(?<old>\d{1,2})\s*(?:to|-|–|—|\s+to\s+|\s+إلى\s+|\s+الى\s+)?\s*(?<new>\d{1,2})\b", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (int.TryParse(match.Groups["old"].Value, out var oldHour) &&
                    int.TryParse(match.Groups["new"].Value, out var newHour))
                {
                    oldStartTime = TimeSpan.FromHours(oldHour);
                    newStartTime = TimeSpan.FromHours(newHour);
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseNewRescheduleTime(IntentResult intent, out TimeSpan newStartTime)
        {
            newStartTime = default;
            var raw = intent.RawMessage ?? string.Empty;

            var match = Regex.Match(raw, @"\b(?:to|ل|الى|إلى)\s*(?<hour>\d{1,2})(?::(?<minute>\d{1,2}))?\s*(?<period>am|pm|ص|م)?\b", RegexOptions.IgnoreCase);
            if (!match.Success)
                return false;

            var hour = int.Parse(match.Groups["hour"].Value);
            var minute = match.Groups["minute"].Success
                ? int.Parse(match.Groups["minute"].Value)
                : 0;

            if (minute is < 0 or > 59)
                return false;

            var period = match.Groups["period"].Value.ToLowerInvariant();
            if ((period is "pm" or "م") && hour < 12)
                hour += 12;
            else if ((period is "am" or "ص") && hour == 12)
                hour = 0;
            else if (string.IsNullOrEmpty(period) && hour is >= 1 and <= 9)
                hour += 12;

            if (hour is < 0 or > 23)
                return false;

            newStartTime = new TimeSpan(hour, minute, 0);
            return true;
        }

        private sealed record QrCodeReply(string? Code)
        {
            public static QrCodeReply NotAvailableYet { get; } = new((string?)null);
        }
    }
}
