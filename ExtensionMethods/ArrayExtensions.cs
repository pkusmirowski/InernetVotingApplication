using System;
using System.Diagnostics.Contracts;

namespace InernetVotingApplication.ExtensionMethods
{
    public static class ArrayExtensions
    {
        // Metoda z atrybutem Pure, informującym kompilator o tym, że metoda nie modyfikuje stanu programu
        [Pure]
        public static bool IsNullOrEmpty(this Array array)
        {
            // Zwraca wartość true, jeśli przekazana tablica jest null lub ma długość równą 0
            return array == null || array.Length == 0;
        }
    }
}