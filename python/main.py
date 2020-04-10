import aiopg
import psycopg2

from rds_iam_authenticator import RdsIAmAuthDsnKwargsSupplier


def connect_using_password(host, port, user, password):
    dsn = "dbname='postgres' host='{}' port='{}' user='{}' password='{}'".format(host, port, user, password)

    with psycopg2.connect(dsn) as conn:
        with conn.cursor() as cursor:
            cursor.execute("SELECT 1")
            print(cursor.fetchone())


def connect_using_iam_auth_token(host, port, user,):
    dsn = "dbname='postgres' host='{}' port='{}' user='{}'".format(host, port, user)
    dsn_kwargs = RdsIAmAuthDsnKwargsSupplier(dsn, "ap-southeast-1").get_dsn_kwargs()

    pool = await aiopg.create_pool(dsn, **dsn_kwargs)

    async with pool.acquire() as conn:
        async with conn.cursor() as cursor:
            await cursor.execute("SELECT 2")
            print(await cursor.fetchone())


if __name__ == "__main__":
    my_host = "database-1.cqfs7g34hr3m.ap-southeast-1.rds.amazonaws.com"
    my_port = 5432

    my_user = "postgres"
    my_password = "abcd1234"

    connect_using_password(my_host, my_port, my_user, my_password)

    my_iam_user = "iam_postgres"
    connect_using_iam_auth_token(my_host, my_user, my_iam_user)
