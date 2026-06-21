using ArenaApplication.Dtos.Gym;
using ArenaDomain.Entities.Gym;
using Mapster;

namespace ArenaApplication.Mappers
{
    public class WorkingHoursMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<WorkingHours, WorkingHoursDto>()
                .Map(dest => dest.OpenTime, src => src.IsClosed ? (TimeSpan?)null : src.OpenTime)
                .Map(dest => dest.CloseTime, src => src.IsClosed ? (TimeSpan?)null : src.CloseTime);

            config.NewConfig<UpdateWorkingHoursDto, WorkingHours>()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.DayOfWeek)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.CreatedBy)
                .Ignore(dest => dest.UpdatedAt)
                .Ignore(dest => dest.UpdatedBy)
                .Ignore(dest => dest.DeletedAt)
                .Ignore(dest => dest.IsDeleted);
        }
    }
}

