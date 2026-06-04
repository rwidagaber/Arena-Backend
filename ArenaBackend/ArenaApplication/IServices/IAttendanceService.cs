using ArenaApplication.Dtos.AttendanceDtos;

namespace ArenaApplication.IServices
{
    public interface IAttendanceService
    {
        Task<AttendanceResponseDto> CreateAsync(CreateAttendanceDto dto);
        Task<List<AttendanceResponseDto>> GetByMemberAsync(Guid memberProfileId);
        Task<List<AttendanceResponseDto>> GetTodayAsync();
    }
}