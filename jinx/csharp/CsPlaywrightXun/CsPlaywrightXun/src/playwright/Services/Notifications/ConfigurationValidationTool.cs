using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Command-line tool for validating notification configuration files
    /// </summary>
    public class ConfigurationValidationTool
    {
        /// <summary>
        /// Validates a notification configuration file
        /// </summary>
        /// <param name="configFilePath">Path to the configuration file</param>
        /// <returns>True if validation passes, false otherwise</returns>
        public static bool ValidateConfigurationFile(string configFilePath)
        {
            Console.WriteLine($"Validating configuration file: {configFilePath}");
            Console.WriteLine(new string('-', 80));

            try
            {
                // Check if file exists
                if (!File.Exists(configFilePath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"ERROR: Configuration file not found: {configFilePath}");
                    Console.ResetColor();
                    return false;
                }

                // Load configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Path.GetDirectoryName(configFilePath) ?? Directory.GetCurrentDirectory())
                    .AddJsonFile(Path.GetFileName(configFilePath), optional: false, reloadOnChange: false)
                    .Build();

                // Create logger factory
                using var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                var logger = loggerFactory.CreateLogger<ConfigurationValidator>();
                var validator = new ConfigurationValidator(logger);

                // Validate configuration
                var result = validator.ValidateConfiguration(configuration);

                // Display results
                Console.WriteLine();
                if (result.IsValid)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ Configuration validation PASSED");
                    Console.ResetColor();
                    Console.WriteLine();
                    DisplayConfigurationSummary(configuration);
                    return true;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ Configuration validation FAILED with {result.Errors.Count} error(s):");
                    Console.ResetColor();
                    Console.WriteLine();

                    for (int i = 0; i < result.Errors.Count; i++)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  {i + 1}. {result.Errors[i]}");
                        Console.ResetColor();
                    }

                    Console.WriteLine();
                    return false;
                }
            }
            catch (JsonException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: Invalid JSON format: {ex.Message}");
                Console.ResetColor();
                return false;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: Unexpected error during validation: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }

        /// <summary>
        /// Displays a summary of the configuration
        /// </summary>
        /// <param name="configuration">Configuration to summarize</param>
        private static void DisplayConfigurationSummary(IConfiguration configuration)
        {
            Console.WriteLine("Configuration Summary:");
            Console.WriteLine(new string('-', 80));

            var notificationsSection = configuration.GetSection("Notifications");

            // Display enabled status
            var enabled = notificationsSection.GetValue<bool>("Enabled", true);
            Console.WriteLine($"Notifications Enabled: {enabled}");
            Console.WriteLine();

            // Display SMTP configuration
            var smtpSection = notificationsSection.GetSection("Smtp");
            if (smtpSection.Exists())
            {
                Console.WriteLine("SMTP Configuration:");
                Console.WriteLine($"  Host: {smtpSection["Host"]}");
                Console.WriteLine($"  Port: {smtpSection["Port"]}");
                Console.WriteLine($"  SSL Enabled: {smtpSection["EnableSsl"]}");
                Console.WriteLine($"  From Email: {smtpSection["FromEmail"]}");
                Console.WriteLine($"  From Display Name: {smtpSection["FromDisplayName"]}");
                Console.WriteLine($"  Timeout: {smtpSection["TimeoutSeconds"]} seconds");
                Console.WriteLine($"  Max Retry Attempts: {smtpSection["MaxRetryAttempts"]}");
                Console.WriteLine();
            }

            // Display default recipients
            var defaultRecipients = notificationsSection.GetSection("DefaultRecipients").Get<string[]>();
            if (defaultRecipients != null && defaultRecipients.Length > 0)
            {
                Console.WriteLine($"Default Recipients ({defaultRecipients.Length}):");
                foreach (var recipient in defaultRecipients)
                {
                    Console.WriteLine($"  - {recipient}");
                }
                Console.WriteLine();
            }

            // Display notification rules
            var rulesSection = notificationsSection.GetSection("Rules");
            if (rulesSection.Exists())
            {
                var rules = rulesSection.Get<NotificationRule[]>();
                if (rules != null && rules.Length > 0)
                {
                    Console.WriteLine($"Notification Rules ({rules.Length}):");
                    foreach (var rule in rules)
                    {
                        Console.WriteLine($"  - {rule.Id} ({rule.Type})");
                        Console.WriteLine($"    Enabled: {rule.IsEnabled}");
                        Console.WriteLine($"    Recipients: {rule.Recipients.Count}");
                        if (rule.CooldownPeriod.HasValue)
                        {
                            Console.WriteLine($"    Cooldown: {rule.CooldownPeriod.Value}");
                        }
                    }
                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// Main entry point for the validation tool
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static int Main(string[] args)
        {
            Console.WriteLine("Notification Configuration Validation Tool");
            Console.WriteLine("==========================================");
            Console.WriteLine();

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: ConfigurationValidationTool <config-file-path>");
                Console.WriteLine();
                Console.WriteLine("Examples:");
                Console.WriteLine("  ConfigurationValidationTool appsettings.Notifications.json");
                Console.WriteLine("  ConfigurationValidationTool src/config/appsettings.Notifications.Development.json");
                Console.WriteLine();
                return 1;
            }

            var configFilePath = args[0];
            var isValid = ValidateConfigurationFile(configFilePath);

            return isValid ? 0 : 1;
        }
    }
}
