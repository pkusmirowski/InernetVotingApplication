using System;

namespace InternetVotingApplication.ExtensionMethods
{
    public static class ArrayExtensions
    {
        /// <summary>
        /// Checks if the array is null or empty.
        /// </summary>
        /// <param name="array">The array to check.</param>
        /// <returns>True if the array is null or empty; otherwise, false.</returns>
        public static bool IsNullOrEmpty(this Array array)
        {
            return array == null || array.Length == 0;
        }
    }
}