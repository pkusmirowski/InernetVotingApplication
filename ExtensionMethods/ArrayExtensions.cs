using System;

namespace InternetVotingApplication.ExtensionMethods
{
    public static class ArrayExtensions
    {
        /// <summary>
        /// Metoda sprawdza, czy tablica jest pusta bądź jej długość wynosi 0 elementów.
        /// </summary>
        /// <param name="array">Tablica do testowania.</param>
        /// <returns>True, jeśli tablica spełnia warunki. Fałsz, jeśli nie spełnia.</returns>
        public static bool IsNullOrEmpty(this Array array)
        {
            return array == null || array.Length == 0;
        }
    }
}
