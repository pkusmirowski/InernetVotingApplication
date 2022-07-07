using System;
using System.ComponentModel.DataAnnotations;

namespace InernetVotingApplication.ExtensionMethods
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class AgeAttribute : ValidationAttribute
    {
        private readonly int _age;

        public AgeAttribute(int age)
        {
            _age = age;
        }

        public override bool IsValid(object value)
        {
            if (DateTime.TryParse(value.ToString(), out DateTime date))
            {
                if (DateTime.Now.Year - date.Year > 120)
                {
                    return false;
                }

                return date.AddYears(_age) < DateTime.Now;
            }

            return false;
        }
    }
}
