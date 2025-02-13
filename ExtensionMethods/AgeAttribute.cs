using System;
using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.ExtensionMethods
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        public AgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
        }

        public override bool IsValid(object value)
        {
            if (value is DateTime birthDate)
            {
                var today = DateTime.Today;
                var age = today.Year - birthDate.Year;

                if (birthDate > today.AddYears(-age))
                {
                    age--;
                }

                return age >= _minimumAge && age <= 120;
            }

            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field requires a valid date of birth with an age between {_minimumAge} and 120 years.";
        }
    }
}