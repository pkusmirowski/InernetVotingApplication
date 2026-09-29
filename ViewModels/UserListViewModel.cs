namespace InternetVotingApplication.ViewModels;

/// <summary>Row of the administrator's user list. Carries no password hash, PESEL or tokens.</summary>
public class UserListItemViewModel
{
    public int Id { get; set; }

    public string Imie { get; set; } = string.Empty;

    public string Nazwisko { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime DataRejestracji { get; set; }

    public bool IsActive { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsLockedOut { get; set; }

    /// <summary>Elections the account took part in (never which candidate was chosen).</summary>
    public int VoteCount { get; set; }
}
