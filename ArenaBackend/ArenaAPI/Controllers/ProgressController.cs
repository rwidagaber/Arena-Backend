using ArenaApplication.Dtos.ProgressLogDtos;
using ArenaApplication.IServices.IProgressServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArenaApi.Controllers
{

     [ApiController]
     [Route("api/progress")]
     [Authorize]
     public class ProgressController : ControllerBase
     {
         private readonly IProgressService _progressService;

         public ProgressController(IProgressService progressService)
         {
             _progressService = progressService;
         }

         [HttpGet]
         public async Task<IActionResult> GetProgress()
         {
             var memberProfileId = Guid.Parse(
                 User.FindFirstValue("memberProfileId")!);

             var result = await _progressService.GetProgressAsync(memberProfileId);
             if (!result.IsSuccess)
                 return NotFound(result.Errors);

             return Ok(result.Value);
         }

         [HttpPost]
         public async Task<IActionResult> LogProgress(CreateProgressLogDto dto)
         {
             var memberProfileId = Guid.Parse(
                 User.FindFirstValue("memberProfileId")!);

             var result = await _progressService
                 .LogProgressAsync(memberProfileId, dto);

             if (!result.IsSuccess)
                 return BadRequest(result.Errors);

             return CreatedAtAction(nameof(GetProgress), result.Value);
         }

         [HttpDelete("{logId}")]
         public async Task<IActionResult> DeleteLog(Guid logId)
         {
             var memberProfileId = Guid.Parse(
                 User.FindFirstValue("memberProfileId")!);

             var result = await _progressService
                 .DeleteLogAsync(memberProfileId, logId);

             if (!result.IsSuccess)
                 return BadRequest(result.Errors);

             return NoContent();
         }
        }
  }


