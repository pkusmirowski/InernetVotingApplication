using InternetVotingApplication.Blockchain;
using InternetVotingApplication.ExtensionMethods;
using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace InternetVotingApplication.Services
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
            var electionDates = _context.DataWyborows.Select(x => new DataWyborowItemViewModel
            {
                Id = x.Id,
                DataRozpoczecia = x.DataRozpoczecia,
                DataZakonczenia = x.DataZakonczenia,
                Opis = x.Opis
            }).ToList();

            foreach (var electionDate in electionDates)
            {
                if (!CheckIfElectionEnded(electionDate.Id))
                {
                    electionDate.Type = 1;
                }
                else if (!CheckIfElectionStarted(electionDate.Id))
                {
                    electionDate.Type = 2;
                }
                else
                {
                    electionDate.Type = 3;
                }
            }

            return new DataWyborowViewModel
            {
                ElectionDates = electionDates
            };
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

            return new KandydatViewModel
            {
                ElectionCandidates = electionCandidates
            };
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

            if (listOfPreviousElectionVotes.Count > 0)
            {
                var previousVote = listOfPreviousElectionVotes.Last();
                electionVoteDB.IdPoprzednie = previousVote.Id;
                electionVoteDB.Hash = HashHelper.Hash(BlockHelper.VoteData(electionVoteDB.IdKandydat, electionVoteDB.IdWybory, electionVoteDB.Glos, previousVote.Hash));
            }
            else
            {
                electionVoteDB.Hash = HashHelper.Hash(BlockHelper.VoteData(electionVoteDB.IdKandydat, electionVoteDB.IdWybory, electionVoteDB.Glos, null));
            }

            var userId = _context.Uzytkowniks
                .Where(u => u.Email == user)
                .Select(u => u.Id)
                .FirstOrDefault();

            var userEmail = _context.Uzytkowniks
                .Where(u => u.Email == user)
                .Select(u => u.Email)
                .FirstOrDefault();

            var userVoiceDB = new GlosUzytkownika
            {
                IdUzytkownik = userId,
                IdWybory = electionId,
                Glos = true
            };

            _context.AddRange(electionVoteDB, userVoiceDB);
            _context.SaveChanges();
            Email.SendEmailVoteHash(electionVoteDB, userEmail);

            return electionVoteDB.Hash;
        }

        public bool CheckIfElectionEnded(int electionId)
        {
            DateTime electionDate = _context.DataWyborows
                .Where(dw => dw.Id == electionId)
                .Select(dw => dw.DataZakonczenia)
                .FirstOrDefault();

            return electionDate >= DateTime.Now;
        }

        public bool CheckIfElectionStarted(int electionId)
        {
            DateTime electionDate = _context.DataWyborows
                .Where(dw => dw.Id == electionId)
                .Select(dw => dw.DataRozpoczecia)
                .FirstOrDefault();

            return electionDate <= DateTime.Now;
        }

        public bool CheckElectionBlockchain(int electionId)
        {
            return VerifyElectionBlockchain(electionId).All(c => c.JestPoprawny);
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
            int userId = await _context.Uzytkowniks
                .Where(u => u.Email == user)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            return await _context.GlosUzytkownikas
                .AnyAsync(g => g.IdUzytkownik == userId && g.IdWybory == election && g.Glos);
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

                electionCandidates1 ??= new GlosowanieWyborczeItemViewModel
                {
                    IdWybory = -1,
                    IdKandydat = -1
                };

                return new GlosowanieWyborczeViewModel
                {
                    SearchCandidate = electionCandidates1
                };
            }

            var electionCandidates = _context.GlosowanieWyborczes.Select(x => new GlosowanieWyborczeItemViewModel
            {
                IdKandydat = x.IdKandydat,
                IdWybory = x.IdWybory,
                Hash = x.Hash
            }).Where(x => x.Hash == Text);

            var candidate = GetCandidateInfo(electionCandidates);

            return new GlosowanieWyborczeViewModel
            {
                SearchCandidate = candidate
            };
        }

        public GlosowanieWyborczeViewModel GetElectionResult(int id)
        {
            var electionResult = _context.GlosowanieWyborczes.Select(x => new GlosowanieWyborczeItemViewModel
            {
                IdKandydat = x.IdKandydat,
                IdWybory = x.IdWybory,
            }).Where(x => x.IdWybory == id).ToList();

            for (int i = 0; i < electionResult.Count; i++)
            {
                electionResult[i] = GetCandidateInfo(electionResult[i]);
                electionResult[i].CountedVotes = CountVotes(id, electionResult[i].IdKandydat);
            }

            electionResult = electionResult.Distinct(new ItemEqualityComparer()).ToList();

            double allElectionVotes = 0;

            allElectionVotes = CountedAllVotes(electionResult, allElectionVotes);

            CountedVotesInPercentage(electionResult, allElectionVotes);

            return new GlosowanieWyborczeViewModel
            {
                GetElectionVotes = electionResult
            };
        }

        private static void CountedVotesInPercentage(List<GlosowanieWyborczeItemViewModel> electionResult, double allElectionVotes)
        {
            for (int i = 0; i < electionResult.Count; i++)
            {
                electionResult[i].CountedVotesPercentage = (electionResult[i].CountedVotes / allElectionVotes) * 100;
            }
        }

        private static double CountedAllVotes(List<GlosowanieWyborczeItemViewModel> electionResult, double allElectionVotes)
        {
            for (int i = 0; i < electionResult.Count; i++)
            {
                allElectionVotes += electionResult[i].CountedVotes;
            }

            return allElectionVotes;
        }

        public int CountVotes(int election, int candidate)
        {
            return _context.GlosowanieWyborczes
                .Count(g => g.IdKandydat == candidate && g.IdWybory == election);
        }

        private GlosowanieWyborczeItemViewModel GetCandidateInfo(GlosowanieWyborczeItemViewModel candidate)
        {
            int idKandydat = candidate.IdKandydat;
            int idWybory = candidate.IdWybory;

            string candidateName = _context.Kandydats
                .Where(Kandydat => Kandydat.Id == idKandydat)
                .Select(Kandydat => Kandydat.Imie)
                .FirstOrDefault();

            string candidateSurname = _context.Kandydats
                .Where(Kandydat => Kandydat.Id == idKandydat)
                .Select(Kandydat => Kandydat.Nazwisko)
                .FirstOrDefault();

            string electionDesc = _context.DataWyborows
                .Where(DataWyborow => DataWyborow.Id == idWybory)
                .Select(DataWyborow => DataWyborow.Opis)
                .FirstOrDefault();

            candidate.CandidateName = candidateName;
            candidate.CandidateSurname = candidateSurname;
            candidate.ElectionDesc = electionDesc;
            return candidate;
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

            int idKandydat = candidate.IdKandydat;
            int idWybory = candidate.IdWybory;

            string candidateName = _context.Kandydats
                .Where(Kandydat => Kandydat.Id == idKandydat)
                .Select(Kandydat => Kandydat.Imie)
                .FirstOrDefault();

            string candidateSurname = _context.Kandydats
                .Where(Kandydat => Kandydat.Id == idKandydat)
                .Select(Kandydat => Kandydat.Nazwisko)
                .FirstOrDefault();

            string electionDesc = _context.DataWyborows
                .Where(DataWyborow => DataWyborow.Id == idWybory)
                .Select(DataWyborow => DataWyborow.Opis)
                .FirstOrDefault();

            candidate.CandidateName = candidateName;
            candidate.CandidateSurname = candidateSurname;
            candidate.ElectionDesc = electionDesc;
            return candidate;
        }

        public List<DataWyborow> ShowElectionByName()
        {
            var listOfElectionId = _context.DataWyborows.Select(r => r.Id);
            return _context.DataWyborows.Where(r => listOfElectionId.Contains(r.Id)).ToList();
        }
    }
}
