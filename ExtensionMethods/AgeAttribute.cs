using System;
using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.ExtensionMethods
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AgeAttribute : ValidationAttribute
    {
        private readonly int _age;

        public AgeAttribute(int age)
        {
            _age = age;
        }

        public override bool IsValid(object value)
        {
            if (value is DateTime date)
            {
                DateTime now = DateTime.Now;

                if ((now - date).TotalDays / 365.25 > 120)
                {
                    return false;
                }

                return date.AddYears(_age) < now;
            }

            return false;
        }
    }
}
