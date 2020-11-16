﻿using InernetVotingApplication.Models;
using InernetVotingApplication.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
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

        public async Task<bool> Register(Uzytkownik user)
        {
            var idnumber = await _context.Uzytkowniks.SingleOrDefaultAsync(x => x.NumerDowodu == user.NumerDowodu);
            var pesel = await _context.Uzytkowniks.SingleOrDefaultAsync(x => x.Pesel == user.Pesel);

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

        public async Task<string> GetLoggedUserName(Logowanie user, string userName)
        {
            var queryName = from Uzytkownik in _context.Uzytkowniks
                            where Uzytkownik.NumerDowodu == user.NumerDowodu
                            select Uzytkownik.Imie;

            return userName = await queryName.FirstAsync();
        }

        public async Task<bool>AuthenticateUser(Logowanie user)
        {
            var account = await _context.Uzytkowniks.SingleOrDefaultAsync(x => x.NumerDowodu == user.NumerDowodu);

            if(BC.Verify(user.Haslo, account.Haslo))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<DataWyborowViewModel> GetAllElections()
        {
            var electionDates = await _context.DataWyborows.Select(x => new DataWyborowItemViewModel
            {
                Id = x.Id,
                DataRozpoczecia = x.DataRozpoczecia,
                DataZakonczenia = x.DataZakonczenia,
                Opis = x.Opis
                
            }).ToListAsync();

            var vm = new DataWyborowViewModel
            {
                ElectionDates = electionDates
            };

            return vm;
        }
    }
}