using ArenaApplication.Dtos.NotificationDtos;
using ArenaApplication.Dtos.Nutrition;
using ArenaDomain.Entities.Notifications;
using ArenaDomain.Entities.Nutrition;
using Mapster;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Mappers
{
    public class NotificationMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Notification, NotificationDto>();
        }
    }
}
