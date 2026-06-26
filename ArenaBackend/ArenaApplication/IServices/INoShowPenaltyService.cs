using System.Threading;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface INoShowPenaltyService
    {
        Task ProcessNoShowPenaltiesAsync(CancellationToken cancellationToken = default);
    }
}
