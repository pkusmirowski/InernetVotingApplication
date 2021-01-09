﻿using InernetVotingApplication.Blockchain;
using InernetVotingApplication.ExtensionMethods;
using InernetVotingApplication.Models;
using InernetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.Internal;
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
        private readonly MailService _mailService;
        public UserService(InternetVotingContext context, MailService mailService)
        {
            _context = context;
            _mailService = mailService;
        }

        public async Task<bool> Register(Uzytkownik user)
        {
            var email = await _context.Uzytkowniks.SingleOrDefaultAsync(x => x.Email == user.Email).ConfigureAwait(false);
            var pesel = await _context.Uzytkowniks.SingleOrDefaultAsync(x => x.Pesel == user.Pesel).ConfigureAwait(false);

            if (email != null && email.Email == user.Email)
            {
                return false;
            }

            if (pesel != null && pesel.Pesel == user.Pesel)
            {
                return false;
            }

            if (!PeselValidation.IsValidPESEL(user.Pesel) || !EmailValidation.IsValidEmail(user.Email))
            {
                return false;
            }
            Guid activationCode = Guid.NewGuid();
            user.KodAktywacyjny = activationCode;
            user.Haslo = BC.HashPassword(user.Haslo);
            user.JestAktywne = false;
            await _context.AddAsync(user).ConfigureAwait(false);
            _context.SaveChanges();

            _mailService.SendEmailAfterRegistration(user);
            return true;
        }

        //Zwraca
        //0 - gdy user jest Adminem
        //1 - gdu user jest Userem
        //2 - gdy nie jest ani Adminem ani Userem
        public async Task<int> LoginAsync(Logowanie user)
        {
            var queryActive = from Uzytkownik in _context.Uzytkowniks
                              where Uzytkownik.Email == user.Email
                              select Uzytkownik.JestAktywne;

            var getUserId = (from Uzytkownik in _context.Uzytkowniks
                             where Uzytkownik.Email == user.Email
                             select Uzytkownik.Id).FirstOrDefault();

            var getUserStatus = (from Uzytkownik in _context.Uzytkowniks
                                 where Uzytkownik.JestAktywne
                                 select Uzytkownik.JestAktywne).FirstOrDefault();

            var checkIfAdmin = (from Administrator in _context.Administrators
                                where Administrator.IdUzytkownik == getUserId
                                select Administrator.IdUzytkownik).FirstOrDefault();

            if (getUserStatus && user.Email != null && user.Haslo != null && (await queryActive.FirstOrDefaultAsync().ConfigureAwait(false)) && await AuthenticateUser(user).ConfigureAwait(false))
            {
                if (getUserId == checkIfAdmin)
                {
                    return 0;
                }
                return 1;
            }

            return 2;
        }

        public async Task<string> GetLoggedEmail(Logowanie user)
        {
            var queryName = from Uzytkownik in _context.Uzytkowniks
                            where Uzytkownik.Email == user.Email
                            select Uzytkownik.Email;

            return await queryName.FirstAsync().ConfigureAwait(false);
        }

        public async Task<bool> AuthenticateUser(Logowanie user)
        {
            var account = await _context.Uzytkowniks.SingleOrDefaultAsync(x => x.Email == user.Email).ConfigureAwait(false);

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
            //Testowanie czasu wykonania kodu
            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();

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

            //var userEmail = (from Uzytkownik in _context.Uzytkowniks
            //                 where Uzytkownik.Email == user
            //                 select Uzytkownik.Email).FirstOrDefault();

            var userVoiceDB = new GlosUzytkownika
            {
                IdUzytkownik = userId,
                IdWybory = electionId,
                Glos = true
            };

            _context.Add(electionVoteDB);
            _context.Add(userVoiceDB);
            _context.SaveChanges();

            //_mailService.SendEmailVoteHash(electionVoteDB, userEmail);
            //stopwatch.Stop();
            //Console.WriteLine("Elapsed Time is {0} ms", stopwatch.ElapsedMilliseconds);

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
                                select Uzytkownik.Id).FirstOrDefaultAsync().ConfigureAwait(false);

            return await (from GlosUzytkownika in _context.GlosUzytkownikas
                          where GlosUzytkownika.IdUzytkownik == userId && GlosUzytkownika.IdWybory == election
                          select GlosUzytkownika.Glos).FirstOrDefaultAsync().ConfigureAwait(false);
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

        public bool ChagePassword(ChangePassword password, string userEmail)
        {
            var account = _context.Uzytkowniks.SingleOrDefault(x => x.Email == userEmail);
            var verifyPassword = BC.Verify(password.Password, account.Haslo);

            if (password.NewPassword != password.ConfirmNewPassword)
            {
                return false;
            }

            if (!verifyPassword)
            {
                return false;
            }

            account.Haslo = BC.HashPassword(password.NewPassword);
            _context.Update(account);
            _context.SaveChanges();

            //_mailService.SendEmailChangePassword(userEmail);

            return true;
        }

        public List<DataWyborow> ShowElectionByName()
        {
            var listOfElectionId = _context.DataWyborows.Select(r => r.Id);
            return _context.DataWyborows.Where(r => listOfElectionId.Contains(r.Id)).ToList();
        }

        public async Task<bool> AddCandidate(Kandydat candidate)
        {
            string candidateName = await (from Kandydat in _context.Kandydats
                                          where Kandydat.Imie == candidate.Imie
                                          select Kandydat.Imie).FirstOrDefaultAsync().ConfigureAwait(false);

            string candidateSurname = await (from Kandydat in _context.Kandydats
                                             where Kandydat.Nazwisko == candidate.Nazwisko
                                             select Kandydat.Nazwisko).FirstOrDefaultAsync().ConfigureAwait(false);

            int candidateId = await (from Kandydat in _context.Kandydats
                                     where Kandydat.Nazwisko == candidate.Nazwisko
                                     select Kandydat.Id).FirstOrDefaultAsync().ConfigureAwait(false);

            int candidateElectionId = await (from Kandydat in _context.Kandydats
                                             where Kandydat.IdWybory == candidate.IdWybory && Kandydat.Imie == candidateName && Kandydat.Nazwisko == candidateSurname
                                             select Kandydat.IdWybory).FirstOrDefaultAsync().ConfigureAwait(false);

            //Sprawdzenie po imieniu i nazwisku
            //Powinien być np. PESEL kandydata aby dwóch kandydatów o
            //jendakowym imieniu i nazwisku mogło wziąć udział w wyborach
            //--> Sytuacja ekstremalna!!!! - to do
            if (candidateName == candidate.Imie && candidateSurname == candidate.Nazwisko && candidateElectionId == candidate.IdWybory)
            {
                return false;
            }

            //Wybory się nie skończyły - done
            //Dodać sprawdzenie czy nie trwają - to do
            if (CheckIfElectionStarted(candidate.IdWybory) || !CheckIfElectionEnded(candidate.IdWybory))
            {
                return false;
            }

            await _context.AddAsync(candidate).ConfigureAwait(false);
            _context.SaveChanges();
            return true;
        }

        public async Task<bool> AddElectionAsync(DataWyborow dataWyborow)
        {
            string electionDescriptions = (from DataWyborow in _context.DataWyborows
                                           where DataWyborow.Opis == dataWyborow.Opis
                                           select DataWyborow.Opis).FirstOrDefault();

            if (electionDescriptions == dataWyborow.Opis)
            {
                return false;
            }
            await _context.AddAsync(dataWyborow).ConfigureAwait(false);
            _context.SaveChanges();
            return true;
        }

        public bool GetUserByAcitvationCode(Guid activationCode)
        {
            var user = (from Uzytkownik in _context.Uzytkowniks
                                           where Uzytkownik.KodAktywacyjny == activationCode
                                           select Uzytkownik).FirstOrDefault();
            if (user != null)
            {
                user.JestAktywne = true;
                user.KodAktywacyjny = Guid.Empty;
                _context.Update(user);
                _context.SaveChanges();
                return true;
            }
            return false;
        }

    }
}