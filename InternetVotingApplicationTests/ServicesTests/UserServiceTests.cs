using InernetVotingApplication.Models;
using InernetVotingApplication.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InternetVotingApplicationTests
{
    [TestFixture]
    public class UserServiceTests
    {
        private readonly Uzytkownik userOne;
        private DbContextOptions<InternetVotingContext> options;

        public UserServiceTests()
        {
            userOne = new Uzytkownik()
            {
                Id = 1,
                Imie = "Jan",
                Nazwisko = "Inny",
                Pesel = "51092337495",
                Email = "test@op.pl",
                DataUrodzenia = new DateTime(1990, 1, 1),
                Haslo = "P@ssw0rd"
            };
        }

        [SetUp]
        public void SetUp()
        {
            options = new DbContextOptionsBuilder<InternetVotingContext>().UseInMemoryDatabase(databaseName: "tempDB").Options;
        }

        //Works alone
        [Test]
        public async Task LoginAndActivationAccount_ReturnsSuccessfullyActivatedAndRegisteredUser()
        {
            var login = new Logowanie()
            {
                Email = "test@op.pl",
                Haslo = "P@ssw0rd"
            };

            var context = new InternetVotingContext(options);
            context.Database.EnsureDeleted();
            var repository = new UserService(context);
            bool result = await repository.RegisterAsync(userOne);
            var bookingFromDb = context.Uzytkowniks.FirstOrDefault(p => p.Id == 1);
            if (bookingFromDb != null)
            {
                bool activationResult = repository.GetUserByAcitvationCode(bookingFromDb.KodAktywacyjny);
                Assert.IsTrue(activationResult);
            }
            int loginresult = await repository.LoginAsync(login);
            Assert.AreEqual(1, loginresult);
        }

        [Test]
        public async Task RegisterUser_CheckValuesFromDatabase()
        {
            using (var context = new InternetVotingContext(options))
            {
                context.Database.EnsureDeleted();
                var repository = new UserService(context);
                bool result = await repository.RegisterAsync(userOne);
                Assert.IsTrue(result);
            }

            using (var context = new InternetVotingContext(options))
            {
                var bookingFromDb = context.Uzytkowniks.FirstOrDefault();
                if (bookingFromDb != null)
                {
                    Assert.AreEqual(bookingFromDb.Id, userOne.Id);
                    Assert.AreEqual(bookingFromDb.Imie, userOne.Imie);
                    Assert.AreEqual(bookingFromDb.Nazwisko, userOne.Nazwisko);
                    Assert.AreEqual(bookingFromDb.Pesel, userOne.Pesel);
                    Assert.AreEqual(bookingFromDb.Email, userOne.Email);
                    Assert.AreEqual(bookingFromDb.DataUrodzenia, userOne.DataUrodzenia);
                    Assert.AreEqual(bookingFromDb.Haslo, userOne.Haslo);
                }
            }
        }

        [Test]
        public async Task ChangePassword_ReturnsSuccessfullyPasswordChange()
        {
            var login = new ChangePassword()
            {
                Password = "P@ssw0rd",
                NewPassword = "PASSW0RD",
                ConfirmNewPassword = "PASSW0RD"
            };

            UserService repository = await RegisterUser();

            bool changePasswordResult = repository.ChangePassword(login, "test@op.pl");
            Assert.IsTrue(changePasswordResult);
        }

        [Test]
        public async Task RecoverPassword_ReturnsSuccessfullyChangedPassword()
        {
            var recPassword = new PasswordRecovery()
            {
                Email = "test@op.pl",
                Pesel = "51092337495"
            };

            UserService repository = await RegisterUser();

            bool recoverPasswordResult = await repository.RecoverPassword(recPassword);
            Assert.IsTrue(recoverPasswordResult);
        }

        private async Task<UserService> RegisterUser()
        {
            var context = new InternetVotingContext(options);
            context.Database.EnsureDeleted();
            var repository = new UserService(context);
            await repository.RegisterAsync(userOne);
            var bookingFromDb = context.Uzytkowniks.FirstOrDefault();
            if (bookingFromDb != null)
            {
                repository.GetUserByAcitvationCode(bookingFromDb.KodAktywacyjny);
            }

            return repository;
        }
    }
}
