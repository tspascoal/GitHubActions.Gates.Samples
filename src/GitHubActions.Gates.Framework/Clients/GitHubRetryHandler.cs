using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Octokit;
using Octokit.Internal;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Channels;
using GitHubActions.Gates.Framework.Exceptions;

// Code Derived From https://github.com/mirsaeedi/octokit.net.extensions/blob/master/src/Octokit.Extensions/Resiliency/GitHubResilientHandler.cs

namespace GitHubActions.Gates.Framework.Clients
{
    class GitHubRetryHandler : DelegatingHandler
    {
        // we need this instance to be able to call Octokit's internal code using reflection
        private static readonly Lazy<HttpClientAdapter> _httpClientAdapter = new(() => new HttpClientAdapter(() => new GitHubRetryHandler()), true);

        static readonly HttpStatusCode[] retryCodes =
        {
            HttpStatusCode.InternalServerError,
            HttpStatusCode.RequestTimeout,
            HttpStatusCode.GatewayTimeout,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable
        };

        private readonly ILogger _logger;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private GitHubRetryHandler()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }
        public GitHubRetryHandler(HttpMessageHandler innerHandler, ILogger logger)
        {
            InnerHandler = innerHandler;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var httpResponse = await ExecuteWithRetryAsync(async () =>
            {
                return await SendCoreAsync(request, cancellationToken);
            });

            var githubResponse = await GetGitHubResponse(httpResponse).ConfigureAwait(false);

            TryToThrowGitHubRelatedErrors(githubResponse);

            return httpResponse;
        }
        internal virtual async Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // should be more than enough to be unique within a function invocation
            string callID = Guid.NewGuid().ToString("N")[..6];

            _logger?.LogInformation($"NET Req: {callID} {request.Method.Method} {request.RequestUri}");

            // cannot use the cancelationToken because its timeout is preconfigured to 100 seconds by Octokit
            var httpResponse = await base.SendAsync(request, CancellationToken.None).ConfigureAwait(false);

            string? requestId = httpResponse.Headers?.GetValues("X-Github-Request-Id").FirstOrDefault();

            _logger?.LogInformation($"NET Res: {callID} {requestId} Status: {httpResponse.StatusCode}");

            return httpResponse;
        }

        private void TryToThrowGitHubRelatedErrors(dynamic? githubResponse)
        {
            if (githubResponse == null) return;

            MethodInfo? handleErrors = typeof(Connection).GetMethod("HandleErrors", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new FatalException("Fatal. Can't get access to octokit.net HandleErrors, downgrade version");

            try
            {
                handleErrors.Invoke(this, new object[] { githubResponse });
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException == null) throw;
                throw e.InnerException;
            }
        }

        private static async Task<IResponse?> GetGitHubResponse(HttpResponseMessage httpResponse)
        {
            MethodInfo? buildResponseMethod = typeof(HttpClientAdapter).GetMethod("BuildResponse", BindingFlags.NonPublic | BindingFlags.Instance) 
                ?? throw new FatalException("Fatal. Can't get access to octokit.net BuildResponse. Downgrade version");
            
            var clonedHttpResponse = await CloneResponseAsync(httpResponse).ConfigureAwait(false);

            var githubResponse = await (dynamic?)buildResponseMethod.Invoke(_httpClientAdapter.Value, new object[] { clonedHttpResponse });

            return githubResponse as IResponse;
        }

        private static async Task<HttpResponseMessage> CloneResponseAsync(HttpResponseMessage response)
        {
            var newResponse = new HttpResponseMessage(response.StatusCode);
            var ms = new MemoryStream();

            foreach (var v in response.Headers) newResponse.Headers.TryAddWithoutValidation(v.Key, v.Value);

            if (response.Content != null)
            {
                // need to call LoadIntoBuffer, otherwise Octokit complains that it can't read the stream
                await response.Content.LoadIntoBufferAsync().ConfigureAwait(false);
                await response.Content.CopyToAsync(ms).ConfigureAwait(false);

                ms.Position = 0;
                newResponse.Content = new StreamContent(ms);
                foreach (var v in response.Content.Headers) newResponse.Content.Headers.TryAddWithoutValidation(v.Key, v.Value);

            }

            return newResponse;
        }
        internal virtual async Task<HttpResponseMessage> ExecuteWithRetryAsync(Func<Task<HttpResponseMessage>> action, int maxRetries = 3, int sleepTime = 2)
        {
            int retryNumber = 0;
            while (true)
            {
                var actionResult = await action();

                if (retryCodes.Contains(actionResult.StatusCode))
                {
                    retryNumber++;
                    if (retryNumber > maxRetries)
                    {
                        return actionResult;
                    }

                    _logger.LogInformation($"Retrying request {retryNumber} of {maxRetries} after {sleepTime} seconds");

                    await Task.Delay((int)Math.Pow(sleepTime, retryNumber) * 1000);
                }
                else
                {
                    return actionResult;
                }
            }
        }
    }
}