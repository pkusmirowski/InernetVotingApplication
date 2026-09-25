namespace InternetVotingApplication
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Voter = "Voter";
    }

    public static class AuthorizationPolicies
    {
        public const string AdminOnly = "AdminOnly";
    }

    public static class RateLimitPolicies
    {
        /// <summary>Sign-in, registration and password recovery: a small fixed window per client address.</summary>
        public const string Auth = "auth";
    }
}
