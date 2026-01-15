using FsCheck;
using FsCheck.Xunit;
using CsPlaywrightXun.Services.Notifications;
using System.Net.Mail;
using System.Linq;

namespace CsPlaywrightXun.Tests.Unit.Notifications
{
    /// <summary>
    /// Property-based tests for SMTP configuration models
    /// Feature: notification-integration, Property 6: SMTP配置存储和使用
    /// </summary>
    public class SmtpConfigurationPropertyTests
    {
        /// <summary>
        /// Generator for valid SMTP hostnames
        /// </summary>
        public static Arbitrary<string> ValidHostnames()
        {
            return Arb.From(
                Gen.Elements(new[]
                {
                    "smtp.gmail.com",
                    "smtp.outlook.com", 
                    "smtp.yahoo.com",
                    "mail.example.com",
                    "localhost",
                    "127.0.0.1",
                    "smtp-server.domain.com"
                })
            );
        }

        /// <summary>
        /// Generator for valid port numbers
        /// </summary>
        public static Arbitrary<int> ValidPorts()
        {
            return Arb.From(Gen.Choose(1, 65535));
        }

        /// <summary>
        /// Generator for valid email addresses
        /// </summary>
        public static Arbitrary<string> ValidEmails()
        {
            return Arb.From(
                Gen.Elements(new[]
                {
                    "test@example.com",
                    "admin@company.org",
                    "noreply@domain.net",
                    "support@service.io",
                    "notifications@test.local"
                })
            );
        }

        /// <summary>
        /// Generator for valid timeout values
        /// </summary>
        public static Arbitrary<int> ValidTimeouts()
        {
            return Arb.From(Gen.Choose(5, 300));
        }

        /// <summary>
        /// Generator for valid retry attempts
        /// </summary>
        public static Arbitrary<int> ValidRetryAttempts()
        {
            return Arb.From(Gen.Choose(0, 10));
        }

        /// <summary>
        /// Generator for valid display names
        /// </summary>
        public static Arbitrary<string> ValidDisplayNames()
        {
            return Arb.From(
                Gen.Elements(new[]
                {
                    "Test Automation",
                    "QA Team",
                    "CI/CD System",
                    "Notification Service",
                    ""
                })
            );
        }

        /// <summary>
        /// Generator for valid usernames
        /// </summary>
        public static Arbitrary<string> ValidUsernames()
        {
            return Arb.From(
                Gen.Elements(new[]
                {
                    "testuser",
                    "admin",
                    "service_account",
                    "automation@example.com",
                    ""
                })
            );
        }

        /// <summary>
        /// Generator for valid passwords
        /// </summary>
        public static Arbitrary<string> ValidPasswords()
        {
            return Arb.From(
                Gen.Elements(new[]
                {
                    "password123",
                    "SecureP@ssw0rd",
                    "TestPassword!",
                    "",
                    "simple"
                })
            );
        }

        /// <summary>
        /// Property 6: SMTP配置存储和使用
        /// For any valid SMTP configuration (server address, port, authentication info, encryption settings, sender info),
        /// the system should be able to correctly store and use these configurations when sending emails
        /// **Validates: Requirements 2.1, 2.2, 2.3, 2.4**
        /// </summary>
        [Property(Arbitrary = new[] { 
            typeof(SmtpConfigurationPropertyTests) 
        })]
        public Property ValidSmtpConfiguration_ShouldBeStoredAndUsedCorrectly()
        {
            return Prop.ForAll(
                ValidHostnames(),
                ValidPorts(),
                ValidEmails(),
                (validHost, validPort, validFromEmail) =>
                {
                    // Arrange
                    var config = new SmtpConfiguration
                    {
                        Host = validHost,
                        Port = validPort,
                        EnableSsl = true,
                        Username = "testuser",
                        Password = "testpass",
                        FromEmail = validFromEmail,
                        FromDisplayName = "Test Service",
                        TimeoutSeconds = 30,
                        MaxRetryAttempts = 3
                    };

                    // Act & Assert
                    var isValid = config.IsValid();
                    var validationErrors = config.GetValidationErrors();

                    // Property: Valid configurations should pass validation
                    return isValid.Label("Configuration should be valid")
                           .And((validationErrors.Count == 0).Label("Should have no validation errors"))
                           .And((config.Host == validHost).Label("Host should be stored correctly"))
                           .And((config.Port == validPort).Label("Port should be stored correctly"))
                           .And((config.FromEmail == validFromEmail).Label("FromEmail should be stored correctly"));
                });
        }

        /// <summary>
        /// Property: Invalid host configurations should fail validation
        /// **Validates: Requirements 2.1**
        /// </summary>
        [Property]
        public Property InvalidHost_ShouldFailValidation(string invalidHost)
        {
            return Prop.ForAll(
                Gen.Elements(new[] { "", "   ", null }).ToArbitrary(),
                (host) =>
                {
                    // Arrange
                    var config = new SmtpConfiguration
                    {
                        Host = host ?? "",
                        Port = 587,
                        FromEmail = "test@example.com"
                    };

                    // Act
                    var isValid = config.IsValid();
                    var validationErrors = config.GetValidationErrors();

                    // Assert
                    return (!isValid).Label("Invalid host should fail validation")
                           .And((validationErrors.Any(e => e.Contains("Host"))).Label("Should contain host validation error"));
                });
        }

