using Octokit;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GitHubActions.TestHelpers.Mocks
{
    internal static class ApiInfoParser
    {
        public const string ReceivedTimeHeaderName = "X-Octokit-ReceivedDate";

        const RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;

        static readonly Regex _linkRelRegex = new("rel=\"(next|prev|first|last)\"", regexOptions);
        static readonly Regex _linkUriRegex = new("<(.+)>", regexOptions);

        static KeyValuePair<string, string> LookupHeader(IDictionary<string, string> headers, string key)
        {
            return headers.FirstOrDefault(h => string.Equals(h.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        static bool Exists(KeyValuePair<string, string> kvp)
        {
            return !kvp.Equals(default(KeyValuePair<string, string>));
        }

        public static ApiInfo ParseResponseHeaders(IDictionary<string, string> responseHeaders)
        {
            var httpLinks = new Dictionary<string, Uri>();
            var oauthScopes = new List<string>();
            var acceptedOauthScopes = new List<string>();
            string? etag = null;

            var acceptedOauthScopesKey = LookupHeader(responseHeaders, "X-Accepted-OAuth-Scopes");
            if (Exists(acceptedOauthScopesKey))
            {
                acceptedOauthScopes.AddRange(acceptedOauthScopesKey.Value
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()));
            }

            var oauthScopesKey = LookupHeader(responseHeaders, "X-OAuth-Scopes");
            if (Exists(oauthScopesKey))
            {
                oauthScopes.AddRange(oauthScopesKey.Value
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()));
            }

            var etagKey = LookupHeader(responseHeaders, "ETag");
            if (Exists(etagKey))
            {
                etag = etagKey.Value;
            }

            var linkKey = LookupHeader(responseHeaders, "Link");
            if (Exists(linkKey))
            {
                var links = linkKey.Value.Split(',');
                foreach (var link in links)
                {
                    var relMatch = _linkRelRegex.Match(link);
                    if (!relMatch.Success || relMatch.Groups.Count != 2) break;

                    var uriMatch = _linkUriRegex.Match(link);
                    if (!uriMatch.Success || uriMatch.Groups.Count != 2) break;

                    httpLinks.Add(relMatch.Groups[1].Value, new Uri(uriMatch.Groups[1].Value));
                }
            }

            var receivedTimeKey = LookupHeader(responseHeaders, ReceivedTimeHeaderName);
            var serverTimeKey = LookupHeader(responseHeaders, "Date");
            TimeSpan serverTimeSkew = TimeSpan.Zero;
            if (DateTimeOffset.TryParse(receivedTimeKey.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var receivedTime)
                && DateTimeOffset.TryParse(serverTimeKey.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var serverTime))
            {
                serverTimeSkew = serverTime - receivedTime;
            }

            return new ApiInfo(httpLinks, oauthScopes, acceptedOauthScopes, etag, new RateLimit(responseHeaders), serverTimeSkew);
        }
    }
}
