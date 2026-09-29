using InternetVotingApplication.ExtensionMethods;
using InternetVotingApplication.Models;

namespace InternetVotingApplication.Tests.Unit;

public class EmailTemplatesTests
{
    [Fact]
    public void Activation_mail_contains_link_and_encodes_names()
    {
        var mail = Email.AfterRegistration("a@b.pl", "<b>Jan</b>", "O'Neil", "https://app/activate/1");

        Assert.Equal("a@b.pl", mail.To);
        Assert.Contains("href=\"https://app/activate/1\"", mail.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>Jan</b>", mail.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("&lt;b&gt;Jan&lt;/b&gt;", mail.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Receipt_and_password_mails_carry_the_expected_data()
    {
        var receipt = Email.VoteReceipt("a@b.pl", "Wybory & Referendum", "HASH");
        Assert.Contains("HASH", receipt.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Wybory &amp; Referendum", receipt.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("<a ", receipt.HtmlBody, StringComparison.Ordinal);

        var linked = Email.VoteReceipt("a@b.pl", "W", "HASH", "https://glosowanie.pl/Account/Search?hash=HASH");
        Assert.Contains("href=\"https://glosowanie.pl/Account/Search?hash=HASH\"", linked.HtmlBody, StringComparison.Ordinal);

        var reset = Email.PasswordReset("a@b.pl", "https://app/reset?token=1", TimeSpan.FromMinutes(90));
        Assert.Contains("90 minut", reset.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("https://app/reset?token=1", reset.HtmlBody, StringComparison.Ordinal);

        var changed = Email.PasswordChanged("a@b.pl");
        Assert.Contains("zmienione", changed.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Anchor_mail_lists_head_hash_block_count_and_signature()
    {
        var anchor = new KotwicaLancucha { Data = new DateTime(2026, 6, 1, 12, 0, 0), LiczbaBlokow = 42, HashGlowy = "ABC", Powod = "Manual", Podpis = "SIG==" };

        var mail = Email.ChainAnchor("komisja@b.pl", "Wybory", 7, anchor, "KEY1");

        Assert.Contains("42", mail.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("ABC", mail.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("SIG==", mail.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("KEY1", mail.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("id 7", mail.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Kopia kontrolna", mail.Subject, StringComparison.Ordinal);
    }
}
