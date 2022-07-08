using InernetVotingApplication.ExtensionMethods;
using NUnit.Framework;

namespace InternetVotingApplicationTests.ExtensionMethodsTests
{
    [TestFixture]
    public class PeselValidationTests
    {
        [Test]
        public void PESELValidate_ReturnsTrue()
        {
            const string PESEL = "58010662721";
            var result = PeselValidation.IsValidPESEL(PESEL);
            Assert.IsTrue(result);
        }
    }
}
