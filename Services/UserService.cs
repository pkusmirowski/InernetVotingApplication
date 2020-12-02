using InernetVotingApplication.Blockchain;
using InernetVotingApplication.ExtensionMethods;
using InernetVotingApplication.Models;
using InernetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
                if (idnumber.NumerDowodu == user.NumerDowodu)
                {
                    return false;
                }
            }

            if (pesel != null)
            {
                if (pesel.Pesel == user.Pesel)
                {
                    return false;
                }
            }

            if (PESELValidation.IsValidPESEL(user.Pesel) == false || IDNumberValidation.ValidateIdNumber(user.NumerDowodu) == false)
            {
                return false;
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

        public string AddVote(string user, int candidateId, int electionId)
        {

            List<GlosowanieWyborcze> listOfPreviousElectionVotes = VerifyElectionBlockchain(electionId);
            if (listOfPreviousElectionVotes.Any(c => !c.JestPoprawny))
            {
                return "0";
            }

            var electionVoteDB = new GlosowanieWyborcze()
            {
                IdKandydat = candidateId,
                IdWybory = electionId,
                Glos = true
            };

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
            return electionVoteDB.Hash;
        }

        public bool CheckElectionDate(int electionId)
        {
            var electionDateQuery = from DataWyborow in _context.DataWyborows
                                    where electionId == DataWyborow.Id
                                    select DataWyborow.DataZakonczenia;
            DateTime electionDate = electionDateQuery.AsEnumerable().First();

            if (electionDate < DateTime.Now)
            {
                return false;
            }

            return true;
        }

        public bool CheckElectionBlockchain(int electionId)
        {
            return !VerifyElectionBlockchain(electionId).Any(c => !c.JestPoprawny);
        }

        private List<GlosowanieWyborcze> VerifyElectionBlockchain(int electionId)
        {
            var listOfRoleId = _context.GlosowanieWyborczes.Select(r => r.Id);
            var listOfPreviousElectionVotes = _context.GlosowanieWyborczes.Where(r => listOfRoleId.Contains(r.Id)).Where(r => r.IdWybory == electionId).ToList();

            BlockChainHelper.VerifyBlockChain(listOfPreviousElectionVotes);
            return listOfPreviousElectionVotes;
        }

        public async Task<bool> CheckIfVoted(string user, int election)
        {
            int userId = await (from Uzytkownik in _context.Uzytkowniks
                                where Uzytkownik.NumerDowodu == user
                                select Uzytkownik.Id).FirstOrDefaultAsync();

            bool ifVoted = await (from GlosUzytkownika in _context.GlosUzytkownikas
                                  where GlosUzytkownika.IdUzytkownik == userId && GlosUzytkownika.IdWybory == election
                                  select GlosUzytkownika.Glos).FirstOrDefaultAsync();
            return ifVoted;
        }


        public GlosowanieWyborczeViewModel SearchVote(string Text)
        {
            if (Text == "1")
            {
                var electionCandidates1 = _context.GlosowanieWyborczes.Select(x => new GlosowanieWyborczeItemViewModel
                {
                    IdKandydat = 0,
                    IdWybory = 0,
                    Hash = "0"
                }).FirstOrDefault();

                var vm1 = new GlosowanieWyborczeViewModel
                {
                    SearchCandidate = electionCandidates1
                };
                return vm1;
            }

            var electionCandidates = _context.GlosowanieWyborczes.Select(x => new GlosowanieWyborczeItemViewModel
            {
                IdKandydat = x.IdKandydat,
                IdWybory = x.IdWybory,
                Hash = x.Hash
            }).Where(x => x.Hash == Text);

            var candidate = GetCandidateInfo(electionCandidates);

            var vm = new GlosowanieWyborczeViewModel
            {
                SearchCandidate = candidate
            };

            return vm;

        }

        private GlosowanieWyborczeItemViewModel GetCandidateInfo(IQueryable<GlosowanieWyborczeItemViewModel> electionCandidates)
        {
            var candidate = electionCandidates.FirstOrDefault();

            int idKandydat = candidate.IdKandydat;
            int idWybory = candidate.IdWybory;

            string candidateName = (from Kandydat in _context.Kandydats
                                    where Kandydat.Id == idKandydat
                                    select Kandydat.Imie).FirstOrDefault();

            string candidateSurname = (from Kandydat in _context.Kandydats
                                       where Kandydat.Id == idKandydat
                                       select Kandydat.Nazwisko).FirstOrDefault();

            string electionDesc = (from DataWyborow in _context.DataWyborows
                                   where DataWyborow.Id == idWybory
                                   select DataWyborow.Opis).FirstOrDefault();

            candidate.CandidateName = candidateName;
            candidate.CandidateSurname = candidateSurname;
            candidate.ElectionDesc = electionDesc;
            return candidate;
        }
    }
}