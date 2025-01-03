using GitHubActions.Gates.Framework.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Text;

namespace GitHubActions.Gates.Framework.FunctionHandlers
{
    public class ValidationHandler
    {
        static readonly string[] settingsNamesList = new string[] { Config.GHAPPID, $"{Config.SERVICEBUSCONNECTIONNAME}:fullyQualifiedNamespace" };
        static readonly string[] secretNamesList = new string[] { Config.GHAPPPEMCERTIFICATENAME, Config.WEBHOOKSECRETNAME, Config.SERVICEBUSCONNECTIONNAME };

        protected ValidationHandler() { }

        protected static async Task<ContentResult> Validate(HttpRequest req, ILogger log)
        {
            log.LogInformation("Validate Settings");

            IConfiguration config = Config.GetConfig();
            StringBuilder htmlBody = new();

            GenerateSettingsList(htmlBody, config);
            GenerateDotNetInfo(htmlBody);

            await GenerateValidateInstallationToken(htmlBody, log, config, (string?)req.Query["installId"]);
            await GenerateAuthenticatedAppData(htmlBody, config, log);
            await GenerateRateLimit(htmlBody, config, log, (string?)req.Query["installId"]);

            return new ContentResult()
            {
                Content = $"<html><head></head><body>{htmlBody}</body></html>",
                ContentType = "text/html",                
                StatusCode = 200
            };
        }

        private static void GenerateDotNetInfo(StringBuilder htmlOutput)
        {
            htmlOutput.Append("<h2>.NET Version</h2>");
            htmlOutput.Append("<table>");
            htmlOutput.Append("<tr><th>Property</th><th>Value</th></tr>");
            htmlOutput.Append($"<tr><td>Version</td><td>{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}</td></tr>");
            htmlOutput.Append($"<tr><td>Runtime</td><td>{System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}</td></tr>");
            htmlOutput.Append($"<tr><td>Processor Arch</td><td>{System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}</td></tr>");
            htmlOutput.Append($"<tr><td>OS</td><td>{System.Runtime.InteropServices.RuntimeInformation.OSDescription} ({System.Runtime.InteropServices.RuntimeInformation.OSArchitecture})</td></tr>");
            htmlOutput.Append("</table>");
        }

        private static void GenerateSettingsList(StringBuilder htmlOutput, IConfiguration config)
        {
            htmlOutput.Append("<h2>Configuration Settings</h2><table><tr><th>Name</th><th>Present</th><th>value</th><th>length</th></tr>");
            foreach (var settingName in settingsNamesList)
            {
                var settingValue = config.GetValue<string>(settingName);
                htmlOutput.Append($"<tr><td>{settingName}</td><td>{!String.IsNullOrEmpty(settingValue)}</td><td>{settingValue}</td><td>{settingValue?.Length}</td></tr>");
            }
            foreach (var settingName in secretNamesList)
            {
                var settingValue = config.GetValue<string>(settingName);
                htmlOutput.Append($"<tr><td>{settingName}</td><td>{!String.IsNullOrEmpty(settingValue)}</td><td>*****</td><td>{settingValue?.Length}</td></tr>");
            }
            htmlOutput.Append("</table>");
        }

        private static async Task GenerateRateLimit(StringBuilder htmlBody, IConfiguration config, ILogger log, string? installationId)
        {
            htmlBody.Append("<h2>Rate Limits</h2>");

            if (!String.IsNullOrEmpty(installationId))
            {
                try
                {
                    var ghClient = new GitHubAppClient(long.Parse(installationId), log, config);
                    
                    var client = await ghClient.GetOCtokit();
                    var ratelimit = await client.RateLimit.GetRateLimits();

                    htmlBody.Append("<table><tr><th>Type</th><th>Limit</th><th>Used</th></tr>");
                    htmlBody.Append($"<tr><td>core</td><td>{ratelimit.Resources.Core.Limit}</td><td>{ratelimit.Resources.Core.Remaining}</td></tr>");
                    htmlBody.Append($"<tr><td>search</td><td>{ratelimit.Resources.Search.Limit}</td><td>{ratelimit.Resources.Search.Remaining}</td></tr>");
                    htmlBody.Append("</table>");
                }
                catch (Exception ex)
                {
                    htmlBody.Append($"Failed to get rate limit data. Check installation id :{ex.Message}");
                }
            }
            else
            {
                htmlBody.Append("Skipped rate limits. Provide a <strong>installId</strong> query string parameter to get rate limits for an installation.");
            }
        }

        private static async Task GenerateValidateInstallationToken(StringBuilder htmlBody, ILogger log, IConfiguration config, string? installationId)
        {
            if (htmlBody is null)
            {
                throw new ArgumentNullException(nameof(htmlBody));
            }

            if (log is null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            string installationValidation = "Skipped getting an installation token. Provide <strong>installId</strong> query string parameter to validate it as well.";
            if (!String.IsNullOrEmpty(installationId))
            {
                installationValidation = "Installation token generated sucessfully";

                try
                {
                    var ghClient = new GitHubAppClient(long.Parse(installationId), log, config);

                    string? installationToken = await ghClient.GetInstallationToken();

                    if (String.IsNullOrEmpty(installationToken))
                    {
                        installationValidation = "Couldn't generate an installation token. Please check the configuration values.";
                    }
                }
                catch (Exception ex)
                {
                    installationValidation = $"Couldn't generate an installation token: <strong>{ex.Message}</strong></br><br/>Tip: If it's the certificate make it's formatted in a single line with newlines escaped (local dev only)";
                }
            }
            htmlBody.Append("<h2>Installation Token</h2>");
            htmlBody.Append(installationValidation);
        }

        private static async Task GenerateAuthenticatedAppData(StringBuilder output, IConfiguration config, ILogger log)
        {
            var githubAppClient = new GitHubAppClient(0, log, config);

            output.Append("<h2>GitHub App</h2>");

            GitHubApp? app = null;
            try
            {
                var jwtClient = githubAppClient.GetOctokitWithJWT();
                app = await jwtClient.GitHubApps.GetCurrent();
            }
            catch (Exception ex)
            {
                output.Append($"<strong>Could not get app info {ex.Message}</strong>");
            }
            if (app != null)
            {
                output.Append("<table><tr><th>App</th><th></th></tr>");
                output.Append($"<tr><td>Name</td><td><a href='{app.HtmlUrl}'>{app.Name}</a></td></tr>");
                output.Append($"<tr><td>Description</td><td>{app.Description}</td></tr>");
                output.Append($"<tr><td>Url</td><td>{app.ExternalUrl}</td></tr>");
                output.Append($"<tr><td>Events</td><td>{string.Join(",", app.Events)}</td></tr>");
                output.Append($"<tr><td>Owner</td><td>{app.Owner.Login} ({app.Owner.Type})</td></tr>");
                output.Append("</table>");
            }
        }
    }
}
