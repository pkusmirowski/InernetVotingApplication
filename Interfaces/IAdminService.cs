using InternetVotingApplication.Models;
using System.Threading.Tasks;

namespace InternetVotingApplication.Interfaces
{
    public interface IAdminService
    {
        Task<bool> AddCandidateAsync(Kandydat candidate);
        Task<bool> AddElectionAsync(DataWyborow dataWyborow);
    }
}
