
using Moq;
using Newtonsoft.Json;
using Octokit;

namespace GitHubActions.TestHelpers.Assert
{
    public class GitHubApiAssert
    {
        public static void AssertGraphQLCall(Mock<IGitHubClient> octoClientMock, string graphQLQuery, object graphQLVars)
        {
            var graphQLRequestBody = new { query = graphQLQuery, variables = graphQLVars };

            octoClientMock.Verify(
                o => o.Connection.Post<object>(
                        new Uri("https://api.github.com/graphql"),
                        It.Is<object>(body => JsonConvert.SerializeObject(body) == JsonConvert.SerializeObject(graphQLRequestBody)),
                        "application/vnd.github+json",
                        "application/json",
                        It.IsAny<IDictionary<string, string>>(),  // parameters
                        It.IsAny<CancellationToken>()),
                    Times.Once);
        }
    }
}
