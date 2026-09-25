using InternetVotingApplication.ExtensionMethods;

namespace InternetVotingApplication.Tests.Unit
{
    public class PeselValidationTests
    {
        [Theory]
        [InlineData("44051401359")]
        [InlineData("02070803628")]
        [InlineData("00000000000")]
        public void Accepts_numbers_with_correct_checksum(string pesel)
        {
            Assert.True(PeselValidation.IsValidPESEL(pesel));
        }

        [Theory]
        [InlineData("44051401358")]
        [InlineData("4405140135")]
        [InlineData("440514013599")]
        [InlineData("4405140135a")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Rejects_invalid_numbers(string? pesel)
        {
            Assert.False(PeselValidation.IsValidPESEL(pesel!));
        }
    }
}
