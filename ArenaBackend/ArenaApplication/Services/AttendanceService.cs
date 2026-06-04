using ArenaApplication.Dtos.AttendanceDtos;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Interfaces;

namespace ArenaInfrastructure.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IGenericRepository<Attendance, Guid> _attendanceRepo;
        private readonly IUnitOfWork _unitOfWork;

        public AttendanceService(
            IGenericRepository<Attendance, Guid> attendanceRepo,
            IUnitOfWork unitOfWork)
        {
            _attendanceRepo = attendanceRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<AttendanceResponseDto> CreateAsync(CreateAttendanceDto dto)
        {
            var attendance = new Attendance
            {
                BookingId = dto.BookingId,
                MemberProfileId = dto.MemberProfileId,
                CheckInTime = DateTime.UtcNow,
                ScannedById = dto.ScannedById
            };

            await _attendanceRepo.AddAsync(attendance);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(attendance);
        }

        public async Task<List<AttendanceResponseDto>> GetByMemberAsync(
            Guid memberProfileId)
        {
            var list = await _attendanceRepo.FindAsync(
                a => a.MemberProfileId == memberProfileId);

            return list.Select(MapToDto).ToList();
        }

        public async Task<List<AttendanceResponseDto>> GetTodayAsync()
        {
            var today = DateTime.UtcNow.Date;
            var list = await _attendanceRepo.FindAsync(
                a => a.CheckInTime.HasValue
                  && a.CheckInTime.Value.Date == today);

            return list.Select(MapToDto).ToList();
        }

        private static AttendanceResponseDto MapToDto(Attendance a)
        {
            return new AttendanceResponseDto
            {
                Id = a.Id,
                BookingId = a.BookingId,
                MemberProfileId = a.MemberProfileId,
                CheckInTime = a.CheckInTime,
                ScannedById = a.ScannedById
            };
        }
    }
}