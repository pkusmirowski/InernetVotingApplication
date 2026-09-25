using InternetVotingApplication.ExtensionMethods;

namespace InternetVotingApplication.Tests.Unit
{
    public class AgeAttributeTests
    {
        private static readonly DateTime Today = new(2026, 6, 1);

        [Fact]
        public void Person_turning_18_today_is_valid()
        {
            var attribute = new AgeAttribute(18);
            Assert.True(attribute.IsValid(Today.AddYears(-18), Today));
        }

        [Fact]
        public void Person_turning_18_tomorrow_is_invalid()
        {
            var attribute = new AgeAttribute(18);
            Assert.False(attribute.IsValid(Today.AddYears(-18).AddDays(1), Today));
        }

        [Fact]
        public void Age_above_120_is_invalid()
        {
            var attribute = new AgeAttribute(18);
            Assert.False(attribute.IsValid(Today.AddYears(-121), Today));
        }

        [Fact]
        public void Non_date_values_are_invalid()
        {
            var attribute = new AgeAttribute(18);
            Assert.False(attribute.IsValid("1990-01-01"));
            Assert.False(attribute.IsValid(null));
        }
    }
}
