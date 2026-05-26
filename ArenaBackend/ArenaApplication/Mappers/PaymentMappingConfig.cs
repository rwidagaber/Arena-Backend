// ArenaApplication/Mappers/PaymentMappingConfig.cs
using ArenaApplication.Dtos.Payment;
using ArenaDomain.Entities.Payments;
using Mapster;

namespace ArenaApplication.Mappers
{
    public class PaymentMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Payment, PaymentDto>()
                .Map(dest => dest.MemberName,
                     src => src.User != null
                         ? $"{src.User.FirstName} {src.User.LastName}"
                         : string.Empty)

                .Map(dest => dest.PlanName,
                     src => src.UserSubscription != null && src.UserSubscription.Plan != null
                         ? src.UserSubscription.Plan.NameEn
                         : string.Empty)

                .Map(dest => dest.PaymentMethod,
                     src => src.PaymentMethod.ToString())

                .Map(dest => dest.Status,
                     src => src.Status.ToString());
        }
    }
}