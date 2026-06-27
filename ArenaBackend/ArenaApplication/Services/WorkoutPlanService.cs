using ArenaApplication.Dtos.WorkoutPlan;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Workout;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaApplication.Services
{
    public class WorkoutPlanService : IWorkoutPlanService
    {
        private readonly IGenericRepository<WorkoutPlan, Guid> _workoutPlanRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public WorkoutPlanService(
            IGenericRepository<WorkoutPlan, Guid> workoutPlanRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _workoutPlanRepo = workoutPlanRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _localizer = localizer;
        }

        public async Task<Result<WorkoutPlanDto>> GetActiveWorkoutPlanByMemberIdAsync(Guid memberProfileId)
        {
            var plan = await _workoutPlanRepo.GetAll()
                .Include(wp => wp.WorkoutDays)
                    .ThenInclude(wd => wd.Exercises)
                        .ThenInclude(we => we.Exercise)
                .FirstOrDefaultAsync(wp => wp.MemberProfileId == memberProfileId && wp.IsActive && !wp.IsDeleted);

            if (plan == null)
            {
                return Result<WorkoutPlanDto>.Failure(_localizer["WorkoutPlanNotFound"]);
            }

            var dto = _mapper.Map<WorkoutPlanDto>(plan);
            return Result<WorkoutPlanDto>.Success(dto);
        }

        public async Task<Result<List<WorkoutPlanDto>>> GetWorkoutPlansByMemberIdAsync(Guid memberProfileId)
        {
            var plans = await _workoutPlanRepo.GetAll()
                .Include(wp => wp.WorkoutDays)
                    .ThenInclude(wd => wd.Exercises)
                        .ThenInclude(we => we.Exercise)
                .Where(wp => wp.MemberProfileId == memberProfileId && !wp.IsDeleted)
                .OrderBy(wp => wp.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<List<WorkoutPlanDto>>(plans);
            return Result<List<WorkoutPlanDto>>.Success(dtos);
        }

        public async Task<Result<WorkoutPlanDto>> GetWorkoutPlanByIdAsync(Guid id, Guid? memberProfileId = null)
        {
            var query = _workoutPlanRepo.GetAll()
                .Include(wp => wp.WorkoutDays)
                    .ThenInclude(wd => wd.Exercises)
                        .ThenInclude(we => we.Exercise)
                .Where(wp => wp.Id == id && !wp.IsDeleted);

            if (memberProfileId.HasValue)
            {
                query = query.Where(wp => wp.MemberProfileId == memberProfileId.Value);
            }

            var plan = await query.FirstOrDefaultAsync();

            if (plan == null)
            {
                return Result<WorkoutPlanDto>.Failure(_localizer["WorkoutPlanNotFound"]);
            }

            var dto = _mapper.Map<WorkoutPlanDto>(plan);
            return Result<WorkoutPlanDto>.Success(dto);
        }

        public async Task<Result<bool>> DeleteWorkoutPlanAsync(Guid id, Guid? memberProfileId = null)
        {
            var query = _workoutPlanRepo.GetAll()
                .Where(wp => wp.Id == id && !wp.IsDeleted);

            if (memberProfileId.HasValue)
            {
                query = query.Where(wp => wp.MemberProfileId == memberProfileId.Value);
            }

            var plan = await query.FirstOrDefaultAsync();

            if (plan == null)
            {
                return Result<bool>.Failure(_localizer["WorkoutPlanNotFound"]);
            }

            await _workoutPlanRepo.SoftDeleteAsync(plan);
            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
    }
}
