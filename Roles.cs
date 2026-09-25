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
}
