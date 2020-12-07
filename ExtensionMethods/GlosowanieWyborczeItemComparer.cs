using InernetVotingApplication.ViewModels;
using System.Collections.Generic;

class ItemEqualityComparer : IEqualityComparer<GlosowanieWyborczeItemViewModel>
{
    public bool Equals(GlosowanieWyborczeItemViewModel x, GlosowanieWyborczeItemViewModel y)
    {
        // Two items are equal if their keys are equal.
        return x.IdKandydat == y.IdKandydat;
    }

    public int GetHashCode(GlosowanieWyborczeItemViewModel obj)
    {
        return obj.IdKandydat.GetHashCode();
    }
}