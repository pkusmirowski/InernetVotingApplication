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
        private readonly MailService _mailService;

        public ElectionService(InternetVotingContext context, MailService mailService)
        {
            _context = context;
            _mailService = mailService;
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

            string previousBlockHash = null;
            if (listOfPreviousElectionVotes.Count > 0)
            {
                var previousVote = listOfPreviousElectionVotes.Last();
                electionVoteDB.IdPoprzednie = previousVote.Id;
                previousBlockHash = previousVote.Hash;
            }

            var blockText = BlockHelper.VoteData(electionVoteDB.IdKandydat, electionVoteDB.IdWybory, electionVoteDB.Glos, previousBlockHash);
            electionVoteDB.Hash = HashHelper.Hash(blockText);

            var userId = (from Uzytkownik in _context.Uzytkowniks
                          where Uzytkownik.Email == user
                          select Uzytkownik.Id).FirstOrDefault();

            var userEmail = (from Uzytkownik in _context.Uzytkowniks
                             where Uzytkownik.Email == user
                             select Uzytkownik.Email).FirstOrDefault();

            var userVoiceDB = new GlosUzytkownika
            {
                IdUzytkownik = userId,
                IdWybory = electionId,
                Glos = true
            };

            _context.Add(electionVoteDB);
            _context.Add(userVoiceDB);
            _context.SaveChanges();
            _mailService.SendEmailVoteHash(electionVoteDB, userEmail);

            return electionVoteDB.Hash;
        }

        public bool CheckIfElectionEnded(int electionId)
        {
            var electionDateQuery = from DataWyborow in _context.DataWyborows
                                    where electionId == DataWyborow.Id
                                    select DataWyborow.DataZakonczenia;
            DateTime electionDate = electionDateQuery.AsEnumerable().First();

            return electionDate >= DateTime.Now;
        }

        public bool CheckIfElectionStarted(int electionId)
        {
            var electionDateQuery = from DataWyborow in _context.DataWyborows
                                    where electionId == DataWyborow.Id
                                    select DataWyborow.DataRozpoczecia;
            DateTime electionDate = electionDateQuery.AsEnumerable().First();

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
            int userId = await (from Uzytkownik in _context.Uzytkowniks
                                where Uzytkownik.Email == user
                                select Uzytkownik.Id).FirstOrDefaultAsync();

            return await (from GlosUzytkownika in _context.GlosUzytkownikas
                          where GlosUzytkownika.IdUzytkownik == userId && GlosUzytkownika.IdWybory == election
                          select GlosUzytkownika.Glos).FirstOrDefaultAsync();
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
            return (from GlosowanieWyborcze in _context.GlosowanieWyborczes
                    where GlosowanieWyborcze.IdKandydat == candidate && GlosowanieWyborcze.IdWybory == election
                    select GlosowanieWyborcze.Glos).Count();
        }

        private GlosowanieWyborczeItemViewModel GetCandidateInfo(GlosowanieWyborczeItemViewModel candidate)
        {
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

        public List<DataWyborow> ShowElectionByName()
        {
            var listOfElectionId = _context.DataWyborows.Select(r => r.Id);
            return _context.DataWyborows.Where(r => listOfElectionId.Contains(r.Id)).ToList();
        }
    }
}