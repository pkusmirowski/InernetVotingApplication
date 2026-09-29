namespace InternetVotingApplication.Models;

public enum ElectionStatus
{
    /// <summary>Start date is in the future.</summary>
    Upcoming,

    /// <summary>Voting is open.</summary>
    Ongoing,

    /// <summary>End date has passed; results are available.</summary>
    Ended
}

public enum LoginStatus
{
    Success,
    InvalidCredentials,
    NotActivated,
    LockedOut
}

public enum RegistrationStatus
{
    Success,
    EmailTaken,
    PeselTaken,
    InvalidPesel,
    InvalidEmail
}

public enum VoteStatus
{
    Success,
    ElectionNotFound,
    ElectionNotStarted,
    ElectionEnded,
    CandidateNotInElection,
    AlreadyVoted,
    ChainCorrupted,

    /// <summary>Lost the optimistic-concurrency race several times in a row; the voter should retry.</summary>
    Conflict
}

public enum AddCandidateStatus
{
    Success,
    ElectionNotFound,
    Duplicate
}

public enum AddElectionStatus
{
    Success,
    Duplicate,
    InvalidDates
}

public enum DeleteCandidateStatus
{
    Success,
    NotFound,
    HasVotes
}

public enum UpdateElectionStatus
{
    Success,
    NotFound,
    Duplicate,
    InvalidDates
}

public enum DeleteElectionStatus
{
    Success,
    NotFound,
    HasVotes
}

public enum UserActionStatus
{
    Success,
    NotFound,

    /// <summary>Nothing to do: the account is already in the requested state.</summary>
    NoChange,

    /// <summary>An administrator may not revoke their own role, and the last administrator cannot be removed.</summary>
    Forbidden
}
