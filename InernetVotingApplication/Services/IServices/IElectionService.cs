using InernetVotingApplication.Models;
using InernetVotingApplication.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InernetVotingApplication.IServices
{
    public interface IElectionService
    {
        public DataWyborowViewModel GetAllElections();

        public KandydatViewModel GetAllCandidates(int id);

        public string AddVote(string user, int candidateId, int electionId);

        public bool CheckIfElectionEnded(int electionId);

        public bool CheckIfElectionStarted(int electionId);

        public bool CheckElectionBlockchain(int electionId);

        Task<bool> CheckIfVoted(string user, int election);

        public GlosowanieWyborczeViewModel SearchVote(string Text);

        public GlosowanieWyborczeViewModel GetElectionResult(int id);

        public int CountVotes(int election, int candidate);

        public List<DataWyborow> ShowElectionByName();
    }
}
