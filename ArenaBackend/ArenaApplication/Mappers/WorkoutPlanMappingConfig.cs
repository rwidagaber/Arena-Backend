using ArenaApplication.Dtos.WorkoutDtos;
using ArenaApplication.Dtos.WorkoutPlan;
using ArenaDomain.Entities.Workout;
using Mapster;

namespace ArenaApplication.Mappers
{
    public class WorkoutPlanMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<WorkoutPlan, WorkoutPlanDto>()
                .Map(dest => dest.Days, src => src.WorkoutDays);

            config.NewConfig<WorkoutExercise, WorkoutExerciseDto>()
                .Map(dest => dest.Name, src => src.ExrciseName);
        }
    }
}
