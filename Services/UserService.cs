using InernetVotingApplication.Blockchain;
using InernetVotingApplication.Models;
using InernetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using BC = BCrypt.Net.BCrypt;

namespace InernetVotingApplication.Services
{
    public class UserService
    {
        private readonly InternetVotingContext _context;

        public UserService(InternetVotingContext context)
        {
            _context = context;
        }

        public async Task<bool> Register(Uzytkownik user)
        {
            var idnumber = await _context.Uzytkowniks.SingleOrDefaultAsync(x => x.NumerDowodu == user.NumerDowodu);
            var pesel = await _context.Uzytkowniks.SingleOrDefaultAsync(x => x.Pesel == user.Pesel);

            if (idnumber != null)
            {
                if (pesel != null)
                {
                    if (idnumber.NumerDowodu == user.NumerDowodu || idnumber.Pesel == user.Pesel)
                    {
                        return false;
                    }
                }
            }

            user.Haslo = BC.HashPassword(user.Haslo);
            user.JestAktywne = true;
            _context.Add(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LoginAsync(Logowanie user)
        {
            var queryActive = from Uzytkownik in _context.Uzytkowniks
                              where Uzytkownik.NumerDowodu == user.NumerDowodu
                              select Uzytkownik.JestAktywne;

            if (user.NumerDowodu != null && user.Haslo != null)
            {
                if (await queryActive.FirstOrDefaultAsync() ?? false)
                {
                    if (await AuthenticateUser(user))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public async Task<string> GetLoggedIdNumber(Logowanie user, string idNumber)
        {
            var queryName = from Uzytkownik in _context.Uzytkowniks
                            where Uzytkownik.NumerDowodu == user.NumerDowodu
                            select Uzytkownik.NumerDowodu;

            return idNumber = await queryName.FirstAsync();
        }

        public async Task<bool> AuthenticateUser(Logowanie user)
        {
            var account = await _context.Uzytkowniks.SingleOrDefaultAsync(x => x.NumerDowodu == user.NumerDowodu);

            return BC.Verify(user.Haslo, account.Haslo);
        }

        public DataWyborowViewModel GetAllElections()
        {
            var electionDates = _context.DataWyborows.Select(x => new DataWyborowItemViewModel
            {
                Id = x.Id,
                DataRozpoczecia = x.DataRozpoczecia,
                DataZakonczenia = x.DataZakonczenia,
                Opis = x.Opis

            });

            var vm = new DataWyborowViewModel
            {
                ElectionDates = electionDates
            };

            return vm;
        }

        public KandydatViewModel GetAllCandidates(int id)
        {

            var electionCandidates = _context.Kandydats.Select(x => new KandydatItemViewModel
            {
                Id = x.Id,
                Imie = x.Imie,
                Nazwisko = x.Nazwisko,
                IdWybory = x.IdWybory
            }).Where(x => x.IdWybory == id);

            var vm = new KandydatViewModel
            {
                ElectionCandidates = electionCandidates
            };

            return vm;
        }

        public bool AddVote(string user, int candidateId, int electionId)
        {
            ;

            var electionVoteDB = new GlosowanieWyborcze()
            {
                IdKandydat = candidateId,
                IdWybory = electionId,
                Glos = true
            };

            var listOfRoleId = _context.GlosowanieWyborczes.Select(r => r.Id);
            var listOfPreviousElectionVotes = _context.GlosowanieWyborczes.Where(r => listOfRoleId.Contains(r.Id)).Where(r => r.IdWybory == electionId).ToList();

            var queryActive = from GlosowanieWyborcze in _context.GlosowanieWyborczes
                              where GlosowanieWyborcze.IdWybory == electionId
                              orderby GlosowanieWyborcze.Id descending
                              select GlosowanieWyborcze.Id;

            BlockChainHelper.VerifyBlockChain(listOfPreviousElectionVotes);

            if (listOfPreviousElectionVotes.Any(c => !c.JestPoprawny))
            {
                throw new InvalidOperationException("Block Chain was invalid");
            }

            string previousBlockHash = null;
            if (listOfPreviousElectionVotes.Any())
            {
                var previousVote = listOfPreviousElectionVotes.Last();
                electionVoteDB.IdPoprzednie = previousVote.Id;
                previousBlockHash = previousVote.Hash;
            }

            var blockText = BlockHelper.VoteData(electionVoteDB.IdKandydat, electionVoteDB.IdWybory, electionVoteDB.Glos, previousBlockHash);
            electionVoteDB.Hash = HashHelper.Hash(blockText);

            var userId = (from Uzytkownik in _context.Uzytkowniks
                          where Uzytkownik.NumerDowodu == user
                          select Uzytkownik.Id).FirstOrDefault();

            var userVoiceDB = new GlosUzytkownika
            {
                IdUzytkownik = userId,
                IdWybory = electionId,
                Glos = true
            };

            _context.Add(electionVoteDB);
            _context.Add(userVoiceDB);
            _context.SaveChanges();
            return true;
        }

        public bool CheckIfVoted(string user, int election)
        {
            int userId = (from Uzytkownik in _context.Uzytkowniks
                          where Uzytkownik.NumerDowodu == user
                          select Uzytkownik.Id).FirstOrDefault();

            bool ifVoted = (from GlosUzytkownika in _context.GlosUzytkownikas
                            where GlosUzytkownika.IdUzytkownik == userId && GlosUzytkownika.IdWybory == election
                            select GlosUzytkownika.Glos).FirstOrDefault();
            return ifVoted;
        }
    }
}