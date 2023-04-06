using InernetVotingApplication.Blockchain;
using InernetVotingApplication.ExtensionMethods;
using InernetVotingApplication.Models;
using InernetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace InernetVotingApplication.Services
{
    public class ElectionService
    {
        private readonly InternetVotingContext _context;

        public ElectionService(InternetVotingContext context)
        {
            _context = context;
        }

        public DataWyborowViewModel GetAllElections()
        {
            var electionDates = _context.DataWyborows
                .Select(x => new DataWyborowItemViewModel
                {
                    Id = x.Id,
                    DataRozpoczecia = x.DataRozpoczecia,
                    DataZakonczenia = x.DataZakonczenia,
                    Opis = x.Opis,
                    Type = CheckElectionType(x.Id)
                })
                .ToList();

            return new DataWyborowViewModel
            {
                ElectionDates = electionDates
            };
        }

        private int CheckElectionType(int electionId)
        {
            if (!CheckIfElectionEnded(electionId))
            {
                return 1;
            }
            else if (!CheckIfElectionStarted(electionId))
            {
                return 2;
            }
            else
            {
                return 3;
            }
        }


        public KandydatViewModel GetAllCandidates(int id)
        {
            var electionCandidates = _context.Kandydats
                .Where(x => x.IdWybory == id)
                .Select(x => new KandydatItemViewModel
                {
                    Id = x.Id,
                    Imie = x.Imie,
                    Nazwisko = x.Nazwisko,
                    IdWybory = x.IdWybory
                })
                .ToList();

            return new KandydatViewModel
            {
                ElectionCandidates = electionCandidates
            };
        }

        public async Task<string> AddVoteAsync(string user, int candidateId, int electionId)
        {
            var listOfPreviousElectionVotes = VerifyElectionBlockchain(electionId);
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
            if (listOfPreviousElectionVotes.Count > 0)
            {
                var previousVote = listOfPreviousElectionVotes.Last();
                electionVoteDB.IdPoprzednie = previousVote.Id;
                previousBlockHash = previousVote.Hash;
            }

            var blockText = BlockHelper.VoteData(electionVoteDB.IdKandydat, electionVoteDB.IdWybory, electionVoteDB.Glos, previousBlockHash);
            electionVoteDB.Hash = HashHelper.Hash(blockText);

            var userId = await _context.Uzytkowniks
                .Where(u => u.Email == user)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var userEmail = await _context.Uzytkowniks
                .Where(u => u.Email == user)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            var userVoiceDB = new GlosUzytkownika
            {
                IdUzytkownik = userId,
                IdWybory = electionId,
                Glos = true
            };

            await _context.AddAsync(electionVoteDB);
            await _context.AddAsync(userVoiceDB);
            await _context.SaveChangesAsync();

            // Wywołanie metody SendEmailVoteHashAsync i oczekiwanie na zakończenie działania tej metody
            await Email.SendEmailVoteHashAsync(electionVoteDB, userEmail);

            return electionVoteDB.Hash;
        }



        public bool CheckIfElectionEnded(int electionId)
        {
            var electionDate = _context.DataWyborows
                .Where(x => x.Id == electionId)
                .Select(x => x.DataZakonczenia)
                .FirstOrDefault();

            return electionDate >= DateTime.Now;
        }

        public bool CheckIfElectionStarted(int electionId)
        {
            var electionDate = _context.DataWyborows
                .Where(x => x.Id == electionId)
                .Select(x => x.DataRozpoczecia)
                .FirstOrDefault();

            return electionDate <= DateTime.Now;
        }

        public bool CheckElectionBlockchain(int electionId)
        {
            return VerifyElectionBlockchain(electionId).All(c => c.JestPoprawny);
        }

        private List<GlosowanieWyborcze> VerifyElectionBlockchain(int electionId)
        {
            var listOfPreviousElectionVotes = _context.GlosowanieWyborczes
                .Where(r => r.IdWybory == electionId)
                .ToList();

            BlockChainHelper.VerifyBlockChain(listOfPreviousElectionVotes);
            return listOfPreviousElectionVotes;
        }

        public async Task<bool> CheckIfVoted(string user, int election)
        {
            int userId = await (from Uzytkownik in _context.Uzytkowniks
                                where Uzytkownik.Email == user
                                select Uzytkownik.Id).FirstOrDefaultAsync();

            return await _context.GlosUzytkownikas.AnyAsync(x => x.IdUzytkownik == userId && x.IdWybory == election);
        }

        public GlosowanieWyborczeViewModel SearchVote(string Text)
        {
            if (Text == "1")
            {
                var electionCandidates1 = _context.GlosowanieWyborczes.Select(_ => new GlosowanieWyborczeItemViewModel
                {
                    IdKandydat = 0,
                    IdWybory = 0,
                    Hash = "0"
                }).FirstOrDefault();

                if (electionCandidates1 == null)
                {
                    electionCandidates1 = new GlosowanieWyborczeItemViewModel
                    {
                        IdWybory = -1,
                        IdKandydat = -1
                    };
                }

                return new GlosowanieWyborczeViewModel
                {
                    SearchCandidate = electionCandidates1
                };
            }

            var electionCandidates = _context.GlosowanieWyborczes
                .Select(x => new GlosowanieWyborczeItemViewModel
                {
                    IdKandydat = x.IdKandydat,
                    IdWybory = x.IdWybory,
                    Hash = x.Hash
                })
                .Where(x => x.Hash == Text);

            var candidate = GetCandidateInfo(electionCandidates);

            return new GlosowanieWyborczeViewModel
            {
                SearchCandidate = candidate
            };
        }


        public GlosowanieWyborczeViewModel GetElectionResult(int id)
        {
            var electionResult = _context.GlosowanieWyborczes
                .Where(x => x.IdWybory == id)
                .GroupBy(x => x.IdKandydat)
                .Select(g => new GlosowanieWyborczeItemViewModel
                {
                    IdKandydat = g.Key,
                    IdWybory = id,
                    CountedVotes = g.Count()
                })
                .ToList();

            double allElectionVotes = electionResult.Sum(c => c.CountedVotes);

            if (allElectionVotes != 0)
            {
                foreach (var candidate in electionResult)
                {
                    GetCandidateInfo(candidate);
                    candidate.CountedVotesPercentage = (candidate.CountedVotes / allElectionVotes) * 100;
                }
            }

            return new GlosowanieWyborczeViewModel
            {
                GetElectionVotes = electionResult
            };
        }

        private GlosowanieWyborczeItemViewModel GetCandidateInfo(GlosowanieWyborczeItemViewModel candidate)
        {
            int idKandydat = candidate.IdKandydat;
            int idWybory = candidate.IdWybory;

            var result = from glosowanie in _context.GlosowanieWyborczes
                         join kandydat in _context.Kandydats on glosowanie.IdKandydat equals kandydat.Id
                         join dataWyborow in _context.DataWyborows on glosowanie.IdWybory equals dataWyborow.Id
                         where glosowanie.IdKandydat == idKandydat && glosowanie.IdWybory == idWybory
                         select new GlosowanieWyborczeItemViewModel
                         {
                             IdKandydat = glosowanie.IdKandydat,
                             IdWybory = glosowanie.IdWybory,
                             Hash = glosowanie.Hash,
                             CandidateName = kandydat.Imie,
                             CandidateSurname = kandydat.Nazwisko,
                             ElectionDesc = dataWyborow.Opis
                         };

            var resultCandidate = result.FirstOrDefault();
            if (resultCandidate == null)
            {
                resultCandidate = new GlosowanieWyborczeItemViewModel
                {
                    IdWybory = -1,
                    IdKandydat = -1
                };
            }

            return resultCandidate;
        }

        private GlosowanieWyborczeItemViewModel GetCandidateInfo(IQueryable<GlosowanieWyborczeItemViewModel> electionCandidates)
        {
            var candidate = electionCandidates.FirstOrDefault();

            if (candidate == null)
            {
                return new GlosowanieWyborczeItemViewModel
                {
                    IdWybory = -1,
                    IdKandydat = -1
                };
            }

            return GetCandidateInfo(candidate);
        }

        public async Task<List<DataWyborow>> ShowElectionByNameAsync()
        {
            return await _context.DataWyborows.ToListAsync();
        }
    }
}
