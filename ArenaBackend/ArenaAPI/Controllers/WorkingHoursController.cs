using ArenaApplication.Dtos.Gym;
using ArenaApplication.Services.Gym;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ArenaApi.Controllers
{
    [ApiController]
    [Route("api/working-hours")]
    public class WorkingHoursController : ControllerBase
    {
        private readonly IWorkingHoursService _workingHoursService;

        public WorkingHoursController(IWorkingHoursService workingHoursService)
        {
            _workingHoursService = workingHoursService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkingHoursDto>>> GetWorkingHours(CancellationToken cancellationToken)
        {
            try
            {
                var workingHours = await _workingHoursService.GetWorkingHoursAsync(cancellationToken);
                return Ok(workingHours);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred retrieving working hours.", details = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<WorkingHoursDto>> UpdateWorkingHours(
            int id,
            [FromBody] UpdateWorkingHoursDto dto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _workingHoursService.UpdateWorkingHoursAsync(id, dto, cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred updating working hours.", details = ex.Message });
            }
        }
    }
}

