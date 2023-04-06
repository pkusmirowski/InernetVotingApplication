using System;
using System.ComponentModel.DataAnnotations;

namespace InernetVotingApplication.ExtensionMethods
{
    // Dekorator AttributeUsage określa, że atrybut Age może być używany dla wszystkich typów oraz że nie można go użyć wielokrotnie na tym samym elemencie.
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class AgeAttribute : ValidationAttribute
    {
        private readonly int _age;

        // Konstruktor, który ustawia wiek i sprawdza, czy jest dodatni.
        public AgeAttribute(int age)
        {
            if (age < 0)
            {
                throw new ArgumentException("Age must be a positive integer");
            }
            _age = age;
        }

        // Metoda IsValid służy do walidacji wartości wieku.
        // Zwraca true, jeśli wiek użytkownika jest większy niż wartość _age, a w przeciwnym razie false.
        public override bool IsValid(object value)
        {
            if (value is not DateTime date)
            {
                return false;
            }

            if (DateTime.Now.Year - date.Year > 120)
            {
                return false;
            }

            return date.AddYears(_age) < DateTime.Now;
        }
    }
}
