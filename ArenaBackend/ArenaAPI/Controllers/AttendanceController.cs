// AttendanceController.cs
using ArenaApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpGet("member/{memberProfileId}")]
    public async Task<IActionResult> GetByMember(Guid memberProfileId)
    {
        var result = await _attendanceService.GetByMemberAsync(memberProfileId);
        return Ok(result);
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetToday()
    {
        var result = await _attendanceService.GetTodayAsync();
        return Ok(result);
    }
}