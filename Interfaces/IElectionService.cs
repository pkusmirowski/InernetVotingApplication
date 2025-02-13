using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InternetVotingApplication.Interfaces
{
    public interface IElectionService
    {
        DataWyborowViewModel GetAllElections();
        KandydatViewModel GetAllCandidates(int id);
        string AddVote(string user, int candidateId, int electionId);
        bool CheckIfElectionEnded(int electionId);
        bool CheckIfElectionStarted(int electionId);
        bool CheckElectionBlockchain(int electionId);
        Task<bool> CheckIfVoted(string user, int election);
        GlosowanieWyborczeViewModel SearchVote(string Text);
        GlosowanieWyborczeViewModel GetElectionResult(int id);
        int CountVotes(int election, int candidate);
        List<DataWyborow> ShowElectionByName();
    }
}
