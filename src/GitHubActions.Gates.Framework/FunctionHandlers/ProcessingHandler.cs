using GitHubActions.Gates.Framework.Clients;
using GitHubActions.Gates.Framework.Exceptions;
using GitHubActions.Gates.Framework.Helpers;
using GitHubActions.Gates.Framework.Models;
using GitHubActions.Gates.Framework.Models.WebHooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Globalization;

namespace GitHubActions.Gates.Framework.FunctionHandlers
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">The configuration file type</typeparam>
    public abstract class ProcessingHandler<T, R> where T : IGatesConfiguration<R>, new() where R : IGatesRule
    {
        protected virtual string Name { get; }
        protected virtual T GateConfiguration { get; private set; }
        protected IGitHubAppClient GitHubClient { get; private set; }
        protected IConfiguration FunctionConfig { get; private set; }
        protected virtual string? ConfigPath { get; }
        protected ILogger Log { get; set; }

        /// <summary>
        /// Outcome for this run. If null no outcome has been set on this run.
        /// </summary>
        private GateOutcome? Outcome { get; set; }

        private DeploymentProtectionRuleWebHook? webHookPayload;
        private string? ProcessingQueueName { get; set; }
        private EventMessage EventMessage { get; set; }

        private const string MissingConfigFileMessage = "Sorry I'm rejecting this. I can't proceed, couldn't retrieve the config file {0}. Error: {1}";
        private const string ErrorParsingConfigFileMessage = "Sorry I'm rejecting this. The {0} file doesn't seem to be valid. Check if the YAML file is valid and it respect the configuration format. Error: {1}";
        private readonly int RetryIfNotSpecifiedSeconds = 30; // The value to retry in case of being rate limited and no header was returned.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>
        ///  For testing only
        /// </summary>
        /// <param name="client"></param>
        /// <param name="payload"></param>
        /// <param name="GateName"></param>
        /// <param name="QueueName"></param>
        /// <param name="ConfigFilePath"></param>
        protected ProcessingHandler(IGitHubAppClient client, DeploymentProtectionRuleWebHook payload, ILogger log, string GateName, string QueueName, string? ConfigFilePath = null) : this(GateName, QueueName, ConfigFilePath)
        {
            GitHubClient = client;
            webHookPayload = payload;
            Log = log;
        }
        protected ProcessingHandler(string GateName, string QueueName, string? ConfigFilePath = null)
        {
            Name = GateName;
            ProcessingQueueName = QueueName;
            ConfigPath = ConfigFilePath;
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// 
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        protected virtual async Task ProcessProcessing(EventMessage message, ILogger log)
        {
            ArgumentNullException.ThrowIfNull(log);
            ArgumentNullException.ThrowIfNull(message);
            
            Log = log;
            EventMessage = message;

            webHookPayload = message.WebHookPayload;
            if (webHookPayload == null)
            {
                Log.LogError("Webhook payload is null");
                throw new Exception("webhook payload is null");
            }

            // TODO: log run id once we have it (also add repo as well)
            Log.LogInformation($"Starting {Name} Gate Callback: {message.WebHookPayload.deployment_callback_url}  Try: {message.TryNumber} Has Outcome: {message.Outcome != null}");

            FunctionConfig = Config.GetConfig();
            GitHubClient ??= new GitHubAppClient(webHookPayload.installation.id, Log, FunctionConfig);

            var repoOwner = webHookPayload.repository.owner.login;
            var repoName = webHookPayload.repository.name;

            try
            {
                // If we already have an outcome we don't need to process everything again.
                try
                {
                    if (message.Outcome != null)
                    {
                        await ProcessOutcome(message.Outcome);
                    }
                    else
                    {
                        await LoadConfiguration(repoOwner, repoName);
                        log.LogInformation($"Processing {Name}");

                        // If a delay was applied we can't call the gate processing
                        if (!await TryApplyDelayIfConfigured())
                        {
                            await Process(webHookPayload);
                        }
                    }
                }
                catch (ApiException ex) when (ex is RateLimitExceededException || ex is AbuseException || ex is SecondaryRateLimitExceededException)
                {
                    await HandleRateLimiting("inner", ex);
                }
                catch (RejectException ex)
                {
                    Log.LogInformation($"Received RejectException from {Name}");
                    await Reject(ex.Message);

                }
                catch (FatalException ex)
                {
                    Log.LogError(ex, $"Fatal {ex.Message}. Giving up (event will be declared as OK to be removed from queue).");
                }
                catch (Exception ex)
                {
                    Log.LogError(ex, $"Error unexpectly Received {ex.GetType().FullName} from {Name} with {ex.Message} {ex.StackTrace}.");
                    await Reject(ex.Message);
                }

                Log.LogInformation($"Finished {Name} Gate Callback: {message.WebHookPayload.deployment_callback_url}  Env: {message.WebHookPayload.environment} Try: {message.TryNumber} With Outcome={Outcome?.State}");
            }
            catch (ApiException ex) when (ex is RateLimitExceededException || ex is AbuseException || ex is SecondaryRateLimitExceededException)
            {
                await HandleRateLimiting("outer", ex);
            }
        }

        private async Task HandleRateLimiting(string label, ApiException ex)
        {
            var resource = RateLimitHelper.GetResource(ex.HttpResponse);

            Log.LogInformation($"Handling rate limit {label} {ex.GetType().Name} for {resource}");
            var retryIn = RateLimitHelper.GetRateLimitReset(ex.HttpResponse, RetryIfNotSpecifiedSeconds);

            await EnQueueProcessing(retryIn);
        }

        /// <summary>
        /// If a delay is configured
        /// </summary>
        /// <returns>True if the processing was delayed false otherwise</returns>
        private async Task<bool> TryApplyDelayIfConfigured()
        {
            if (EventMessage.Delayed || GateConfiguration == null)
            {
                return false;
            }
            var rule = GateConfiguration.GetRule(webHookPayload!.environment);

            if (rule != null && ((IGatesRule)rule)!.WaitMinutes > 0)
            {
                EventMessage.Delayed = true;
                await EnQueueProcessing(DateTime.UtcNow.AddMinutes(rule.WaitMinutes));

                return true;
            }
            return false;
        }

        protected virtual async Task EnQueueProcessing(DateTime queueTime)
        {
            var sbClient = new ServiceBusClient(FunctionConfig);

            Log.LogInformation($"enqueuing {Name} {queueTime:f}");

            // Inject the message in the queue to be processed later again after rate limit resets
            EventMessage.TryNumber++;
            if (EventMessage?.RemainingTries == 1) return;

            EventMessage!.RemainingTries--;

            // Inject Outcome if it has been already issue. This means if a gate does something after an outcome is set and is rate limited,
            // those steps will never run
            // But it's an acceptable tradeoff since an outcome is most likely the last thing to happen.
            EventMessage.Outcome = Outcome;
            await sbClient.SendMessage(ProcessingQueueName!, EventMessage, queueTime);
        }

        private async Task ProcessOutcome(GateOutcome outcome)
        {
            Log.LogInformation($"Processing Previous Outcome {outcome.State} {outcome.Comment} with schedule {outcome.Schedule:f}");
            if (outcome.State == OutcomeState.Approved)
            {
                await Approve(outcome.Comment, outcome.Schedule);
            }
            else if (outcome.State == OutcomeState.Rejected)
            {
                await Reject(outcome.Comment);
            }
            else
            {
                throw new Exception($"This shouldn't happen. Unknown outcome {outcome.State}");
            }
        }

        /// <summary>
        /// Loads the configuration from GitHub (if configured) and stores in property
        /// 
        /// If there is an error loading the file or the has validation errors the gate is rejected.
        /// 
        /// </summary>
        /// <param name="repoFullName"></param>
        /// <returns></returns>
        /// <exception cref="RejectException"></exception>
        internal async Task LoadConfiguration(string repoOwner, string repoName)
        {
            if (ConfigPath != null)
            {
                Log.LogInformation($"Loading {ConfigPath} for {Name}");

                IReadOnlyList<Octokit.RepositoryContent> configFiles;
                try
                {
                    var client = await GitHubClient!.GetOCtokit();
                    configFiles = await client.Repository.Content.GetAllContents(repoOwner, repoName, ConfigPath);
                }
                catch (ApiException ex) when (ex is RateLimitExceededException || ex is AbuseException || ex is SecondaryRateLimitExceededException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (ex is FatalException) throw;

                    Log.LogError(ex, $"Error getting file: {ex.Message} {ex.GetType().Name}");

                    throw new RejectException(String.Format(CultureInfo.InvariantCulture, MissingConfigFileMessage, ConfigPath, ex.Message));
                }

                var configFile = configFiles[0];
                Log.LogInformation($"Parsing {ConfigPath}@{configFile.Sha} with {configFile.Size} bytes");

                try
                {
                    GateConfiguration = new T();
                    GateConfiguration!.Load(configFile.Content);
                }
                catch (Exception ex)
                {
                    Log.LogError(ex, $"Error parsing yaml file. Error: {ex.Message} ({ex.GetType().Name})");
                    throw new RejectException(String.Format(CultureInfo.InvariantCulture, ErrorParsingConfigFileMessage, ConfigPath, ex.Message));
                }

                var validationErrors = GateConfiguration!.Validate();
                if (validationErrors?.Count > 0)
                {
                    // It would be nice if we pointed to the SHA and not the fetched branch. But this requires url rewriting,
                    // opted to avoid coupling with UI urls
                    throw new RejectException($"Config file [{ConfigPath}]({configFile.HtmlUrl}) is not valid:\n{GateConfiguration.GenerateMarkdownErrorList(validationErrors)}");
                }
            }
            else
            {
                Log.LogDebug("Not reading configuration file from GitHub");
            }
        }

        protected abstract Task Process(DeploymentProtectionRuleWebHook payload);

        /// <summary>
        /// Approves the gate with an optional comment
        /// 
        /// The approval can also be time delayed
        /// </summary>
        /// <param name="comment"></param>
        /// <param name="approvalTime"></param>
        /// <returns></returns>
        public virtual async Task Approve(string? comment = null, DateTime? approvalTime = null)
        {
            var properties = CreateMetricProperties();

            if (approvalTime != null)
            {
                await EnQueueProcessing(approvalTime!.Value);
                Log.LogMetric("Approved Queued", 1, properties);
                return;
            }

            Outcome = new GateOutcome
            {
                Comment = comment,
                State = OutcomeState.Approved
            };
            try
            {
                await GitHubClient.Approve(webHookPayload!.deployment_callback_url, webHookPayload.environment, comment);
                Log.LogMetric("Approved", 1, properties);
            }
            catch (ApiException ex) when (ex is RateLimitExceededException || ex is AbuseException || ex is SecondaryRateLimitExceededException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.LogError(ex, $"Error accepting {webHookPayload?.deployment_callback_url} {ex.Message}. Ignored ({ex.GetType()})");
            }
        }

        public virtual async Task Reject(string? comment = null)
        {
            var properties = CreateMetricProperties();

            Outcome = new GateOutcome
            {
                Comment = comment,
                State = OutcomeState.Rejected
            };

            // TODO: send run attempt when we have it
            try
            {
                await GitHubClient.Reject(webHookPayload!.deployment_callback_url, webHookPayload.environment, comment);
                Log.LogMetric("Rejected", 1, properties);
            }
            catch (ApiException ex) when (ex is RateLimitExceededException || ex is AbuseException || ex is SecondaryRateLimitExceededException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.LogError(ex, $"Error rejecting {webHookPayload?.deployment_callback_url} {ex.Message}. Ignored ({ex.GetType()})");
            }
        }
        public virtual async Task AddComment(string comment)
        {
            try
            {
                await GitHubClient.ReportUpdate(webHookPayload!.deployment_callback_url, webHookPayload.environment, comment);
            }
            catch (ApiException ex) when (ex is RateLimitExceededException || ex is AbuseException || ex is SecondaryRateLimitExceededException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.LogError(ex, $"Error adding comment {webHookPayload?.deployment_callback_url} {ex.Message}. Ignored ({ex.GetType()})");
            }
        }

        protected Repo GetRepository()
        {
            return new Repo(webHookPayload!.repository.full_name);
        }

        /// <summary>
        ///  TEMPORARY SOLUTION UNTIL THE PAYLOAD OFFERS THE RUNID
        /// </summary>
        /// <param name="callbackUrl"></param>
        /// <returns>The run id of the workflow run being processed</returns>
        protected long GetRunID()
        {
            return long.Parse(GetRunID(webHookPayload?.deployment_callback_url));
        }

        /// <summary>
        ///  TEMPORARY SOLUTION UNTIL THE PAYLOAD OFFERS THE RUNID
        /// </summary>
        /// <param name="callbackUrl"></param>
        /// <returns></returns>
        internal static string GetRunID(string? callbackUrl)
        {
            ArgumentNullException.ThrowIfNull(callbackUrl);
            if (String.IsNullOrWhiteSpace(callbackUrl)) throw new ArgumentException("Value cannot be empty", nameof(callbackUrl));
            // Get run id from callback url 
            // sample https://api.github.com/repos/monae/gates/actions/runs/4493385896/deployment_protection_rule

            var uri = new Uri(callbackUrl);
            string[] segments = uri.AbsolutePath.Split("/");

            return segments[6];
        }

        private Dictionary<string, object> CreateMetricProperties()
        {
            var repository = GetRepository();
            return new Dictionary<string, object>
            {
                { "Environment", webHookPayload!.environment },
                { "RunId", GetRunID() },
                { "Owner",  repository.Owner },
                { "Repo", repository.Name}
            };
        }
    }
}
