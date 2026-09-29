using InternetVotingApplication.Services;

namespace InternetVotingApplication.Tests.Unit;

/// <summary>Plain-language names shown on the audit, chain and administrator pages.</summary>
public class DescriptionTests
{
    [Theory]
    [InlineData(AuditLog.Actions.ElectionCreated, "Utworzono wybory")]
    [InlineData(AuditLog.Actions.ElectionUpdated, "Zmieniono wybory")]
    [InlineData(AuditLog.Actions.ElectionDeleted, "Usunięto wybory")]
    [InlineData(AuditLog.Actions.CandidateAdded, "Dodano kandydata")]
    [InlineData(AuditLog.Actions.CandidateDeleted, "Usunięto kandydata")]
    [InlineData(AuditLog.Actions.ChainVerified, "Kontrola rejestru głosów: bez zastrzeżeń")]
    [InlineData(AuditLog.Actions.ChainCorrupted, "Kontrola rejestru głosów: wykryto nieprawidłowości")]
    [InlineData(AuditLog.Actions.AnchorPublished, "Wysłano kopię kontrolną")]
    [InlineData(AuditLog.Actions.AdminPromoted, "Nadano uprawnienia administratora")]
    [InlineData(AuditLog.Actions.AdminRevoked, "Odebrano uprawnienia administratora")]
    [InlineData(AuditLog.Actions.UserActivated, "Aktywowano konto")]
    [InlineData("TestAccountsSeeded", "Utworzono konta testowe")]
    [InlineData("TestAccountsDisabled", "Wyłączono konta testowe poza środowiskiem Development")]
    [InlineData("SomethingNew", "SomethingNew")]
    [InlineData(null, "")]
    public void Audit_actions_have_polish_descriptions(string? action, string expected)
    {
        Assert.Equal(expected, AuditLog.Actions.Describe(action));
    }

    [Theory]
    [InlineData(ChainService.ReasonPeriodic, "okresowa")]
    [InlineData(ChainService.ReasonElectionEnded, "końcowa, wysłana po zamknięciu głosowania")]
    [InlineData(ChainService.ReasonManual, "na polecenie administratora")]
    [InlineData("Other", "Other")]
    [InlineData(null, "")]
    public void Anchor_reasons_have_polish_descriptions(string? reason, string expected)
    {
        Assert.Equal(expected, ChainService.DescribeReason(reason));
    }

    [Theory]
    [InlineData("Background", "kontrola automatyczna")]
    [InlineData("Manual", "kontrola zlecona przez administratora")]
    [InlineData("Results", "kontrola przy wyświetleniu wyników")]
    [InlineData("Vote", "Vote")]
    [InlineData(null, "")]
    public void Verification_triggers_have_polish_descriptions(string? trigger, string expected)
    {
        Assert.Equal(expected, ChainService.DescribeTrigger(trigger));
    }
}
