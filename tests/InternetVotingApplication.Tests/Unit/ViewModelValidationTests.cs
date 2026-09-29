using System.ComponentModel.DataAnnotations;
using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;

namespace InternetVotingApplication.Tests.Unit;

/// <summary>Data-annotation rules of the form models, evaluated the same way MVC model binding does.</summary>
public class ViewModelValidationTests
{
    private static IReadOnlyList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static RegisterViewModel ValidRegistration() => new()
    {
        Imie = "Anna",
        Nazwisko = "Nowak",
        Email = "anna@example.com",
        Pesel = "02070803628",
        DataUrodzenia = DateTime.Today.AddYears(-30),
        Haslo = "Secret#Pass1",
        ConfirmPassword = "Secret#Pass1",
    };

    [Fact]
    public void Valid_registration_passes()
    {
        Assert.Empty(Validate(ValidRegistration()));
    }

    [Theory]
    [InlineData("short1!", "Haslo")]
    [InlineData("alllowercase1!", "Haslo")]
    [InlineData("NoDigits!!", "Haslo")]
    [InlineData("NoSpecial123", "Haslo")]
    public void Weak_passwords_are_rejected(string password, string member)
    {
        var model = ValidRegistration();
        model.Haslo = password;
        model.ConfirmPassword = password;

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(member));
    }

    [Fact]
    public void Password_confirmation_must_match()
    {
        var model = ValidRegistration();
        model.ConfirmPassword = "Other#Pass1";

        Assert.Contains(Validate(model), r => r.MemberNames.Contains("ConfirmPassword"));
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("123456789012")]
    [InlineData("1234567890a")]
    [InlineData("")]
    public void Pesel_must_be_eleven_digits(string pesel)
    {
        var model = ValidRegistration();
        model.Pesel = pesel;

        Assert.Contains(Validate(model), r => r.MemberNames.Contains("Pesel"));
    }

    [Fact]
    public void Minors_and_missing_birth_dates_are_rejected()
    {
        var minor = ValidRegistration();
        minor.DataUrodzenia = DateTime.Today.AddYears(-17);
        Assert.Contains(Validate(minor), r => r.MemberNames.Contains("DataUrodzenia"));

        var missing = ValidRegistration();
        missing.DataUrodzenia = null;
        Assert.Contains(Validate(missing), r => r.MemberNames.Contains("DataUrodzenia"));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void Email_must_be_valid(string email)
    {
        var model = ValidRegistration();
        model.Email = email;

        Assert.Contains(Validate(model), r => r.MemberNames.Contains("Email"));
    }

    [Fact]
    public void Election_form_requires_end_after_start()
    {
        var start = new DateTime(2026, 6, 1, 8, 0, 0);
        var ok = new ElectionFormViewModel { Opis = "W", DataRozpoczecia = start, DataZakonczenia = start.AddHours(1) };
        Assert.Empty(Validate(ok));

        var same = new ElectionFormViewModel { Opis = "W", DataRozpoczecia = start, DataZakonczenia = start };
        Assert.Contains(Validate(same), r => r.MemberNames.Contains("DataZakonczenia"));

        var missing = new ElectionFormViewModel { Opis = "" };
        var errors = Validate(missing);
        Assert.Contains(errors, r => r.MemberNames.Contains("Opis"));
        Assert.Contains(errors, r => r.MemberNames.Contains("DataRozpoczecia"));
    }

    [Fact]
    public void Candidate_form_requires_all_fields()
    {
        var errors = Validate(new CandidateFormViewModel());

        Assert.Contains(errors, r => r.MemberNames.Contains("Imie"));
        Assert.Contains(errors, r => r.MemberNames.Contains("Nazwisko"));
        Assert.Contains(errors, r => r.MemberNames.Contains("IdWybory"));
        Assert.Empty(Validate(new CandidateFormViewModel { Imie = "A", Nazwisko = "B", IdWybory = 1 }));
    }

    [Fact]
    public void Change_and_reset_password_forms_apply_policy_and_confirmation()
    {
        var change = new ChangePassword { Password = "old", NewPassword = "weak", ConfirmNewPassword = "weak" };
        Assert.Contains(Validate(change), r => r.MemberNames.Contains("NewPassword"));

        var reset = new ResetPasswordViewModel { Token = Guid.NewGuid(), NewPassword = "Secret#Pass1", ConfirmNewPassword = "Different#1" };
        Assert.Contains(Validate(reset), r => r.MemberNames.Contains("ConfirmNewPassword"));

        Assert.Empty(Validate(new ResetPasswordViewModel { Token = Guid.NewGuid(), NewPassword = "Secret#Pass1", ConfirmNewPassword = "Secret#Pass1" }));
    }

    [Fact]
    public void Login_and_recovery_forms_require_email_and_password()
    {
        Assert.Contains(Validate(new Logowanie()), r => r.MemberNames.Contains("Email"));
        Assert.Contains(Validate(new Logowanie()), r => r.MemberNames.Contains("Haslo"));
        Assert.Contains(Validate(new PasswordRecovery { Email = "x" }), r => r.MemberNames.Contains("Email"));
        Assert.Empty(Validate(new PasswordRecovery { Email = "a@b.pl" }));
    }

    [Fact]
    public void Too_long_values_are_reported_in_polish()
    {
        var model = ValidRegistration();
        model.Imie = new string('a', 51);

        var error = Assert.Single(Validate(model), r => r.MemberNames.Contains("Imie"));
        Assert.Equal("To pole może mieć najwyżej 50 znaków.", error.ErrorMessage);
    }

    [Fact]
    public void No_form_model_falls_back_to_an_english_framework_message()
    {
        object[] emptyForms =
        [
            new RegisterViewModel(), new Logowanie(), new PasswordRecovery(), new ChangePassword(),
            new ResetPasswordViewModel(), new CandidateFormViewModel(), new ElectionFormViewModel(), new KandydatViewModel(),
        ];

        Assert.All(emptyForms.SelectMany(Validate), r => Assert.DoesNotContain("field", r.ErrorMessage, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Voting_form_requires_a_candidate()
    {
        Assert.Contains(Validate(new KandydatViewModel { ElectionId = 1 }), r => r.MemberNames.Contains("SelectedCandidateId"));
        Assert.Empty(Validate(new KandydatViewModel { ElectionId = 1, SelectedCandidateId = 3 }));
    }
}
