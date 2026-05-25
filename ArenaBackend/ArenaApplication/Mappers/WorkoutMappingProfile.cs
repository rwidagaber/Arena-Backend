using AutoMapper;
using ArenaDomain.Entities.Workout;
using ArenaApplication.Dtos.WorkoutDtos;

namespace ArenaApplication.Mappers
{
    public class WorkoutMappingProfile : Profile
    {
        public WorkoutMappingProfile()
        {
            // WorkoutDay Mappings
            CreateMap<WorkoutDay, WorkoutDayDto>()
                .ForMember(dest => dest.Exercises, opt => opt.MapFrom(src => src.Exercises));

            CreateMap<WorkoutDayDto, WorkoutDay>()
                .ForMember(dest => dest.Exercises, opt => opt.Ignore());

            CreateMap<CreateWorkoutDayDto, WorkoutDay>();

            CreateMap<UpdateWorkoutDayDto, WorkoutDay>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.WorkoutPlanId, opt => opt.Ignore())
                .ForMember(dest => dest.WorkoutPlan, opt => opt.Ignore())
                .ForMember(dest => dest.Exercises, opt => opt.Ignore());

            // WorkoutExercise Mappings
            CreateMap<WorkoutExercise, WorkoutExerciseDto>()
                .ForMember(dest => dest.Exercise, opt => opt.MapFrom(src => src.Exercise));

            CreateMap<WorkoutExerciseDto, WorkoutExercise>()
                .ForMember(dest => dest.WorkoutDay, opt => opt.Ignore())
                .ForMember(dest => dest.Exercise, opt => opt.Ignore());

            CreateMap<CreateWorkoutExerciseDto, WorkoutExercise>();

            CreateMap<UpdateWorkoutExerciseDto, WorkoutExercise>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.WorkoutDayId, opt => opt.Ignore())
                .ForMember(dest => dest.ExerciseId, opt => opt.Ignore())
                .ForMember(dest => dest.WorkoutDay, opt => opt.Ignore())
                .ForMember(dest => dest.Exercise, opt => opt.Ignore());

            // Exercise Mappings
            CreateMap<Exercise, ExerciseDto>();

            CreateMap<ExerciseDto, Exercise>()
                .ForMember(dest => dest.MemberProfile, opt => opt.Ignore())
                .ForMember(dest => dest.WorkoutExercises, opt => opt.Ignore());

            CreateMap<CreateExerciseDto, Exercise>();

            CreateMap<UpdateExerciseDto, Exercise>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MemberProfileId, opt => opt.Ignore())
                .ForMember(dest => dest.MemberProfile, opt => opt.Ignore())
                .ForMember(dest => dest.WorkoutExercises, opt => opt.Ignore());
        }
    }
}
