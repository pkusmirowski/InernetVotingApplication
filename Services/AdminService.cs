using InternetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;
namespace InternetVotingApplication.Services
{
    public class AdminService
    {
        private readonly InternetVotingContext _context;
        public AdminService(InternetVotingContext context)
        {
            _context = context;
        }

        public async Task<bool> AddCandidateAsync(Kandydat candidate)
        {
            bool isCandidateExists = await _context.Kandydats
                .AnyAsync(x => x.Imie == candidate.Imie && x.Nazwisko == candidate.Nazwisko);

            if (isCandidateExists)
            {
                return false;
            }

            //if (await _electionService.CheckIfElectionStarted(candidate.IdWybory) || !_electionService.CheckIfElectionEnded(candidate.IdWybory))
            //{
            //    return false;
            //}

            await _context.AddAsync(candidate);
            await _context.SaveChangesAsync();
            return true;
        }



        public async Task<bool> AddElectionAsync(DataWyborow dataWyborow)
        {
            bool isElectionExists = await _context.DataWyborows
                .AnyAsync(x => x.Opis == dataWyborow.Opis);

            if (isElectionExists)
            {
                return false;
            }

            await _context.AddAsync(dataWyborow);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
