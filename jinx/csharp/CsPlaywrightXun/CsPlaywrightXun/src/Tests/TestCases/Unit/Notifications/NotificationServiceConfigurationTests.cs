using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CsPlaywrightXun.Core.Configuration;
using CsPlaywrightXun.Services.Notifications;
using Xunit;

namespace CsPlaywrightXun.Tests.Unit.Notifications
{
    /// <summary>
    /// Unit tests for notification service configuration
    /// </summary>
    public class NotificationServiceConfigurationTests
    {
        [Fact]
        public void AddNotificationServices_ShouldRegisterAllRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Notifications:Smtp:Host"] = "smtp.test.com",
                    ["Notifications:Smtp:Port"] = "587",
                    ["Notifications:Smtp:FromEmail"] = "test@test.com",
                    ["Notifications:Enabled"] = "true"
                })
                .Build();

            // Act
            services.AddNotificationServices(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            Assert.NotNull(serviceProvider.GetService<IEmailNotificationService>());
            Assert.NotNull(serviceProvider.GetService<ISmtpClient>());
            Assert.NotNull(serviceProvider.GetService<IEmailTemplateEngine>());
            Assert.NotNull(serviceProvider.GetService<IOptions<SmtpConfiguration>>());
            Assert.NotNull(serviceProvider.GetService<IOptions<NotificationSettings>>());
        }

        [Fact]
        public void SmtpConfiguration_ShouldBindFromConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Notifications:Smtp:Host"] = "smtp.example.com",
                    ["Notifications:Smtp:Port"] = "465",
                    ["Notifications:Smtp:EnableSsl"] = "true",
                    ["Notifications:Smtp:Username"] = "testuser",
                    ["Notifications:Smtp:FromEmail"] = "noreply@example.com",
                    ["Notifications:Smtp:FromDisplayName"] = "Test Automation"
                })
                .Build();

            // Act
            services.AddNotificationServices(configuration);
            var serviceProvider = services.BuildServiceProvider();
            var smtpConfig = serviceProvider.GetRequiredService<IOptions<SmtpConfiguration>>().Value;

            // Assert
            Assert.Equal("smtp.example.com", smtpConfig.Host);
            Assert.Equal(465, smtpConfig.Port);
            Assert.True(smtpConfig.EnableSsl);
            Assert.Equal("testuser", smtpConfig.Username);
            Assert.Equal("noreply@example.com", smtpConfig.FromEmail);
            Assert.Equal("Test Automation", smtpConfig.FromDisplayName);
        }

        [Fact]
        public void NotificationSettings_ShouldBindFromConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Notifications:Enabled"] = "true",
                    ["Notifications:DefaultRecipients:0"] = "admin@test.com",
                    ["Notifications:DefaultRecipients:1"] = "qa@test.com"
                })
                .Build();

            // Act
            services.AddNotificationServices(configuration);
            var serviceProvider = services.BuildServiceProvider();
            var notificationSettings = serviceProvider.GetRequiredService<IOptions<NotificationSettings>>().Value;

            // Assert
            Assert.True(notificationSettings.Enabled);
            Assert.Contains("admin@test.com", notificationSettings.DefaultRecipients);
            Assert.Contains("qa@test.com", notificationSettings.DefaultRecipients);
        }

        [Fact]
        public void NotificationSettings_IsValid_ShouldReturnFalseForEmptySettings()
        {
            // Arrange
            var settings = new NotificationSettings();

            // Act
            var isValid = settings.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void NotificationSettings_IsValid_ShouldReturnTrueForValidSettings()
        {
            // Arrange
            var settings = new NotificationSettings
            {
                Smtp = new SmtpConfiguration
                {
                    Host = "smtp.example.com",
                    Port = 587,
                    FromEmail = "test@example.com"
                },
                Rules = new List<NotificationRule>
                {
                    new NotificationRule
                    {
                        Type = NotificationType.TestFailure,
                        Recipients = { "admin@example.com" }
                    }
                }
            };

            // Act
            var isValid = settings.IsValid();

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void NotificationSettings_GetValidationErrors_ShouldReturnErrorsForInvalidSettings()
        {
            // Arrange
            var settings = new NotificationSettings
            {
                Smtp = null,
                Rules = new List<NotificationRule>
                {
                    new NotificationRule
                    {
                        Recipients = { "invalid-email" }
                    }
                },
                DefaultRecipients = { "invalid-default-email" }
            };

            // Act
            var errors = settings.GetValidationErrors();

            // Assert
            Assert.Contains("SMTP configuration is required", errors);
            Assert.Contains(errors, e => e.Contains("Recipient 'invalid-email' is not a valid email address"));
            Assert.Contains("Default recipient 'invalid-default-email' is not a valid email address", errors);
        }

        [Theory]
        [InlineData("", false)] // Empty host should fail validation
        [InlineData("smtp.test.com", true)] // Valid host should pass
        public void ValidateNotificationConfiguration_ShouldValidateSmtpHost(string host, bool shouldBeValid)
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Notifications:Smtp:Host"] = host,
                    ["Notifications:Smtp:Port"] = "587",
                    ["Notifications:Smtp:FromEmail"] = "test@test.com"
                })
                .Build();

            // Act
            services.AddNotificationServices(configuration);
            services.ValidateNotificationConfiguration();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            if (shouldBeValid)
            {
                var smtpConfig = serviceProvider.GetRequiredService<IOptions<SmtpConfiguration>>().Value;
                Assert.Equal(host, smtpConfig.Host);
            }
            else
            {
                Assert.Throws<OptionsValidationException>(() =>
                    serviceProvider.GetRequiredService<IOptions<SmtpConfiguration>>().Value);
            }
        }

        [Theory]
        [InlineData(0, false)] // Port 0 should fail
        [InlineData(587, true)] // Valid port should pass
        [InlineData(65536, false)] // Port above range should fail
        public void ValidateNotificationConfiguration_ShouldValidateSmtpPort(int port, bool shouldBeValid)
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Notifications:Smtp:Host"] = "smtp.test.com",
                    ["Notifications:Smtp:Port"] = port.ToString(),
                    ["Notifications:Smtp:FromEmail"] = "test@test.com"
                })
                .Build();

            // Act
            services.AddNotificationServices(configuration);
            services.ValidateNotificationConfiguration();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            if (shouldBeValid)
            {
                var smtpConfig = serviceProvider.GetRequiredService<IOptions<SmtpConfiguration>>().Value;
                Assert.Equal(port, smtpConfig.Port);
            }
            else
            {
                Assert.Throws<OptionsValidationException>(() =>
                    serviceProvider.GetRequiredService<IOptions<SmtpConfiguration>>().Value);
            }
        }

        [Theory]
        [InlineData(4, false)] // Timeout too low should fail
        [InlineData(30, true)] // Valid timeout should pass
        [InlineData(301, false)] // Timeout too high should fail
        public void ValidateNotificationConfiguration_ShouldValidateSmtpTimeout(int timeout, bool shouldBeValid)
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Notifications:Smtp:Host"] = "smtp.test.com",
                    ["Notifications:Smtp:Port"] = "587",
                    ["Notifications:Smtp:FromEmail"] = "test@test.com",
                    ["Notifications:Smtp:TimeoutSeconds"] = timeout.ToString()
                })
                .Build();

            // Act
            services.AddNotificationServices(configuration);
            services.ValidateNotificationConfiguration();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            if (shouldBeValid)
            {
                var smtpConfig = serviceProvider.GetRequiredService<IOptions<SmtpConfiguration>>().Value;
                Assert.Equal(timeout, smtpConfig.TimeoutSeconds);
            }
            else
            {
                Assert.Throws<OptionsValidationException>(() =>
                    serviceProvider.GetRequiredService<IOptions<SmtpConfiguration>>().Value);
            }
        }

        [Theory]
        [InlineData(-1, false)] // Negative retry attempts should fail
        [InlineData(3, true)] // Valid retry attempts should pass
        [InlineData(11, false)] // Too many retry attempts should fail
        public void ValidateNotificationConfiguration_ShouldValidateMaxRetryAttempts(int maxRetry, bool shouldBeValid)
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Notifications:Smtp:Host"] = "smtp.test.com",
                    ["Notifications:Smtp:Port"] = "587",
                    ["Notifications:Smtp:FromEmail"] = "test@test.com",
                    ["Notifications:Smtp:MaxRetryAttempts"] = maxRetry.ToString()
                })
                .Build();

            // Act
            services.AddNotificationServices(configuration);
            services.ValidateNotificationConfiguration();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            if (shouldBeValid)
            {
                var smtpConfig = serviceProvider.GetRequiredService<IOptions<SmtpConfiguration>>().Value;
                Assert.Equal(maxRetry, smtpConfig.MaxRetryAttempts);
            }
            else
            {
                Assert.Throws<OptionsValidationException>(() =>
                    serviceProvider.GetRequiredService<IOptions<SmtpConfiguration>>().Value);
            }
        }
    }
}