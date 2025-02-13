using InternetVotingApplication.ViewModels;
using System.Collections.Generic;

namespace InternetVotingApplication.ExtensionMethods
{
    internal class ItemEqualityComparer : IEqualityComparer<GlosowanieWyborczeItemViewModel>
    {
        public bool Equals(GlosowanieWyborczeItemViewModel x, GlosowanieWyborczeItemViewModel y)
        {
            if (x == null || y == null)
            {
                return false;
            }

            // Two items are equal if their IdKandydat properties are equal.
            return x.IdKandydat == y.IdKandydat;
        }

        public int GetHashCode(GlosowanieWyborczeItemViewModel obj)
        {
            if (obj == null)
            {
                return 0;
            }

            // Return the hash code of the IdKandydat property.
            return obj.IdKandydat.GetHashCode();
        }
    }
}