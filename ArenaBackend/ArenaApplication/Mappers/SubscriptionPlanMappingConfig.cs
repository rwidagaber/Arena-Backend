using ArenaApplication.Dtos.SubscriptionPlanDtos;
using ArenaDomain.Entities.Subscription;
using Mapster;

namespace ArenaApplication.Mappers
{
    public class SubscriptionPlanMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // SubscriptionPlan → SubscriptionPlanDto
            config.NewConfig<SubscriptionPlan, SubscriptionPlanDto>()
                .Map(dest => dest.Name, src => src.NameEn)
                .Map(dest => dest.Description, src => src.DescriptionEn);

            // SubscriptionPlanDto → SubscriptionPlan (for Create operations)
            config.NewConfig<SubscriptionPlanDto, SubscriptionPlan>()
                .Map(dest => dest.NameEn, src => src.Name)
                .Map(dest => dest.NameAr, src => src.Name)
                .Map(dest => dest.DescriptionEn, src => src.Description)
                .Map(dest => dest.DescriptionAr, src => src.Description);
        }
    }
}
