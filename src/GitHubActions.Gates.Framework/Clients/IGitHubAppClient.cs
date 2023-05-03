using Octokit;
using System.Net;

namespace GitHubActions.Gates.Framework.Clients
{
    public interface IGitHubAppClient
    {
        Task<HttpStatusCode> Approve(string callbackUrl, string environmentName, string? comment = "");
        Task<HttpStatusCode> Reject(string callbackUrl, string environmentName, string? comment);

        Task<string?> GetInstallationToken();
        Task<dynamic?> GraphQLAsync(string query, object variables);
        Task ReportUpdate(string callbackUrl, string environmentName, string comment);
                
        /// <summary>
        /// Get an octokit instance that uses an installation token that can be used to make calls
        /// against a specific installation.
        /// </summary>
        /// <returns></returns>
        Task<IGitHubClient> GetOCtokit();
        /// <summary>
        /// Get an octokit instance that can be use to call GitHub apps API.
        /// </summary>
        /// <returns></returns>
        IGitHubClient GetOctokitWithJWT();        
    }
}