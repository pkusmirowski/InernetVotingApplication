using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace InternetVotingApplication.Services
{
    public class AdminService(InternetVotingContext context) : IAdminService
    {
        private readonly InternetVotingContext _context = context;

        public async Task<bool> AddCandidateAsync(Kandydat candidate)
        {
            if (await _context.Kandydats.AnyAsync(x => x.Imie == candidate.Imie && x.Nazwisko == candidate.Nazwisko))
            {
                return false;
            }

            await _context.Kandydats.AddAsync(candidate);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddElectionAsync(DataWyborow dataWyborow)
        {
            if (await _context.DataWyborows.AnyAsync(x => x.Opis == dataWyborow.Opis))
            {
                return false;
            }

            await _context.DataWyborows.AddAsync(dataWyborow);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
