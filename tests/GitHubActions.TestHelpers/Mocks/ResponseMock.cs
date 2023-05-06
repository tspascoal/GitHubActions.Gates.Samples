#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Octokit;
using System.Collections.ObjectModel;
using System.Net;


namespace GitHubActions.TestHelpers.Mocks
{
    internal class ResponseMock : IResponse
    {
        public ResponseMock() : this(new Dictionary<string, string>())
        {
        }

        public ResponseMock(IDictionary<string, string> headers)
        {
            Headers = new ReadOnlyDictionary<string, string>(headers);
            ApiInfo = ApiInfoParser.ParseResponseHeaders(headers);
        }

        public ResponseMock(HttpStatusCode statusCode, object body, IDictionary<string, string> headers, string contentType)
        {
            StatusCode = statusCode;
            Body = body;
            Headers = new ReadOnlyDictionary<string, string>(headers);
            ApiInfo = ApiInfoParser.ParseResponseHeaders(headers);
            ContentType = contentType;
        }

        /// <summary>
        /// Raw response body. Typically a string, but when requesting images, it will be a byte array.
        /// </summary>
        public object Body { get; private set; }
        /// <summary>
        /// Information about the API.
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers { get; private set; }
        /// <summary>
        /// Information about the API response parsed from the response headers.
        /// </summary>
        public ApiInfo ApiInfo { get; internal set; } // This setter is internal for use in tests.
        /// <summary>
        /// The response status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }
        /// <summary>
        /// The content type of the response.
        /// </summary>
        public string ContentType { get; private set; }
    }
}
