using InernetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
namespace InernetVotingApplication.Services
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
            var existingCandidate = await _context.Kandydats
                .FirstOrDefaultAsync(c => c.Imie == candidate.Imie && c.Nazwisko == candidate.Nazwisko && c.IdWybory == candidate.IdWybory);

            if (existingCandidate != null)
            {
                return false;
            }

            await _context.AddAsync(candidate);
            await _context.SaveChangesAsync();
            return true;
        }



        public async Task<bool> AddElectionAsync(DataWyborow dataWyborow)
        {
            bool electionExists = await _context.DataWyborows.AnyAsync(e => e.Opis == dataWyborow.Opis);
            if (electionExists)
            {
                return false;
            }

            await _context.AddAsync(dataWyborow);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
