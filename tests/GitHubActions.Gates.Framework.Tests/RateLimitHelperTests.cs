using GitHubActions.Gates.Framework.Clients;
using GitHubActions.Gates.Framework.Exceptions;
using GitHubActions.Gates.Framework.Helpers;
using GitHubActions.Gates.Framework.Tests.Helpers;
using GitHubActions.TestHelpers;
using Moq;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GitHubActions.Gates.Framework.Tests
{
    public class RateLimitHelperTests
    {
        [Fact]
        public void GetRateLimitReset_RateLimitedWithRetryAfter()
        {
            const string expectedResource = "UnitTest";
            var retryAfter = 30;
            var headers = new Dictionary<string, string>
                {
                    { "x-ratelimit-resource", expectedResource }, // this should be X-Ratelimit-Resource but this also tests case insensitive
                    {"Retry-After", retryAfter.ToString()},
                    {"X-RateLimit-Remaining", "42"}
                };

            var response = OctokitResponseFactory.CreateResponse(HttpStatusCode.Forbidden, "", headers);
            var rateExceededException = new RateLimitExceededException(response);

            var timeTolerance = 10; // Seconds. Because we are not mocking DateTime.UtcNow, we need to give some tolerance to the test.
            var expected = DateTime.UtcNow.AddSeconds(retryAfter);

            var reset = RateLimitHelper.GetRateLimitReset(rateExceededException.HttpResponse, 30);
            var resource = RateLimitHelper.GetResource(rateExceededException.HttpResponse);

            Assert.Equal(expectedResource, resource);
            Assert.InRange(reset, expected.AddSeconds(-timeTolerance), expected.AddSeconds(timeTolerance));
        }

        [Fact]
        public void GetRateLimitReset_RateLimitedWithReset()
        {
            const string expectedResource = "UnitTest";
            DateTime expectedReset = new(2030, 4, 6, 8, 4, 31);

            var headers = new Dictionary<string, string>
                {
                    { "x-ratelimit-resource", expectedResource }, // this should be X-Ratelimit-Resource but this also tests case insensitive
                    { "X-Ratelimit-Reset", "1901693071" },
                    {"X-RateLimit-Remaining", "42"}
                };

            var response = OctokitResponseFactory.CreateResponse(HttpStatusCode.Forbidden, "", headers);
            var rateExceededException = new RateLimitExceededException(response);

            var reset = RateLimitHelper.GetRateLimitReset(rateExceededException.HttpResponse, 30);
            var resource = RateLimitHelper.GetResource(rateExceededException.HttpResponse);

            Assert.Equal(expectedResource, resource);
            Assert.Equal(expectedReset, reset);
        }

        [Fact]
        public void GetRateLimitReset_RateLimitedWithResetAndRetry_ResetTakesPrecedence()
        {
            const string expectedResource = "UnitTest";
            DateTime expectedReset = new(2030, 4, 6, 8, 4, 31);

            var headers = new Dictionary<string, string>
                {
                    { "x-ratelimit-resource", expectedResource }, // this should be X-Ratelimit-Resource but this also tests case insensitive
                    {"Retry-After", "5"},
                    { "X-Ratelimit-Reset", "1901693071" },
                    {"X-RateLimit-Remaining", "42"}
                };

            var response = OctokitResponseFactory.CreateResponse(HttpStatusCode.Forbidden, "", headers);
            var rateExceededException = new RateLimitExceededException(response);

            var reset = RateLimitHelper.GetRateLimitReset(rateExceededException.HttpResponse, 30);
            var resource = RateLimitHelper.GetResource(rateExceededException.HttpResponse);

            Assert.Equal(expectedResource, resource);
            Assert.Equal(expectedReset, reset);
        }

        [Fact]
        public void GetRateLimitReset_NoHeaders_UseFallback()
        {
            const string expectedResource = "UnitTest";
            var expectedFallbackMinutes = 30;
            var headers = new Dictionary<string, string>
                {
                    { "x-ratelimit-resource", expectedResource }, // this should be X-Ratelimit-Resource but this also tests case insensitive
                };

            var response = OctokitResponseFactory.CreateResponse(HttpStatusCode.Forbidden, "", headers);
            var rateExceededException = new RateLimitExceededException(response);

            var timeTolerance = 10; // Seconds. Because we are not mocking DateTime.UtcNow, we need to give some tolerance to the test.
            var expected = DateTime.UtcNow.AddMinutes(expectedFallbackMinutes);

            var reset = RateLimitHelper.GetRateLimitReset(rateExceededException.HttpResponse, expectedFallbackMinutes);
            var resource = RateLimitHelper.GetResource(rateExceededException.HttpResponse);

            Assert.Equal(expectedResource, resource);
            Assert.InRange(reset, expected.AddSeconds(-timeTolerance), expected.AddSeconds(timeTolerance));
        }

    }
}
