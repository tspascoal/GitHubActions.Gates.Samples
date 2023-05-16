using JWT.Algorithms;
using JWT.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography;


using System.Runtime.CompilerServices;
using ProductHeaderValue = Octokit.ProductHeaderValue;
using Octokit;
using Octokit.Internal;
using GitHubActions.Gates.Framework.Exceptions;
using System.Reflection;
using Newtonsoft.Json;
using Microsoft.CSharp.RuntimeBinder;

// Needed for unit tests
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GitHubActions.Gates.Framework.Clients
{
    public class GitHubAppClient : IGitHubAppClient
    {
        readonly IConfiguration? _config;
        string _installationToken;
        readonly long _installationId;
        readonly ILogger _log;

        IGitHubClient _octoKit;
        IGitHubClient _octoKitJWT;

        private const string baseUrl = "https://api.github.com";
        private const string AcceptValue = "application/vnd.github+json";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GitHubAppClient(long installationId, ILogger logger, IConfiguration? config = null)

        {
            _config = config ?? Config.GetConfig();
            _installationId = installationId;
            _log = logger;
        }

        internal GitHubAppClient(IGitHubClient octokit, long installationId, ILogger logger, IConfiguration? config = null) : this(installationId, logger, config)
        {
            _octoKit = octokit;
            _octoKitJWT = octokit;
        }

        private GitHubAppClient() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public async Task<IGitHubClient> GetOCtokit()
        {
            if (_octoKit != null)
            {
                return _octoKit;
            }

            var installationToken = await GetInstallationToken();

            var githubConnection = new Connection(CreateProductHeader(),
              new HttpClientAdapter(() => GetHttpHandlerChain(_log)))
            {
                Credentials = new Octokit.Credentials(installationToken),
            };

            var githubClient = new GitHubClient(githubConnection);


            return _octoKit = githubClient;
        }

        private ProductHeaderValue CreateProductHeader()
        {
            string version = typeof(GitHubAppClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                             ?? "sourcecode";
            // TODO: we should do a better job cleaning up version just in case
            return new ProductHeaderValue($"GatesSample-{_installationId}", version.Replace("/", "_"));
        }

        public virtual IGitHubClient GetOctokitWithJWT()
        {
            if (_octoKitJWT != null)
            {
                return _octoKitJWT;
            }

            var jwtToken = CreateJWTToken(_config![Config.GHAPPPEMCERTIFICATENAME], _config![Config.GHAPPID]);

            var githubConnection = new Connection(CreateProductHeader(),
                  new HttpClientAdapter(() => GetHttpHandlerChain(_log)))
            {

                Credentials = new Octokit.Credentials(jwtToken, AuthenticationType.Bearer)
            };

            return _octoKitJWT = new GitHubClient(githubConnection);
        }

        public async Task ReportUpdate(string callbackUrl, string? environmentName, string? comment)
        {
            var payload = new
            {
                environment_name = environmentName,
                comment
            };

            _log.LogInformation($"Reporting Update {environmentName} {callbackUrl} with comment: {comment}");
            var client = await GetOCtokit();

            await client.Connection.Post(new Uri(callbackUrl), payload, AcceptValue);

        }

        public virtual async Task<HttpStatusCode> Reject(string callbackUrl, string environmentName, string? comment)
        {
            _log.LogInformation($"Reject {environmentName} {callbackUrl} {comment}");
            return await SetApprovalDecision(callbackUrl, "rejected", environmentName, comment);
        }

        public virtual async Task<HttpStatusCode> Approve(string callbackUrl, string environmentName, string? comment = "")
        {
            _log.LogInformation($"Approve {environmentName} {callbackUrl} {comment}");
            return await SetApprovalDecision(callbackUrl, "approved", environmentName, comment);
        }

        internal virtual async Task<HttpStatusCode> SetApprovalDecision(string callbackUrl, string? state, string? environmentName, string? comment = "")
        {

            var payload = new
            {
                state,
                environment_name = environmentName,
                comment = comment ?? ""
            };

            var client = await GetOCtokit();

            return await client.Connection.Post(new Uri(callbackUrl), payload, AcceptValue);
        }


        private static string CreateJWTToken(string? pemCertificate, string? appId)
        {
            if (string.IsNullOrEmpty(appId)) throw new ArgumentException("appId is null or empty", nameof(appId));
            if (string.IsNullOrEmpty(pemCertificate)) throw new ArgumentException("pemCertificate is null or empty", nameof(pemCertificate));

            var rsa = RSA.Create();
            rsa.ImportFromPem(pemCertificate);

            return JwtBuilder.Create()
                      .WithAlgorithm(new RS256Algorithm(rsa, rsa))
                      .AddClaim("exp", DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds())
                      .AddClaim("iss", appId)
                      .AddClaim("iat", Math.Round((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds))
                      .Encode();
        }

        public virtual async Task<string?> GetInstallationToken()
        {
            if (_installationToken != null) return _installationToken;

            var githubClient = GetOctokitWithJWT();

            AccessToken token;
            try
            {
                token = await githubClient.GitHubApps.CreateInstallationToken(_installationId);
            }
            catch (Exception e)
            {
                _log.LogError($"Fatal. Can't get installation token. {e.Message} ({e.GetType().Name})");
                throw new FatalException($"Can't get installation token. {e.Message}. Validate settings.");
            }

            return _installationToken = token.Token;
        }

        public async Task<dynamic?> GraphQLAsync(string? query, object variables)
        {
            var payload = new
            {
                query,
                variables
            };

            var octoClient = await GetOCtokit();

            var response = await octoClient.Connection.Post<string>(new Uri($"{baseUrl}/graphql"), payload, AcceptValue, "application/json");

            if (response == null || response.HttpResponse?.Body == null)
            {
                throw new Exception("No Response from API call");
            }

            dynamic? data = JsonConvert.DeserializeObject<dynamic>((string)response.HttpResponse.Body);

            HandleGraphQLErrors(data);

            return data;
        }

        private static void HandleGraphQLErrors(dynamic? responseData)
        {
            if (responseData != null)
            {
                try
                {
                    var errors = responseData.errors;

                    if (errors != null)
                    {
                        List<string> errorsResult = new();

                        foreach (var error in errors)
                        {
                            errorsResult.Add((string)error.message);
                        }

                        throw new GraphQLException($"API call failed with errors", errorsResult);
                    }
                }
                catch (RuntimeBinderException)
                {
                    //  errors property doesn't exist
                }
            }
        }

        private static HttpMessageHandler GetHttpHandlerChain(ILogger logger)
        {
            var handler = HttpMessageHandlerFactory.CreateDefault();

            return new GitHubRetryHandler(handler, logger);
        }
    }
}
