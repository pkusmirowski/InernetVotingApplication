using InernetVotingApplication.Blockchain;
using InernetVotingApplication.ExtensionMethods;
using InernetVotingApplication.Models;
using InernetVotingApplication.ViewModels;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using MimeKit.Text;
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
            var email = await _context.Uzytkowniks.SingleOrDefaultAsync(x => x.Email == user.Email).ConfigureAwait(false);
            var pesel = await _context.Uzytkowniks.SingleOrDefaultAsync(x => x.Pesel == user.Pesel).ConfigureAwait(false);

            if (email != null)
            {
                if (email.Email == user.Email)
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

            if (!PESELValidation.IsValidPESEL(user.Pesel) || !EmailValidation.IsValidEmail(user.Email))
            {
                return false;
            }

            user.Haslo = BC.HashPassword(user.Haslo);
            user.JestAktywne = true;
            _context.Add(user);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            SmtpClient smtp = SendEmailAfterRegistration(user);
            return true;
        }

        private static SmtpClient SendEmailAfterRegistration(Uzytkownik user)
        {
            var sendEmail = new MimeMessage();
            sendEmail.From.Add(MailboxAddress.Parse("aplikacjadoglosowania@gmail.com"));
            sendEmail.To.Add(MailboxAddress.Parse(user.Email));
            sendEmail.Subject = "Pomyślne założenie konta w apllikacji do głosowania";
            sendEmail.Body = new TextPart(TextFormat.Html) { Text = "<h2>Twoje konto <b>" + user.Imie + " " + user.Nazwisko + "</b> w aplikacji do głosowania zostało założone pomyślnie!</h2>" };
            var smtp = new SmtpClient();
            smtp.Connect("smtp.ethereal.email", 587, SecureSocketOptions.StartTls);
            smtp.Authenticate("cleve.daugherty2@ethereal.email", "96712EmxRGP6ZNWA8V");
            smtp.Send(sendEmail);
            smtp.Disconnect(true);
            return smtp;
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

            var checkIfAdmin = (from Administrator in _context.Administrators
                                where Administrator.IdUzytkownik == getUserId
                                select Administrator.IdUzytkownik).FirstOrDefault();

            if (user.Email != null && user.Haslo != null)
            {
                if (await queryActive.FirstOrDefaultAsync().ConfigureAwait(false) ?? false)
                {
                    if (await AuthenticateUser(user).ConfigureAwait(false))
                    {
                        if (getUserId == checkIfAdmin)
                        {
                            return 0;
                        }
                        return 1;
                    }
                }
            }

            return 2;
        }

        public async Task<string> GetLoggedEmail(Logowanie user, string email)
        {
            var queryName = from Uzytkownik in _context.Uzytkowniks
                            where Uzytkownik.Email == user.Email
                            select Uzytkownik.Email;

            return email = await queryName.FirstAsync().ConfigureAwait(false);
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
            CountVotes(electionId, candidateId);

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

            SendEmailVoteHash(electionVoteDB, userEmail);

            return electionVoteDB.Hash;
        }

        private static void SendEmailVoteHash(GlosowanieWyborcze electionVoteDB, string userEmail)
        {
            var sendEmail = new MimeMessage();
            sendEmail.From.Add(MailboxAddress.Parse("aplikacjadoglosowania@gmail.com"));
            sendEmail.To.Add(MailboxAddress.Parse(userEmail));
            sendEmail.Subject = "Dziękujemy za zagłosowanie w wyborach";
            sendEmail.Body = new TextPart(TextFormat.Html) { Text = "<h2>Hash twojego głosu: <b>" + electionVoteDB.Hash + "</b></h2></br> <p>Możesz sprawdzić poprawność swojego głosu w wyszukiwarce znajdującej się na stronie</p>" };
            var smtp = new SmtpClient();
            smtp.Connect("smtp.ethereal.email", 587, SecureSocketOptions.StartTls);
            smtp.Authenticate("cleve.daugherty2@ethereal.email", "96712EmxRGP6ZNWA8V");
            smtp.Send(sendEmail);
            smtp.Disconnect(true);
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

            bool ifVoted = await (from GlosUzytkownika in _context.GlosUzytkownikas
                                  where GlosUzytkownika.IdUzytkownik == userId && GlosUzytkownika.IdWybory == election
                                  select GlosUzytkownika.Glos).FirstOrDefaultAsync().ConfigureAwait(false);
            return ifVoted;
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

            var vm = new GlosowanieWyborczeViewModel
            {
                GetElectionVotes = electionResult
            };

            return vm;
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
            var count = (from GlosowanieWyborcze in _context.GlosowanieWyborczes
                         where GlosowanieWyborcze.IdKandydat == candidate && GlosowanieWyborcze.IdWybory == election
                         select GlosowanieWyborcze.Glos).Count();

            return count;
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