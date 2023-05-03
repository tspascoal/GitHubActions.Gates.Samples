using Octokit;

namespace GitHubActions.Gates.Framework.Helpers
{
    internal static class RateLimitHelper
    {
        internal static string GetResource(IResponse responseFromException)
        {
            return TryGetHeader(responseFromException.Headers, "X-Ratelimit-Resource");
        }

        /// <summary>
        /// Calculates next reset time from the response headers. Only to be used if the exception is missing this value
        /// </summary>
        /// <param name="responseFromException"></param>
        /// <param name="fallBackMinutes"></param>
        /// <returns></returns>
        internal static DateTime GetRateLimitReset(IResponse responseFromException, int fallBackMinutes)
        {
            if (responseFromException == null) return DateTime.UtcNow.AddMinutes(fallBackMinutes);

            string? rateLimitReset = TryGetHeader(responseFromException.Headers, "X-Ratelimit-Reset");
            var retryAfter = TryGetHeader(responseFromException.Headers, "Retry-After");

            if (rateLimitReset != null)
            {
                if (String.IsNullOrWhiteSpace(rateLimitReset) || !long.TryParse(rateLimitReset, out long parsedRateLimitReset))
                {
                    return DateTime.UtcNow.AddMinutes(fallBackMinutes);
                }
                if (parsedRateLimitReset < 0) return DateTime.UtcNow.AddMinutes(fallBackMinutes);

                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(parsedRateLimitReset);
            }
            else if (retryAfter != null)
            {
                if (String.IsNullOrWhiteSpace(retryAfter) || !int.TryParse(retryAfter, out int retrySeconds))
                {
                    return DateTime.UtcNow.AddMinutes(fallBackMinutes);
                }

                if (retrySeconds < 0) return DateTime.UtcNow.AddMinutes(fallBackMinutes);

                return DateTime.UtcNow.AddSeconds(Double.Parse(retryAfter));
            }
            return DateTime.UtcNow.AddMinutes(fallBackMinutes);
        }
        /// <summary>
        /// Fetches a header (case insensitive)
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="headerName"></param>
        /// <returns></returns>
        private static string TryGetHeader(IReadOnlyDictionary<string, string> headers, string headerName)
        {
            return headers.FirstOrDefault(h => string.Equals(h.Key, headerName, StringComparison.OrdinalIgnoreCase)).Value;
        }
    }
}
