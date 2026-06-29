using ArenaDomain.Shared;
using System;
using System.Collections.Generic;

namespace ArenaDomain.Entities.Gym
{
    public class EquipmentCategory : BaseEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
    }
}
