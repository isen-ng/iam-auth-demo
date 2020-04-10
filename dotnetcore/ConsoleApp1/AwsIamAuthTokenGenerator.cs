using System;
using Amazon.RDS.Util;
using Microsoft.Extensions.Logging;

namespace Npgsql
{
    public class AwsIamAuthTokenGenerator
    {
        private readonly ILogger<AwsIamAuthTokenGenerator> _logger;
        private readonly string _usernamePrefix;

        public AwsIamAuthTokenGenerator(string usernamePrefix, ILogger<AwsIamAuthTokenGenerator> logger)
        {
            if (string.IsNullOrWhiteSpace(usernamePrefix))
            {
                throw new ArgumentException($"{nameof(usernamePrefix)} cannot be null or empty");
            }

            _usernamePrefix = usernamePrefix;
            _logger = logger;
        }

        /// <summary>
        /// This is the required method signature for NpgsqlConnection.ProviderPasswordCallback
        /// </summary>
        public string GenerateAwsIamAuthToken(string host, int port, string database, string username)
        {
            if (username.StartsWith(_usernamePrefix))
            {
                _logger.LogInformation("Generating iam auth token for {username}", username);
                return RDSAuthTokenGenerator.GenerateAuthToken(host, port, username);
            }
            else
            {
                _logger.LogDebug("Skip generating iam auth token because {username} does not match {prefix}", username,
                    _usernamePrefix);

                return null;
            }
        }
    }
}
