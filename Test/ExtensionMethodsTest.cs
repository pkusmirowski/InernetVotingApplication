
using InernetVotingApplication.ExtensionMethods;
using Xunit;
namespace Test
{
    public class ExtensionMethodsTest
    {
        [Fact]
        public void IsValidPESELTest()
        {
            const string pesel = "77022154992";
            bool test = PeselValidation.IsValidPESEL(pesel);
            Assert.True(test);
        }

        [Fact]
        public void IsValidEmailTest()
        {
            const string email = "test@op.pl";
            bool test = EmailValidation.IsValidEmail(email);
            Assert.True(test);
        }

        [Fact]
        public void GeneratePasswordTest()
        {
            const int passwordChars = 8;
            string password = GeneratePassword.CreateRandomPassword(passwordChars);
            int passwordLength = password.Length;
            Assert.Equal(passwordLength, passwordChars);
        }
    }
}
