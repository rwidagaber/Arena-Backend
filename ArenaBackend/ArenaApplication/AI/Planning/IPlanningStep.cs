using System.Threading.Tasks;

namespace ArenaApplication.AI.Planning
{
    public interface IPlanningStep
    {
        Task ExecuteAsync(PlanningContext context);
    }
}
