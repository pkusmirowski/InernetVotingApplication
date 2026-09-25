namespace InternetVotingApplication.Services.Mail
{
    public sealed record EmailMessage(string To, string Subject, string HtmlBody);
}
