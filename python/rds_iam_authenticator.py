import logging

import boto3
from psycopg2 import extensions as ext

# when psycopg2 creates a connection with the dns and kwargs, it evaluates the kwargs into strings
# by using `str(arg)`. This allows us to pass in an object with its `__str__` overridden such that
# the evaluation of the AWS RDS IAM Auth token is deferred to the moment the connection is created.
# This is especially important for connection pools because connection creation may be deferred to
# a later time. By also deferring the AWS RDS IAM Auth token creation, we ensure that connections
# created at any point in the application always has a valid token.


class RdsIAmAuthDsnKwargsSupplier:
    def __init__(self, dsn, region):
        self.logger = logging.getLogger(__name__ + "." + self.__class__.__name__)

        parsed = ext.parse_dsn(dsn)
        user = parsed.get("user")
        password = parsed.get("password")

        if not password and user and user.startswith("iam_"):
            self.logger.info("Using RDS dns kwargs supplier because password is empty and username starts with iam_")
            self.kwargs = {
                'password': RdsIamAuthTokenGenerator(dsn, region),
                'sslmode': RdsIamAuthSslModeProvider(dsn),
                'sslrootcert': RdsIamAuthSslRootProvider(dsn)
            }
        else:
            self.logger.info("Not using RDS dns kwargs supplier because password is not empty or "
                             "username does not start with iam_")
            self.kwargs = {}

    def get_dsn_kwargs(self):
        return self.kwargs


class RdsIamAuthTokenGenerator:
    def __init__(self, dsn, region):
        parsed = ext.parse_dsn(dsn)
        self.host = parsed.get("host", "localhost")
        self.port = parsed.get("port", "5432")
        self.user = parsed.get("user", "postgres")

        self.client = boto3.client('rds', region_name=region)
        self.logger = logging.getLogger(__name__ + "." + self.__class__.__name__)

    def __str__(self):
        self.logger.debug("Generating iam auth token ...")
        return self.client.generate_db_auth_token(
            DBHostname=self.host,
            Port=self.port,
            DBUsername=self.user)


class RdsIamAuthSslModeProvider:
    def __init__(self, dsn):
        parsed = ext.parse_dsn(dsn)
        self.ssl_mode = parsed.get("sslmode", "require")
        self.logger = logging.getLogger(__name__ + "." + self.__class__.__name__)

    def __str__(self):
        self.logger.debug("Using ssl_mode[%s]", self.ssl_mode)
        return self.ssl_mode


class RdsIamAuthSslRootProvider:
    def __init__(self, dsn):
        parsed = ext.parse_dsn(dsn)

        # ca chain downloaded from https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/UsingWithRDS.SSL.html
        # or `wget https: //s3.amazonaws.com/rds-downloads/rds-combined-ca-bundle.pem`
        self.ssl_root_cert = parsed.get("sslrootcert", "./certificates/rds-combined-ca-bundle.pem")
        self.logger = logging.getLogger(__name__ + "." + self.__class__.__name__)

    def __str__(self):
        self.logger.debug("Using ssl_root_cert[%s]", self.ssl_root_cert)
        return self.ssl_root_cert
