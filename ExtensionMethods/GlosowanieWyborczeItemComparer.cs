using InternetVotingApplication.ViewModels;
using System.Collections.Generic;

namespace InternetVotingApplication.ExtensionMethods
{
    internal class ItemEqualityComparer : IEqualityComparer<GlosowanieWyborczeItemViewModel>
    {
        public bool Equals(GlosowanieWyborczeItemViewModel x, GlosowanieWyborczeItemViewModel y)
        {
            // Two items are equal if their keys are equal.
            return x.IdKandydat == y.IdKandydat;
        }

        public int GetHashCode(GlosowanieWyborczeItemViewModel obj)
        {
            // Return the hash code of the key.
            return obj.IdKandydat.GetHashCode();
        }
    }
}
