using System;

namespace InernetVotingApplication.ExtensionMethods
{
    public static class Extensions
    {
        /// <summary>Metoda sprawdza czy tablica jest pusta bądź jej długość to 0 elementów</summary>
        /// <param name="array">Tablica do testowania</param>
        /// <returns>True jeśli tablica spełnia warunki. Fałsz jeśli ich nie spełnia</returns>
        public static bool IsNullOrEmpty(this Array array)
        {
            return (array == null || array.Length == 0);
        }
        
    }
}
