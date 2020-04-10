using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Npgsql
{
    public class AwsClientCertificateProvider
    {
        private readonly Dictionary<string, X509Certificate2Collection> _caChains;
        private readonly ILogger<AwsClientCertificateProvider> _logger;

        public AwsClientCertificateProvider(ILogger<AwsClientCertificateProvider> logger)
        {
            _logger = logger;
            _caChains = new Dictionary<string, X509Certificate2Collection>();

            var directory = new DirectoryInfo("Certificates/");
            var pkcs7Files = directory.GetFiles("*.p7b");

            // some flavours (linux & windows) of dotnet only work with pkcs7 format, while OSX understands
            // pem and pkcs7. To avoid confusion, always convert pem to pkcs7 first
            // ca chain downloaded from https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/UsingWithRDS.SSL.html
            // or `wget https://s3.amazonaws.com/rds-downloads/rds-combined-ca-bundle.p7b`
            foreach (var file in pkcs7Files)
            {
                var c = new X509Certificate2Collection();
                c.Import(file.FullName);

                _caChains.Add(file.FullName, c);
                _logger.LogInformation("Loaded: {count} certificates from {certificate}", c.Count, file.FullName);
            }
        }

        /// <summary>
        /// This is the required method signature for NpgsqlConnection.RemoteCertificateValidationCallback
        /// </summary>
        public bool UserCertificateValidationCallback(object sender, X509Certificate serverCertificate,
            X509Chain serverChain, SslPolicyErrors error)
        {
            if (error == SslPolicyErrors.None)
            {
                return true;
            }

            // see https://github.com/dotnet/runtime/issues/26449#issuecomment-396407866
            // > The self-signed certificate must be registered as trusted on the system (e.g. in the LM\Root store).
            // this behaviour does not allow us to programmatically provide a trust anchor, and thus
            // we must resort to checking that last status, and last element of the chain
            var serverCert = new X509Certificate2(serverCertificate);

            foreach (var caChainEntry in _caChains)
            {
                var chain = new X509Chain {ChainPolicy = {RevocationMode = X509RevocationMode.NoCheck}};
                chain.ChainPolicy.ExtraStore.AddRange(caChainEntry.Value);

                chain.Build(serverCert);

                if (chain.ChainStatus.Length == 1 &&
                    chain.ChainStatus.First().Status == X509ChainStatusFlags.UntrustedRoot &&
                    chain.ChainPolicy.ExtraStore.Contains(
                        chain.ChainElements[chain.ChainElements.Count - 1].Certificate))
                {
                    // chain is valid, thus cert signed by root certificate
                    // and we expect that root is untrusted which the status flag tells us
                    // but we check that it is a known certificate
                    _logger.LogDebug("{certificate} validated against {ca}", serverCert.Subject, caChainEntry.Key);
                    return true;
                }

                _logger.LogDebug("{certificate} did not validate against {ca}", serverCert.Subject, caChainEntry.Key);
            }

            return false;
        }
    }
}
