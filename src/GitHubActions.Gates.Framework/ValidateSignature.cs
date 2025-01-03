using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;



[assembly: InternalsVisibleTo("GitHubActions.Gates.Framework.Tests")]
namespace GitHubActions.Gates.Framework
{
    public static class ValidateSignature
    {
        public static void ValidateSignatureIfConfigured(IConfiguration config, ILogger log, HttpRequest req, string requestBody)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (log is null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            if (req is null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            if (string.IsNullOrEmpty(requestBody))
            {
                throw new ArgumentException($"'{nameof(requestBody)}' cannot be null or empty.", nameof(requestBody));
            }

            string? secret = config[Config.WEBHOOKSECRETNAME];
            if (!string.IsNullOrEmpty(secret))
            {
                string? signature = req.Headers["X-Hub-Signature-256"].FirstOrDefault();
                if (string.IsNullOrEmpty(signature))
                {
                    log.LogError("No signature found. Ignoring request.");
                    throw new Exception("No signature found");
                }

                // Remove the sha256= prefix
                signature = signature[7..];

                // Compute the hash
                using var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
                var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(requestBody));
                var computedSignature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                if (signature != computedSignature)
                {
                    log.LogError("Invalid signature. Ignoring request.");
                    throw new Exception("Invalid signature");
                }
            }
        }
    }
}