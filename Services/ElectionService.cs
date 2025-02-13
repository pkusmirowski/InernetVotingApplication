using InternetVotingApplication.Blockchain;
using InternetVotingApplication.ExtensionMethods;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InternetVotingApplication.Services
{
    public class ElectionService(InternetVotingContext context) : IElectionService
    {
        private readonly InternetVotingContext _context = context;

        public DataWyborowViewModel GetAllElections()
        {
            var electionDates = _context.DataWyborows.Select(x => new DataWyborowItemViewModel
            {
                Id = x.Id,
                DataRozpoczecia = x.DataRozpoczecia,
                DataZakonczenia = x.DataZakonczenia,
                Opis = x.Opis,
                Type = GetElectionType(x.Id)
            }).ToList();

            return new DataWyborowViewModel
            {
                ElectionDates = electionDates
            };
        }

        private int GetElectionType(int electionId)
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
                }).ToList();

            return new KandydatViewModel
            {
                ElectionCandidates = electionCandidates
            };
        }

        public string AddVote(string user, int candidateId, int electionId)
        {
            var listOfPreviousElectionVotes = VerifyElectionBlockchain(electionId);
            if (listOfPreviousElectionVotes.Any(c => !c.JestPoprawny))
            {
                return "0";
            }

            var electionVoteDB = new GlosowanieWyborcze
            {
                IdKandydat = candidateId,
                IdWybory = electionId,
                Glos = true,
                IdPoprzednie = listOfPreviousElectionVotes.LastOrDefault()?.Id,
                Hash = HashHelper.Hash(BlockHelper.VoteData(candidateId, electionId, true, listOfPreviousElectionVotes.LastOrDefault()?.Hash))
            };

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
            var electionDate = _context.DataWyborows
                .Where(dw => dw.Id == electionId)
                .Select(dw => dw.DataZakonczenia)
                .FirstOrDefault();

            return electionDate >= DateTime.Now;
        }

        public bool CheckIfElectionStarted(int electionId)
        {
            var electionDate = _context.DataWyborows
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
            var listOfPreviousElectionVotes = _context.GlosowanieWyborczes
                .Where(r => r.IdWybory == electionId)
                .ToList();

            BlockChainHelper.VerifyBlockChain(listOfPreviousElectionVotes);
            return listOfPreviousElectionVotes;
        }

        public async Task<bool> CheckIfVoted(string user, int election)
        {
            var userId = await _context.Uzytkowniks
                .Where(u => u.Email == user)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            return await _context.GlosUzytkownikas
                .AnyAsync(g => g.IdUzytkownik == userId && g.IdWybory == election && g.Glos);
        }

        public GlosowanieWyborczeViewModel SearchVote(string text)
        {
            var electionCandidates = _context.GlosowanieWyborczes
                .Where(x => x.Hash == text)
                .Select(x => new GlosowanieWyborczeItemViewModel
                {
                    IdKandydat = x.IdKandydat,
                    IdWybory = x.IdWybory,
                    Hash = x.Hash
                }).FirstOrDefault();

            if (electionCandidates == null)
            {
                electionCandidates = new GlosowanieWyborczeItemViewModel
                {
                    IdWybory = -1,
                    IdKandydat = -1
                };
            }
            else
            {
                electionCandidates = GetCandidateInfo(electionCandidates);
            }

            return new GlosowanieWyborczeViewModel
            {
                SearchCandidate = electionCandidates
            };
        }
        public GlosowanieWyborczeViewModel GetElectionResult(int id)
        {
            var electionResult = _context.GlosowanieWyborczes
                .Where(x => x.IdWybory == id)
                .Select(x => new GlosowanieWyborczeItemViewModel
                {
                    IdKandydat = x.IdKandydat,
                    IdWybory = x.IdWybory,
                }).ToList();

            foreach (var result in electionResult)
            {
                var candidateInfo = GetCandidateInfo(result);
                candidateInfo.CountedVotes = CountVotes(id, candidateInfo.IdKandydat);
                result.CandidateName = candidateInfo.CandidateName;
                result.CandidateSurname = candidateInfo.CandidateSurname;
                result.ElectionDesc = candidateInfo.ElectionDesc;
                result.CountedVotes = candidateInfo.CountedVotes;
            }

            electionResult = electionResult.Distinct(new ItemEqualityComparer()).ToList();

            var allElectionVotes = electionResult.Sum(x => x.CountedVotes);

            foreach (var result in electionResult)
            {
                result.CountedVotesPercentage = (result.CountedVotes / allElectionVotes) * 100;
            }

            return new GlosowanieWyborczeViewModel
            {
                GetElectionVotes = electionResult
            };
        }

        public int CountVotes(int election, int candidate)
        {
            return _context.GlosowanieWyborczes
                .Count(g => g.IdKandydat == candidate && g.IdWybory == election);
        }

        private GlosowanieWyborczeItemViewModel GetCandidateInfo(GlosowanieWyborczeItemViewModel candidate)
        {
            var candidateInfo = _context.Kandydats
                .Where(k => k.Id == candidate.IdKandydat)
                .Select(k => new
                {
                    k.Imie,
                    k.Nazwisko
                }).FirstOrDefault();

            var electionDesc = _context.DataWyborows
                .Where(dw => dw.Id == candidate.IdWybory)
                .Select(dw => dw.Opis)
                .FirstOrDefault();

            candidate.CandidateName = candidateInfo?.Imie;
            candidate.CandidateSurname = candidateInfo?.Nazwisko;
            candidate.ElectionDesc = electionDesc;

            return candidate;
        }

        public List<DataWyborow> ShowElectionByName()
        {
            return _context.DataWyborows.ToList();
        }
    }
}
