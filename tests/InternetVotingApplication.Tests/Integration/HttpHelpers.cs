using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace InternetVotingApplication.Tests.Integration
{
    internal static partial class HttpHelpers
    {
        [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
        private static partial Regex TokenRegex();

        [GeneratedRegex("value=\"([^\"]+)\"[^>]*name=\"__RequestVerificationToken\"")]
        private static partial Regex TokenRegexReversed();

        /// <summary>Path and query of the Location header, whether it is absolute or relative.</summary>
        public static string LocationPath(this HttpResponseMessage response)
        {
            var location = response.Headers.Location;
            Assert.NotNull(location);
            return location.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString;
        }

        public static string ExtractAntiforgeryToken(string html)
        {
            var match = TokenRegex().Match(html);
            if (!match.Success)
            {
                match = TokenRegexReversed().Match(html);
            }

            Assert.True(match.Success, "Anti-forgery token not found in the page.");
            return match.Groups[1].Value;
        }

        /// <param name="url">Page that renders the form (source of the anti-forgery token).</param>
        /// <param name="postUrl">Where the form posts; defaults to <paramref name="url"/>.</param>
        public static async Task<HttpResponseMessage> PostFormAsync(this HttpClient client, string url, IDictionary<string, string> fields, string? postUrl = null)
        {
            var page = await client.GetAsync(new Uri(url, UriKind.Relative));
            page.EnsureSuccessStatusCode();
            var token = ExtractAntiforgeryToken(await page.Content.ReadAsStringAsync());

            var form = new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = token };
            using var content = new FormUrlEncodedContent(form);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            return await client.PostAsync(new Uri(postUrl ?? url, UriKind.Relative), content);
        }
    }
}
