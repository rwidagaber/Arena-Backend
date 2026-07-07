using System;

namespace ArenaInfrastructure.AI
{
    public class GoalRequiredException : Exception
    {
        public GoalRequiredException(string message) : base(message)
        {
        }
    }
}
