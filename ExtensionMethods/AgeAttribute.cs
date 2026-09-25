using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.ExtensionMethods
{
    /// <summary>
    /// Validates that a date of birth corresponds to an age between <c>minimumAge</c> and 120 years.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class AgeAttribute(int minimumAge) : ValidationAttribute
    {
        public int MinimumAge { get; } = minimumAge;

        public override bool IsValid(object? value)
        {
            if (value is not DateTime birthDate)
            {
                return false;
            }

            return IsValid(birthDate, DateTime.Today);
        }

        public bool IsValid(DateTime birthDate, DateTime today)
        {
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age))
            {
                age--;
            }

            return age >= MinimumAge && age <= 120;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"Pole {name} musi zawierać datę urodzenia osoby w wieku od {MinimumAge} do 120 lat.";
        }
    }
}
