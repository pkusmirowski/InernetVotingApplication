using InernetVotingApplication.IServices;
using InernetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
namespace InernetVotingApplication.Services
{
    public class AdminService : IAdminService
    {
        private readonly InternetVotingContext _context;
        public AdminService(InternetVotingContext context)
        {
            _context = context;
        }

        public async Task<bool> AddCandidateAsync(Kandydat candidate)
        {
            string candidateName = await (from Kandydat in _context.Kandydats
                                          where Kandydat.Imie == candidate.Imie
                                          select Kandydat.Imie).FirstOrDefaultAsync();

            string candidateSurname = await (from Kandydat in _context.Kandydats
                                             where Kandydat.Nazwisko == candidate.Nazwisko
                                             select Kandydat.Nazwisko).FirstOrDefaultAsync();

            int candidateId = await (from Kandydat in _context.Kandydats
                                     where Kandydat.Nazwisko == candidate.Nazwisko
                                     select Kandydat.Id).FirstOrDefaultAsync();

            int candidateElectionId = await (from Kandydat in _context.Kandydats
                                             where Kandydat.IdWybory == candidate.IdWybory && Kandydat.Imie == candidateName && Kandydat.Nazwisko == candidateSurname
                                             select Kandydat.IdWybory).FirstOrDefaultAsync();

            //Sprawdzenie po imieniu i nazwisku
            //Powinien być np. PESEL kandydata aby dwóch kandydatów o
            //jendakowym imieniu i nazwisku mogło wziąć udział w wyborach
            //--> Sytuacja ekstremalna!!!! - to do
            if (candidateName == candidate.Imie && candidateSurname == candidate.Nazwisko && candidateElectionId == candidate.IdWybory)
            {
                return false;
            }

            //to FIX
            //if (_electionService.CheckIfElectionStarted(candidate.IdWybory) || !_electionService.CheckIfElectionEnded(candidate.IdWybory))
            //{
            //    return false;
            //}

            await _context.AddAsync(candidate);
            _context.SaveChanges();
            return true;
        }

        public async Task<bool> AddElectionAsync(DataWyborow dataWyborow)
        {
            string electionDescriptions = await (from DataWyborow in _context.DataWyborows
                                                 where DataWyborow.Opis == dataWyborow.Opis
                                                 select DataWyborow.Opis).FirstOrDefaultAsync();

            if (electionDescriptions == dataWyborow.Opis)
            {
                return false;
            }
            await _context.AddAsync(dataWyborow);
            _context.SaveChanges();
            return true;
        }
    }
}
