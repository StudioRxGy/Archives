#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates notification configuration files

.DESCRIPTION
    This script validates notification configuration files for the CsPlaywrightXun framework.
    It checks JSON syntax, required fields, email formats, and SMTP settings.

.PARAMETER ConfigFile
    Path to the configuration file to validate

.PARAMETER Verbose
    Show detailed validation information

.EXAMPLE
    .\Validate-NotificationConfig.ps1 -ConfigFile appsettings.Notifications.json

.EXAMPLE
    .\Validate-NotificationConfig.ps1 -ConfigFile appsettings.Notifications.Production.json -Verbose
#>

param(
    [Parameter(Mandatory=$true, HelpMessage="Path to the configuration file")]
    [string]$ConfigFile,
    
    [Parameter(Mandatory=$false)]
    [switch]$Verbose
)

# Color output functions
function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Error-Message {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

function Write-Warning-Message {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ $Message" -ForegroundColor Cyan
}

# Validation functions
function Test-EmailAddress {
    param([string]$Email)
    
    if ([string]::IsNullOrWhiteSpace($Email)) {
        return $false
    }
    
    try {
        $null = [System.Net.Mail.MailAddress]::new($Email)
        return $true
    }
    catch {
        return $false
    }
}

function Test-PortNumber {
    param([int]$Port)
    return ($Port -ge 1 -and $Port -le 65535)
}

function Test-TimeoutValue {
    param([int]$Timeout)
    return ($Timeout -ge 5 -and $Timeout -le 300)
}

function Test-RetryAttempts {
    param([int]$Attempts)
    return ($Attempts -ge 0 -and $Attempts -le 10)
}

# Main validation
Write-Host ""
Write-Host "Notification Configuration Validator" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Check if file exists
if (-not (Test-Path $ConfigFile)) {
    Write-Error-Message "Configuration file not found: $ConfigFile"
    exit 1
}

Write-Info "Validating: $ConfigFile"
Write-Host ""

$errors = @()
$warnings = @()

try {
    # Load JSON
    $config = Get-Content $ConfigFile -Raw | ConvertFrom-Json
    Write-Success "JSON syntax is valid"
    
    # Check Notifications section
    if (-not $config.Notifications) {
        $errors += "Notifications section not found"
    }
    else {
        Write-Success "Notifications section found"
        
        # Validate SMTP configuration
        if (-not $config.Notifications.Smtp) {
            $errors += "SMTP configuration not found"
        }
        else {
            Write-Success "SMTP configuration found"
            
            $smtp = $config.Notifications.Smtp
            
            # Validate Host
            if ([string]::IsNullOrWhiteSpace($smtp.Host)) {
                $errors += "SMTP Host is required"
            }
            elseif ($smtp.Host -match '\$\{.*\}') {
                Write-Warning-Message "SMTP Host uses environment variable: $($smtp.Host)"
            }
            else {
                Write-Success "SMTP Host: $($smtp.Host)"
            }
            
            # Validate Port
            if (-not (Test-PortNumber $smtp.Port)) {
                $errors += "SMTP Port must be between 1 and 65535 (current: $($smtp.Port))"
            }
            else {
                Write-Success "SMTP Port: $($smtp.Port)"
            }
            
            # Validate FromEmail
            if ([string]::IsNullOrWhiteSpace($smtp.FromEmail)) {
                $errors += "From Email is required"
            }
            elseif ($smtp.FromEmail -match '\$\{.*\}') {
                Write-Warning-Message "From Email uses environment variable: $($smtp.FromEmail)"
            }
            elseif (-not (Test-EmailAddress $smtp.FromEmail)) {
                $errors += "From Email is not a valid email address: $($smtp.FromEmail)"
            }
            else {
                Write-Success "From Email: $($smtp.FromEmail)"
            }
            
            # Validate Timeout
            if ($smtp.TimeoutSeconds -and -not (Test-TimeoutValue $smtp.TimeoutSeconds)) {
                $errors += "Timeout must be between 5 and 300 seconds (current: $($smtp.TimeoutSeconds))"
            }
            elseif ($smtp.TimeoutSeconds) {
                Write-Success "Timeout: $($smtp.TimeoutSeconds) seconds"
            }
            
            # Validate MaxRetryAttempts
            if ($smtp.MaxRetryAttempts -and -not (Test-RetryAttempts $smtp.MaxRetryAttempts)) {
                $errors += "Max retry attempts must be between 0 and 10 (current: $($smtp.MaxRetryAttempts))"
            }
            elseif ($smtp.MaxRetryAttempts) {
                Write-Success "Max Retry Attempts: $($smtp.MaxRetryAttempts)"
            }
            
            # Check SSL
            if ($smtp.EnableSsl -eq $false) {
                Write-Warning-Message "SSL/TLS is disabled (not recommended for production)"
            }
            else {
                Write-Success "SSL/TLS: Enabled"
            }
        }
        
        # Validate Default Recipients
        if ($config.Notifications.DefaultRecipients) {
            $recipientCount = $config.Notifications.DefaultRecipients.Count
            Write-Success "Default Recipients: $recipientCount"
            
            foreach ($recipient in $config.Notifications.DefaultRecipients) {
                if ([string]::IsNullOrWhiteSpace($recipient)) {
                    $errors += "Default recipient cannot be empty"
                }
                elseif ($recipient -match '\$\{.*\}') {
                    Write-Warning-Message "Recipient uses environment variable: $recipient"
                }
                elseif (-not (Test-EmailAddress $recipient)) {
                    $errors += "Invalid default recipient email: $recipient"
                }
                elseif ($Verbose) {
                    Write-Info "  - $recipient"
                }
            }
        }
        
        # Validate Rules
        if ($config.Notifications.Rules) {
            $ruleCount = $config.Notifications.Rules.Count
            Write-Success "Notification Rules: $ruleCount"
            
            $ruleIds = @{}
            
            foreach ($rule in $config.Notifications.Rules) {
                if ($Verbose) {
                    Write-Host ""
                    Write-Info "Validating rule: $($rule.Id)"
                }
                
                # Check for duplicate IDs
                if ($ruleIds.ContainsKey($rule.Id)) {
                    $errors += "Duplicate rule ID: $($rule.Id)"
                }
                else {
                    $ruleIds[$rule.Id] = $true
                }
                
                # Validate Type
                $validTypes = @("TestStart", "TestSuccess", "TestFailure", "CriticalFailure", "ReportGenerated")
                if ($rule.Type -notin $validTypes) {
                    $errors += "Invalid notification type in rule '$($rule.Id)': $($rule.Type)"
                }
                elseif ($Verbose) {
                    Write-Info "  Type: $($rule.Type)"
                }
                
                # Validate Recipients
                if (-not $rule.Recipients -or $rule.Recipients.Count -eq 0) {
                    $errors += "Rule '$($rule.Id)' must have at least one recipient"
                }
                else {
                    foreach ($recipient in $rule.Recipients) {
                        if ([string]::IsNullOrWhiteSpace($recipient)) {
                            $errors += "Recipient in rule '$($rule.Id)' cannot be empty"
                        }
                        elseif ($recipient -match '\$\{.*\}') {
                            if ($Verbose) {
                                Write-Warning-Message "  Recipient uses environment variable: $recipient"
                            }
                        }
                        elseif (-not (Test-EmailAddress $recipient)) {
                            $errors += "Invalid recipient email in rule '$($rule.Id)': $recipient"
                        }
                        elseif ($Verbose) {
                            Write-Info "  Recipient: $recipient"
                        }
                    }
                }
                
                # Check if enabled
                if ($rule.IsEnabled -eq $false -and $Verbose) {
                    Write-Warning-Message "  Rule is disabled"
                }
            }
        }
        else {
            $warnings += "No notification rules defined"
        }
    }
}
catch {
    Write-Error-Message "Failed to parse JSON: $($_.Exception.Message)"
    exit 1
}

# Display results
Write-Host ""
Write-Host "Validation Results" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan
Write-Host ""

if ($warnings.Count -gt 0) {
    Write-Host "Warnings ($($warnings.Count)):" -ForegroundColor Yellow
    foreach ($warning in $warnings) {
        Write-Warning-Message $warning
    }
    Write-Host ""
}

if ($errors.Count -eq 0) {
    Write-Success "Configuration validation PASSED"
    Write-Host ""
    exit 0
}
else {
    Write-Error-Message "Configuration validation FAILED with $($errors.Count) error(s):"
    Write-Host ""
    for ($i = 0; $i -lt $errors.Count; $i++) {
        Write-Host "  $($i + 1). $($errors[$i])" -ForegroundColor Red
    }
    Write-Host ""
    exit 1
}
