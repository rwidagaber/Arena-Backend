using ArenaDomain.Shared;

namespace ArenaDomain.Entities.Gym
{
    public class GymSetting : BaseEntity<int>
    {
        public int NoShowThreshold { get; set; }
        public bool IsNoShowPenaltyEnabled { get; set; } = true;
    }
}
