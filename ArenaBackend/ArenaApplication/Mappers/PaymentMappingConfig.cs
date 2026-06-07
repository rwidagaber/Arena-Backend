// ArenaApplication/Mappers/PaymentMappingConfig.cs
using ArenaApplication.Dtos.Payment;
using ArenaDomain.Entities.Payments;
using Mapster;
using System.Globalization;

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

                .Map(dest => dest.MemberId,
                         src => src.UserId != null ? $"{src.UserId}" : string.Empty)

                .Map(dest => dest.PlanName,
                     src => src.UserSubscription != null && src.UserSubscription.Plan != null
                         ? (CultureInfo.CurrentUICulture.Name.StartsWith("ar")
                             ? src.UserSubscription.Plan.NameAr
                             : src.UserSubscription.Plan.NameEn)
                         : string.Empty)

                .Map(dest=>dest.PlanId,
                    src=>src.UserSubscription.PlanId)

                .Map(dest => dest.PaymentMethod,
                     src => src.PaymentMethod.ToString())

                .Map(dest => dest.Status,
                     src => src.Status.ToString())
                     
                .Map(dest => dest.SubscriptionEndDate,
                     src => src.UserSubscription != null ? src.UserSubscription.EndDate : (DateTime?)null)
                     
                .Map(dest => dest.SubscriptionStatus,
                     src => src.UserSubscription != null ? src.UserSubscription.Status.ToString() : null);
        }
    }
}
