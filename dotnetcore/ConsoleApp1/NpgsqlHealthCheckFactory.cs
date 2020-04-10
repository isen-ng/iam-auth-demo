using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace HealthChecks.NpgSql
{
    public class NpgsqlHealthCheckFactory
    {
        public static HealthCheckRegistration Create(string name, string connectionString)
        {
            return new HealthCheckRegistration(name,
                p =>
                {
                    var tokenGenerator = p.GetRequiredService<AwsIamAuthTokenGenerator>();
                    var awsClientCertificateProvider = p.GetRequiredService<AwsClientCertificateProvider>();

                    return new NpgSqlHealthCheck(connectionString,
                        sql: "SELECT 1;",
                        c =>
                        {
                            c.ProvidePasswordCallback = tokenGenerator.GenerateAwsIamAuthToken;
                            c.UserCertificateValidationCallback =
                                awsClientCertificateProvider.UserCertificateValidationCallback;
                        });
                }, null, null, null);
        }
    }
}
