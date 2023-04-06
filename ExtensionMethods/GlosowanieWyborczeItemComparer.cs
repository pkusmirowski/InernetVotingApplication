using InernetVotingApplication.ViewModels;
using System.Collections.Generic;

namespace InernetVotingApplication.ExtensionMethods
{
    internal class ItemEqualityComparer : IEqualityComparer<GlosowanieWyborczeItemViewModel>
    {
        public bool Equals(GlosowanieWyborczeItemViewModel x, GlosowanieWyborczeItemViewModel y)
        {
            // Two items are equal if their IdKandydat properties are equal.
            return x.IdKandydat == y.IdKandydat;
        }

        public int GetHashCode(GlosowanieWyborczeItemViewModel obj)
        {
            // Returns the hash code of the IdKandydat property.
            return obj.IdKandydat.GetHashCode();
        }
    }
}
