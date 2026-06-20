using ArenaApplication.Dtos.SubscriptionPlanDtos;
using ArenaDomain.Entities.Subscription;
using Mapster;
using System.Globalization;

namespace ArenaApplication.Mappers
{
    public class SubscriptionPlanMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // SubscriptionPlan → SubscriptionPlanDto
            config.NewConfig<SubscriptionPlan, SubscriptionPlanDto>()
                .Map(dest => dest.Name,
                     src => CultureInfo.CurrentUICulture.Name.StartsWith("ar")
                         ? src.NameAr : src.NameEn)
                .Map(dest => dest.Description,
                     src => CultureInfo.CurrentUICulture.Name.StartsWith("ar")
                         ? src.DescriptionAr : src.DescriptionEn)
                .Map(dest => dest.NameEn, src => src.NameEn)
                .Map(dest => dest.NameAr, src => src.NameAr)
                .Map(dest => dest.DescriptionEn, src => src.DescriptionEn)
                .Map(dest => dest.DescriptionAr, src => src.DescriptionAr);
        }
    }
}
