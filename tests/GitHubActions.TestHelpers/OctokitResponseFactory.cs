#pragma warning disable CS8604 // Possible null reference argument.
using GitHubActions.TestHelpers.Mocks;
using Octokit;
using Octokit.Internal;
using System.Net;

// Adapted from https://github.com/octokit/octokit.net/blob/main/Octokit.Tests/Helpers/TestSetup.cs#L38

namespace GitHubActions.TestHelpers
{
    public static class OctokitResponseFactory
    {
        public static Task<IApiResponse<T>> CreateApiResponse<T>(HttpStatusCode statusCode)
        {
            var response = CreateResponse(statusCode);
            return Task.FromResult<IApiResponse<T>>(new ApiResponse<T>(response));
        }

        public static Task<IApiResponse<T>> CreateApiResponse<T>(HttpStatusCode statusCode, T body)
        {
            var response = CreateResponse(statusCode, body);
            return Task.FromResult<IApiResponse<T>>(new ApiResponse<T>(response));
        }
        public static IResponse CreateResponse(HttpStatusCode statusCode)
        {
            return CreateResponse<object>(statusCode, null);
        }

        public static IResponse CreateResponse<T>(HttpStatusCode statusCode, T? body)
        {
            return new ResponseMock(statusCode, body, new Dictionary<string, string>(), "application/json");
        }

        public static IResponse CreateResponse<T>(HttpStatusCode statusCode, T body, IDictionary<string, string> headers)
        {
            return CreateResponse(statusCode, body, headers, "application/json");
        }

        public static IResponse CreateResponse<T>(HttpStatusCode statusCode, T body, IDictionary<string, string> headers, string contentType)
        {
            return new ResponseMock(statusCode, body, headers, contentType);
        }
    }
}
