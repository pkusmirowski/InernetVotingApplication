using System.Linq;

namespace InernetVotingApplication.ExtensionMethods
{
    public static class PeselValidation
    {
        // Metoda sprawdzająca poprawność numeru PESEL
        public static bool IsValidPESEL(string input)
        {
            // Wagi poszczególnych cyfr PESEL
            int[] weights = { 1, 3, 7, 9, 1, 3, 7, 9, 1, 3 };

            // Sprawdzenie, czy długość podanego numeru PESEL jest równa 11
            if (input.Length != 11)
            {
                return false;
            }

            int controlSum = 0;
            // Dla każdej cyfry z pierwszych 10 wykorzystywana jest waga z tablicy weights
            // i obliczana jest suma kontrolna
            foreach (var (digit, weight) in input[..10].Zip(weights))
            {
                // Sprawdzenie, czy dana cyfra jest liczbą
                if (!int.TryParse(digit.ToString(), out int parsedDigit))
                {
                    return false;
                }
                controlSum += weight * parsedDigit;
            }
            // Obliczenie ostatniej cyfry numeru PESEL
            int controlNumber = (10 - (controlSum % 10)) % 10;
            // Sprawdzenie, czy obliczona cyfra kontrolna jest równa ostatniej cyfrze numeru PESEL
            return controlNumber == int.Parse(input[^1].ToString());
        }
    }
}
