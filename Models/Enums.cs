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
    Duplicate,

    /// <summary>The ballot is closed once voting starts: a candidate added later would be missing for earlier voters.</summary>
    ElectionStarted
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
    HasVotes,

    /// <summary>The ballot is closed once voting starts.</summary>
    ElectionStarted
}

public enum UpdateElectionStatus
{
    Success,
    NotFound,
    Duplicate,
    InvalidDates,

    /// <summary>Voting has started: the start date can no longer move.</summary>
    StartLocked,

    /// <summary>Voting has ended: the dates can no longer change, so an ended election cannot be reopened.</summary>
    ElectionEnded,

    /// <summary>The new end would fall before a vote that has already been cast.</summary>
    EndBeforeLastVote,

    /// <summary>A vote changed the election row while the form was being saved.</summary>
    Conflict
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
