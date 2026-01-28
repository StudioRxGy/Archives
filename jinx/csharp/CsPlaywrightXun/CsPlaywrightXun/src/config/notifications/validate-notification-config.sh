#!/bin/bash

# Notification Configuration Validator
# Validates notification configuration files for CsPlaywrightXun framework

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Output functions
success() {
    echo -e "${GREEN}✓${NC} $1"
}

error() {
    echo -e "${RED}✗${NC} $1"
}

warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

info() {
    echo -e "${CYAN}ℹ${NC} $1"
}

# Check if jq is installed
if ! command -v jq &> /dev/null; then
    error "jq is required but not installed. Please install jq:"
    echo "  macOS: brew install jq"
    echo "  Ubuntu/Debian: sudo apt-get install jq"
    echo "  CentOS/RHEL: sudo yum install jq"
    exit 1
fi

# Check arguments
if [ $# -eq 0 ]; then
    echo "Usage: $0 <config-file>"
    echo ""
    echo "Examples:"
    echo "  $0 appsettings.Notifications.json"
    echo "  $0 appsettings.Notifications.Development.json"
    exit 1
fi

CONFIG_FILE="$1"

echo ""
echo -e "${CYAN}Notification Configuration Validator${NC}"
echo -e "${CYAN}====================================${NC}"
echo ""

# Check if file exists
if [ ! -f "$CONFIG_FILE" ]; then
    error "Configuration file not found: $CONFIG_FILE"
    exit 1
fi

info "Validating: $CONFIG_FILE"
echo ""

ERRORS=0

# Validate JSON syntax
if ! jq empty "$CONFIG_FILE" 2>/dev/null; then
    error "Invalid JSON syntax"
    exit 1
fi
success "JSON syntax is valid"

# Check Notifications section
if ! jq -e '.Notifications' "$CONFIG_FILE" > /dev/null 2>&1; then
    error "Notifications section not found"
    ((ERRORS++))
else
    success "Notifications section found"
fi

# Validate SMTP configuration
if ! jq -e '.Notifications.Smtp' "$CONFIG_FILE" > /dev/null 2>&1; then
    error "SMTP configuration not found"
    ((ERRORS++))
else
    success "SMTP configuration found"
    
    # Validate Host
    HOST=$(jq -r '.Notifications.Smtp.Host // empty' "$CONFIG_FILE")
    if [ -z "$HOST" ]; then
        error "SMTP Host is required"
        ((ERRORS++))
    elif [[ "$HOST" =~ \$\{.*\} ]]; then
        warning "SMTP Host uses environment variable: $HOST"
    else
        success "SMTP Host: $HOST"
    fi
    
    # Validate Port
    PORT=$(jq -r '.Notifications.Smtp.Port // empty' "$CONFIG_FILE")
    if [ -z "$PORT" ] || [ "$PORT" -lt 1 ] || [ "$PORT" -gt 65535 ]; then
        error "SMTP Port must be between 1 and 65535 (current: $PORT)"
        ((ERRORS++))
    else
        success "SMTP Port: $PORT"
    fi
    
    # Validate FromEmail
    FROM_EMAIL=$(jq -r '.Notifications.Smtp.FromEmail // empty' "$CONFIG_FILE")
    if [ -z "$FROM_EMAIL" ]; then
        error "From Email is required"
        ((ERRORS++))
    elif [[ "$FROM_EMAIL" =~ \$\{.*\} ]]; then
        warning "From Email uses environment variable: $FROM_EMAIL"
    elif ! [[ "$FROM_EMAIL" =~ ^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$ ]]; then
        error "From Email is not a valid email address: $FROM_EMAIL"
        ((ERRORS++))
    else
        success "From Email: $FROM_EMAIL"
    fi
    
    # Validate Timeout
    TIMEOUT=$(jq -r '.Notifications.Smtp.TimeoutSeconds // empty' "$CONFIG_FILE")
    if [ -n "$TIMEOUT" ]; then
        if [ "$TIMEOUT" -lt 5 ] || [ "$TIMEOUT" -gt 300 ]; then
            error "Timeout must be between 5 and 300 seconds (current: $TIMEOUT)"
            ((ERRORS++))
        else
            success "Timeout: $TIMEOUT seconds"
        fi
    fi
    
    # Validate MaxRetryAttempts
    MAX_RETRY=$(jq -r '.Notifications.Smtp.MaxRetryAttempts // empty' "$CONFIG_FILE")
    if [ -n "$MAX_RETRY" ]; then
        if [ "$MAX_RETRY" -lt 0 ] || [ "$MAX_RETRY" -gt 10 ]; then
            error "Max retry attempts must be between 0 and 10 (current: $MAX_RETRY)"
            ((ERRORS++))
        else
            success "Max Retry Attempts: $MAX_RETRY"
        fi
    fi
    
    # Check SSL
    ENABLE_SSL=$(jq -r '.Notifications.Smtp.EnableSsl // true' "$CONFIG_FILE")
    if [ "$ENABLE_SSL" = "false" ]; then
        warning "SSL/TLS is disabled (not recommended for production)"
    else
        success "SSL/TLS: Enabled"
    fi
fi

# Validate Default Recipients
RECIPIENT_COUNT=$(jq -r '.Notifications.DefaultRecipients | length // 0' "$CONFIG_FILE")
if [ "$RECIPIENT_COUNT" -gt 0 ]; then
    success "Default Recipients: $RECIPIENT_COUNT"
    
    for i in $(seq 0 $((RECIPIENT_COUNT - 1))); do
        RECIPIENT=$(jq -r ".Notifications.DefaultRecipients[$i]" "$CONFIG_FILE")
        if [ -z "$RECIPIENT" ]; then
            error "Default recipient at index $i cannot be empty"
            ((ERRORS++))
        elif [[ "$RECIPIENT" =~ \$\{.*\} ]]; then
            warning "Recipient uses environment variable: $RECIPIENT"
        elif ! [[ "$RECIPIENT" =~ ^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$ ]]; then
            error "Invalid default recipient email: $RECIPIENT"
            ((ERRORS++))
        fi
    done
fi

# Validate Rules
RULE_COUNT=$(jq -r '.Notifications.Rules | length // 0' "$CONFIG_FILE")
if [ "$RULE_COUNT" -gt 0 ]; then
    success "Notification Rules: $RULE_COUNT"
    
    declare -A RULE_IDS
    
    for i in $(seq 0 $((RULE_COUNT - 1))); do
        RULE_ID=$(jq -r ".Notifications.Rules[$i].Id" "$CONFIG_FILE")
        
        # Check for duplicate IDs
        if [ -n "${RULE_IDS[$RULE_ID]}" ]; then
            error "Duplicate rule ID: $RULE_ID"
            ((ERRORS++))
        fi
        RULE_IDS[$RULE_ID]=1
        
        # Validate Type
        RULE_TYPE=$(jq -r ".Notifications.Rules[$i].Type" "$CONFIG_FILE")
        case "$RULE_TYPE" in
            TestStart|TestSuccess|TestFailure|CriticalFailure|ReportGenerated)
                ;;
            *)
                error "Invalid notification type in rule '$RULE_ID': $RULE_TYPE"
                ((ERRORS++))
                ;;
        esac
        
        # Validate Recipients
        RULE_RECIPIENT_COUNT=$(jq -r ".Notifications.Rules[$i].Recipients | length // 0" "$CONFIG_FILE")
        if [ "$RULE_RECIPIENT_COUNT" -eq 0 ]; then
            error "Rule '$RULE_ID' must have at least one recipient"
            ((ERRORS++))
        else
            for j in $(seq 0 $((RULE_RECIPIENT_COUNT - 1))); do
                RECIPIENT=$(jq -r ".Notifications.Rules[$i].Recipients[$j]" "$CONFIG_FILE")
                if [ -z "$RECIPIENT" ]; then
                    error "Recipient in rule '$RULE_ID' cannot be empty"
                    ((ERRORS++))
                elif [[ "$RECIPIENT" =~ \$\{.*\} ]]; then
                    : # Environment variable, skip validation
                elif ! [[ "$RECIPIENT" =~ ^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$ ]]; then
                    error "Invalid recipient email in rule '$RULE_ID': $RECIPIENT"
                    ((ERRORS++))
                fi
            done
        fi
    done
else
    warning "No notification rules defined"
fi

# Display results
echo ""
echo -e "${CYAN}Validation Results${NC}"
echo -e "${CYAN}==================${NC}"
echo ""

if [ $ERRORS -eq 0 ]; then
    success "Configuration validation PASSED"
    echo ""
    exit 0
else
    error "Configuration validation FAILED with $ERRORS error(s)"
    echo ""
    exit 1
fi
