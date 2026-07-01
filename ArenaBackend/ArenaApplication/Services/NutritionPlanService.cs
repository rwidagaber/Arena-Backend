using ArenaApplication.Dtos.Nutrition;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Nutrition;
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
    public class NutritionPlanService : INutritionPlanService
    {
        private readonly IGenericRepository<NutritionPlan, Guid> _nutritionPlanRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public NutritionPlanService(
            IGenericRepository<NutritionPlan, Guid> nutritionPlanRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _nutritionPlanRepo = nutritionPlanRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _localizer = localizer;
        }

        public async Task<Result<NutritionPlanResponseDto>> GetActiveNutritionPlanByMemberIdAsync(Guid memberProfileId)
        {
            var plan = await _nutritionPlanRepo.GetAll()
                .Include(np => np.Meals)
                .FirstOrDefaultAsync(np => np.MemberProfileId == memberProfileId && np.IsActive && !np.IsDeleted);

            if (plan == null)
            {
                return Result<NutritionPlanResponseDto>.Failure(_localizer["NutritionPlanNotFound"]);
            }

            var dto = _mapper.Map<NutritionPlanResponseDto>(plan);
            return Result<NutritionPlanResponseDto>.Success(dto);
        }

        public async Task<Result<List<NutritionPlanResponseDto>>> GetNutritionPlansByMemberIdAsync(Guid memberProfileId)
        {
            var plans = await _nutritionPlanRepo.GetAll()
                .Include(np => np.Meals)
                .Where(np => np.MemberProfileId == memberProfileId && !np.IsDeleted)
                .OrderBy(np => np.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<List<NutritionPlanResponseDto>>(plans);
            return Result<List<NutritionPlanResponseDto>>.Success(dtos);
        }

        public async Task<Result<NutritionPlanResponseDto>> GetNutritionPlanByIdAsync(Guid id, Guid? memberProfileId = null)
        {
            var query = _nutritionPlanRepo.GetAll()
                .Include(np => np.Meals)
                .Where(np => np.Id == id && !np.IsDeleted);

            if (memberProfileId.HasValue)
            {
                query = query.Where(np => np.MemberProfileId == memberProfileId.Value);
            }

            var plan = await query.FirstOrDefaultAsync();

            if (plan == null)
            {
                return Result<NutritionPlanResponseDto>.Failure(_localizer["NutritionPlanNotFound"]);
            }

            var dto = _mapper.Map<NutritionPlanResponseDto>(plan);
            return Result<NutritionPlanResponseDto>.Success(dto);
        }

        public async Task<Result<bool>> DeleteNutritionPlanAsync(Guid id, Guid? memberProfileId = null)
        {
            var query = _nutritionPlanRepo.GetAll()
                .Where(np => np.Id == id && !np.IsDeleted);

            if (memberProfileId.HasValue)
            {
                query = query.Where(np => np.MemberProfileId == memberProfileId.Value);
            }

            var plan = await query.FirstOrDefaultAsync();

            if (plan == null)
            {
                return Result<bool>.Failure(_localizer["NutritionPlanNotFound"]);
            }

            await _nutritionPlanRepo.SoftDeleteAsync(plan);
            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
    }
}
