using System;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace ConsoleApp1
{
    class Program
    {
        private static void ConnectUsingPassword(string host, int port, string username, string password)
        {
            var connectionString = $"Host={host};Port={port};Database=postgres;Username={username};Password={password}";

            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "Select 1;";
            var result = command.ExecuteScalar();

            Console.WriteLine(result);
        }

        private static void ConnectUsingIamAuth(string host, int port, string username)
        {
            var connectionString = $"Host={host};Port={port};Database=postgres;Username={username};SslMode=Require";

            using var connection = new NpgsqlConnection(connectionString)
            {
                UserCertificateValidationCallback =
                    new AwsClientCertificateProvider(NullLogger<AwsClientCertificateProvider>.Instance)
                        .UserCertificateValidationCallback,
                ProvidePasswordCallback =
                    new AwsIamAuthTokenGenerator("iam_", NullLogger<AwsIamAuthTokenGenerator>.Instance)
                        .GenerateAwsIamAuthToken
            };

            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "Select 2;";
            var result = command.ExecuteScalar();

            Console.WriteLine(result);
        }

        static void Main(string[] args)
        {
            var host = "database-1.cqfs7g34hr3m.ap-southeast-1.rds.amazonaws.com";
            var port = 5432;

            var username = "postgres";
            var password = "abcd1234";

            ConnectUsingPassword(host, port, username, password);

            var iamUsername = "iam_postgres";

            ConnectUsingIamAuth(host, port, iamUsername);
        }
    }
}