        /// <summary>
        /// Property: Invalid port configurations should fail validation
        /// **Validates: Requirements 2.1**
        /// </summary>
        [Property]
        public Property InvalidPort_ShouldFailValidation()
        {
            return Prop.ForAll(
                Gen.Elements(new[] { 0, -1, 65536, 100000 }).ToArbitrary(),
                (invalidPort) =>
                {
                    // Arrange
                    var config = new SmtpConfiguration
                    {
                        Host = "smtp.example.com",
                        Port = invalidPort,
                        FromEmail = "test@example.com"
                    };

                    // Act
                    var isValid = config.IsValid();
                    var validationErrors = config.GetValidationErrors();

                    // Assert
                    return (!isValid).Label("Invalid port should fail validation")
                           .And((validationErrors.Any(e => e.Contains("Port"))).Label("Should contain port validation error"));
                });
        }

        /// <summary>
        /// Property: Invalid email addresses should fail validation
        /// **Validates: Requirements 2.4**
        /// </summary>
        [Property]
        public Property InvalidFromEmail_ShouldFailValidation()
        {
            return Prop.ForAll(
                Gen.Elements(new[] { "", "invalid-email", "test@", "@example.com", "test.example.com", "test@.com" }).ToArbitrary(),
                (invalidEmail) =>
                {
                    // Arrange
                    var config = new SmtpConfiguration
                    {
                        Host = "smtp.example.com",
                        Port = 587,
                        FromEmail = invalidEmail
                    };

                    // Act
                    var isValid = config.IsValid();
                    var validationErrors = config.GetValidationErrors();

                    // Assert
                    return (!isValid).Label("Invalid email should fail validation")
                           .And((validationErrors.Any(e => e.Contains("Email") || e.Contains("required"))).Label("Should contain email validation error"));
                });
        }

        /// <summary>
        /// Property: Invalid timeout values should fail validation
        /// **Validates: Requirements 2.1**
        /// </summary>
        [Property]
        public Property InvalidTimeout_ShouldFailValidation()
        {
            return Prop.ForAll(
                Gen.Elements(new[] { 4, 0, -1, 301, 1000 }).ToArbitrary(),
                (invalidTimeout) =>
                {
                    // Arrange
                    var config = new SmtpConfiguration
                    {
                        Host = "smtp.example.com",
                        Port = 587,
                        FromEmail = "test@example.com",
                        TimeoutSeconds = invalidTimeout
                    };

                    // Act
                    var isValid = config.IsValid();
                    var validationErrors = config.GetValidationErrors();

                    // Assert
                    return (!isValid).Label("Invalid timeout should fail validation")
                           .And((validationErrors.Any(e => e.Contains("Timeout"))).Label("Should contain timeout validation error"));
                });
        }

        /// <summary>
        /// Property: Invalid retry attempts should fail validation
        /// **Validates: Requirements 2.1**
        /// </summary>
        [Property]
        public Property InvalidRetryAttempts_ShouldFailValidation()
        {
            return Prop.ForAll(
                Gen.Elements(new[] { -1, -5, 11, 20 }).ToArbitrary(),
                (invalidRetry) =>
                {
                    // Arrange
                    var config = new SmtpConfiguration
                    {
                        Host = "smtp.example.com",
                        Port = 587,
                        FromEmail = "test@example.com",
                        MaxRetryAttempts = invalidRetry
                    };

                    // Act
                    var isValid = config.IsValid();
                    var validationErrors = config.GetValidationErrors();

                    // Assert
                    return (!isValid).Label("Invalid retry attempts should fail validation")
                           .And((validationErrors.Any(e => e.Contains("retry"))).Label("Should contain retry validation error"));
                });
        }

        /// <summary>
        /// Property: SSL/TLS configuration should be stored correctly
        /// **Validates: Requirements 2.3**
        /// </summary>
        [Property]
        public Property SslConfiguration_ShouldBeStoredCorrectly(bool enableSsl)
        {
            // Arrange
            var config = new SmtpConfiguration
            {
                Host = "smtp.example.com",
                Port = 587,
                FromEmail = "test@example.com",
                EnableSsl = enableSsl
            };

            // Act & Assert
            return (config.EnableSsl == enableSsl).Label("SSL setting should be stored correctly")
                   .And(config.IsValid().Label("Valid SSL configuration should pass validation"));
        }

