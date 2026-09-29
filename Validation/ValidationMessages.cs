using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace InternetVotingApplication.Validation;

/// <summary>
/// Polish replacements for the validation messages the framework would otherwise produce in English.
/// </summary>
public static class ValidationMessages
{
    /// <summary>For <c>[StringLength]</c>: {0} is the field name, {1} the maximum length.</summary>
    public const string TooLong = "To pole może mieć najwyżej {1} znaków.";

    /// <summary>Messages of model binding itself (a date or a number that cannot be read, a missing value).</summary>
    public static void UsePolish(DefaultModelBindingMessageProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        provider.SetMissingBindRequiredValueAccessor(_ => "To pole jest wymagane.");
        provider.SetMissingKeyOrValueAccessor(() => "To pole jest wymagane.");
        provider.SetMissingRequestBodyRequiredValueAccessor(() => "Formularz jest pusty. Wypełnij go i wyślij ponownie.");
        provider.SetValueMustNotBeNullAccessor(_ => "To pole jest wymagane.");
        provider.SetAttemptedValueIsInvalidAccessor((value, _) => $"Wartość „{value}” jest nieprawidłowa. Popraw ją i spróbuj ponownie.");
        provider.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"Wartość „{value}” jest nieprawidłowa. Popraw ją i spróbuj ponownie.");
        provider.SetUnknownValueIsInvalidAccessor(_ => "Ta wartość jest nieprawidłowa. Popraw ją i spróbuj ponownie.");
        provider.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Ta wartość jest nieprawidłowa. Popraw ją i spróbuj ponownie.");
        provider.SetValueIsInvalidAccessor(value => $"Wartość „{value}” jest nieprawidłowa. Popraw ją i spróbuj ponownie.");
        provider.SetValueMustBeANumberAccessor(_ => "W tym polu można wpisać tylko liczbę.");
        provider.SetNonPropertyValueMustBeANumberAccessor(() => "W tym polu można wpisać tylko liczbę.");
    }
}
