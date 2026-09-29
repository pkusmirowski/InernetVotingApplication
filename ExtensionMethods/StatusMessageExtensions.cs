using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace InternetVotingApplication.ExtensionMethods;

/// <summary>
/// One-time status message shown after a redirect by <c>Views/Shared/_StatusMessage.cshtml</c>. Failures are
/// flagged so that the view renders them as an error, not as a success.
/// </summary>
public static class StatusMessageExtensions
{
    public const string MessageKey = "StatusMessage";
    public const string IsErrorKey = "StatusIsError";

    public static void SetStatus(this ITempDataDictionary tempData, string message, bool isError = false)
    {
        ArgumentNullException.ThrowIfNull(tempData);
        tempData[MessageKey] = message;
        if (isError)
        {
            tempData[IsErrorKey] = true;
        }
    }
}