        /// <summary>
        /// Property: Authentication configuration should be stored correctly
        /// **Validates: Requirements 2.2**
        /// </summary>
        [Property]
        public Property AuthenticationConfiguration_ShouldBeStoredCorrectly()
        {
            return Prop.ForAll(
                ValidUsernames(),
                ValidPasswords(),
                (username, password) =>
                {
                    // Arrange
                    var config = new SmtpConfiguration
                    {
                        Host = "smtp.example.com",
                        Port = 587,
                        FromEmail = "test@example.com",
                        Username = username,
                        Password = password
                    };

                    // Act & Assert
                    return (config.Username == username).Label("Username should be stored correctly")
                           .And((config.Password == password).Label("Password should be stored correctly"))
                           .And(config.IsValid().Label("Valid authentication configuration should pass validation"));
                });
        }

        /// <summary>
        /// Property: Configuration round-trip should preserve all values
        /// **Validates: Requirements 2.1, 2.2, 2.3, 2.4**
        /// </summary>
        [Property]
        public Property ConfigurationRoundTrip_ShouldPreserveAllValues()
        {
            return Prop.ForAll(
                ValidHostnames(),
                ValidPorts(),
                ValidEmails(),
                (host, port, fromEmail) =>
                {
                    // Arrange
                    var originalConfig = new SmtpConfiguration
                    {
                        Host = host,
                        Port = port,
                        EnableSsl = true,
                        Username = "testuser",
                        Password = "testpass",
                        FromEmail = fromEmail,
                        FromDisplayName = "Test Service",
                        TimeoutSeconds = 30,
                        MaxRetryAttempts = 3
                    };

                    // Act - Simulate storing and retrieving configuration
                    var storedConfig = new SmtpConfiguration
                    {
                        Host = originalConfig.Host,
                        Port = originalConfig.Port,
                        EnableSsl = originalConfig.EnableSsl,
                        Username = originalConfig.Username,
                        Password = originalConfig.Password,
                        FromEmail = originalConfig.FromEmail,
                        FromDisplayName = originalConfig.FromDisplayName,
                        TimeoutSeconds = originalConfig.TimeoutSeconds,
                        MaxRetryAttempts = originalConfig.MaxRetryAttempts
                    };

                    // Assert - All values should be preserved
                    return (storedConfig.Host == originalConfig.Host).Label("Host should be preserved")
                           .And((storedConfig.Port == originalConfig.Port).Label("Port should be preserved"))
                           .And((storedConfig.EnableSsl == originalConfig.EnableSsl).Label("SSL setting should be preserved"))
                           .And((storedConfig.Username == originalConfig.Username).Label("Username should be preserved"))
                           .And((storedConfig.Password == originalConfig.Password).Label("Password should be preserved"))
                           .And((storedConfig.FromEmail == originalConfig.FromEmail).Label("FromEmail should be preserved"))
                           .And((storedConfig.FromDisplayName == originalConfig.FromDisplayName).Label("FromDisplayName should be preserved"))
                           .And((storedConfig.TimeoutSeconds == originalConfig.TimeoutSeconds).Label("Timeout should be preserved"))
                           .And((storedConfig.MaxRetryAttempts == originalConfig.MaxRetryAttempts).Label("MaxRetryAttempts should be preserved"));
                });
        }

        /// <summary>
        /// Property: Authentication and SSL configuration should be stored correctly
        /// **Validates: Requirements 2.2, 2.3**
        /// </summary>
        [Property]
        public Property AuthenticationAndSslConfiguration_ShouldBeStoredCorrectly()
        {
            return Prop.ForAll(
                ValidUsernames(),
                ValidPasswords(),
                Arb.Default.Bool(),
                (username, password, enableSsl) =>
                {
                    // Arrange
                    var config = new SmtpConfiguration
                    {
                        Host = "smtp.example.com",
                        Port = 587,
                        FromEmail = "test@example.com",
                        Username = username,
                        Password = password,
                        EnableSsl = enableSsl
                    };

                    // Act & Assert
                    return (config.Username == username).Label("Username should be stored correctly")
                           .And((config.Password == password).Label("Password should be stored correctly"))
                           .And((config.EnableSsl == enableSsl).Label("SSL setting should be stored correctly"))
                           .And(config.IsValid().Label("Valid configuration should pass validation"));
                });
        }

        /// <summary>
        /// Property: Timeout and retry configuration should be stored correctly
        /// **Validates: Requirements 2.1**
        /// </summary>
        [Property]
        public Property TimeoutAndRetryConfiguration_ShouldBeStoredCorrectly()
        {
            return Prop.ForAll(
                ValidTimeouts(),
                ValidRetryAttempts(),
                (timeout, retry) =>
                {
                    // Arrange
                    var config = new SmtpConfiguration
                    {
                        Host = "smtp.example.com",
                        Port = 587,
                        FromEmail = "test@example.com",
                        TimeoutSeconds = timeout,
                        MaxRetryAttempts = retry
                    };

                    // Act & Assert
                    return (config.TimeoutSeconds == timeout).Label("Timeout should be stored correctly")
                           .And((config.MaxRetryAttempts == retry).Label("MaxRetryAttempts should be stored correctly"))
                           .And(config.IsValid().Label("Valid configuration should pass validation"));
                });
        }
    }
}