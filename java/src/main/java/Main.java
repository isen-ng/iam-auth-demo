import com.amazonaws.auth.DefaultAWSCredentialsProviderChain;
import com.amazonaws.services.rds.auth.GetIamAuthTokenRequest;
import com.amazonaws.services.rds.auth.RdsIamAuthTokenGenerator;
import com.zaxxer.hikari.HikariConfig;
import com.zaxxer.hikari.HikariDataSource;

import java.sql.Connection;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.time.Duration;
import java.util.Timer;
import java.util.TimerTask;

public class Main {
    private static void ConnectUsingPassword(String host, int port, String username, String password) {
        HikariConfig config = new HikariConfig();
        config.setJdbcUrl(String.format("jdbc:postgresql://%s:%d/postgres", host, port));
        config.setUsername(username);
        config.setPassword(password);

        HikariDataSource dataSource = new HikariDataSource(config);

        try (Connection connection = dataSource.getConnection();
             Statement statement = connection.createStatement();
             ResultSet resultSet = statement.executeQuery("SELECT 1")) {

            resultSet.next();
            System.out.println(resultSet.getInt(1));
        } catch (SQLException ex) {
            System.out.println(ex);
        }
    }

    private static Timer ConnectUsingIamAuth(String host, int port, String username) {
        HikariConfig config = new HikariConfig();
        config.setJdbcUrl(String.format("jdbc:postgresql://%s:%d/postgres", host, port));
        config.setUsername(username);
        config.setPassword(generateRdsPassword(host, port, username));
        config.addDataSourceProperty("sslmode", "require");
        config.addDataSourceProperty("sslrootcert", "rds-combined-ca-bundle.pem");

        HikariDataSource dataSource = new HikariDataSource(config);

        // periodically update the password at runtime
        // this is required because https://github.com/brettwooldridge/HikariCP/pull/1335
        Timer periodicUpdater = new Timer();
        periodicUpdater.scheduleAtFixedRate(
                new TimerTask() {
                    public void run() {
                        // this MXBean is only available when HikariDataSource is manually constructed
                        dataSource.getHikariConfigMXBean()
                                .setPassword(generateRdsPassword(host, port, username));
                    }
                },
                Duration.ofMinutes(10).toMillis(),      // run first occurrence immediately
                Duration.ofMinutes(10).toMillis());

        try (Connection connection = dataSource.getConnection();
             Statement statement = connection.createStatement();
             ResultSet resultSet = statement.executeQuery("SELECT 2")) {

            resultSet.next();
            System.out.println(resultSet.getInt(1));
        } catch (SQLException ex) {
            System.out.println(ex);
        }

        return periodicUpdater;
    }

    private static String generateRdsPassword(String host, int port, String username) {
        RdsIamAuthTokenGenerator rdsTokenGenerator = RdsIamAuthTokenGenerator.builder()
                .credentials(new DefaultAWSCredentialsProviderChain())
                .region("ap-southeast-1")
                .build();

        return rdsTokenGenerator.getAuthToken(new GetIamAuthTokenRequest(host, port, username));
    }

    public static void main(String[] args) {
        String host = "database-1.cqfs7g34hr3m.ap-southeast-1.rds.amazonaws.com";
        int port = 5432;

        String username = "postgres";
        String password = "abcd1234";

        ConnectUsingPassword(host, port, username, password);

        String iamUsername = "iam_postgres";

        Timer updater = ConnectUsingIamAuth(host, port, iamUsername);
        updater.cancel();
    }
}
