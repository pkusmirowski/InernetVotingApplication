using InernetVotingApplication.Controllers;
using InernetVotingApplication.IServices;
using InernetVotingApplication.Models;
using InernetVotingApplication.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Test
{
    public class AccountControllerTest
    {
        public Mock<IUserService> mock = new();
        public ElectionService? electionService;
        public AdminService? adminService;
        [Fact]
        public async void RegisterTestTrue()
        {
            var user = new Uzytkownik
            {
                Imie = "Jan",
                Nazwisko = "Inny",
                Email = "test@op.pl",
                Haslo = "P@www0rd!",
                ConfirmPassword = "P@www0rd!",
                Pesel = "56051518229"
            };
            mock.Setup(x => x.RegisterAsync(user)).ReturnsAsync(true);

            var acc = new AccountController(mock.Object, electionService, adminService);

            var result = await acc.RegisterAsync(user);
            Assert.IsAssignableFrom<ViewResult>(result);
            //mock.VerifyGet(Times.Once());
        }
        [Fact]
        public async void RegisterTestFalse()
        {
            var user = new Uzytkownik
            {
                Imie = "Jan",
                Nazwisko = "Inny",
                Email = "test@op.pl",
                Haslo = "P@www0rd!",
                ConfirmPassword = "P@www0rd!",
                Pesel = "56051518229"
            };
            mock.Setup(x => x.RegisterAsync(user)).ReturnsAsync(false);

            var acc = new AccountController(mock.Object, electionService, adminService);

            var result = await acc.RegisterAsync(user);
            Assert.IsAssignableFrom<IActionResult>(result);
            //mock.VerifyGet(Times.Once());
        }
    }
}