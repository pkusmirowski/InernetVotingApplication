using InernetVotingApplication.Models;
using System.Threading.Tasks;

namespace InernetVotingApplication.IServices
{
    public interface IAdminService
    {
        Task<bool> AddCandidateAsync(Kandydat candidate);

        Task<bool> AddElectionAsync(DataWyborow dataWyborow);
    }
}
