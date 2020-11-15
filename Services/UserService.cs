using InernetVotingApplication.Models;
using InernetVotingApplication.ViewModels;
using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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


        public bool Register(Uzytkownik user)
        {
            var idnumber = _context.Uzytkowniks.SingleOrDefault(x => x.NumerDowodu == user.NumerDowodu);
            var pesel = _context.Uzytkowniks.SingleOrDefault(x => x.Pesel == user.Pesel);

            if(idnumber != null)
            {
                if(pesel != null)
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
            _context.SaveChanges();
            return true;
        }

        public bool Login(Logowanie user, ref string userName)
        {
            if (user.NumerDowodu != null && user.Haslo != null)
            {
                var queryActive = from Uzytkownik in _context.Uzytkowniks
                            where Uzytkownik.NumerDowodu == user.NumerDowodu
                            select Uzytkownik.JestAktywne;
                
                if (queryActive.FirstOrDefault() ?? false)
                {
                    if (Authenticate(user))
                    {
                        var queryName = from Uzytkownik in _context.Uzytkowniks
                                        where Uzytkownik.NumerDowodu == user.NumerDowodu
                                        select Uzytkownik.Imie;

                        userName = queryName.First();

                        return true;
                    }
                }
            }

            return false;
        }

        public bool Authenticate(Logowanie user)
        {
            var account = _context.Uzytkowniks.SingleOrDefault(x => x.NumerDowodu == user.NumerDowodu);

            if(BC.Verify(user.Haslo, account.Haslo))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public DataWyborowViewModel GetAll()
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
    }
}