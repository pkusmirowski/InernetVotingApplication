using InernetVotingApplication.ExtensionMethods;
using NUnit.Framework;

namespace InternetVotingApplicationTests.ExtensionMethodsTests
{
    [TestFixture]
    public class GeneratePasswordTests
    {
        [Test]
        public void GenerateRandomPassword_ResultsNotEmpty()
        {
            const int length = 10;
            var result = GeneratePassword.CreateRandomPassword(length);
            Assert.NotNull(result);
            Assert.That(result.Length, Is.EqualTo(length));
        }
    }
}
