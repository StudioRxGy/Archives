#!/bin/bash

# Database backup script for Enterprise Automation Framework
# Usage: ./backup.sh [database_name] [backup_type]

set -e

# Configuration
BACKUP_DIR="/backups"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
RETENTION_DAYS=30
DATABASE_NAME=${1:-"testdata"}
BACKUP_TYPE=${2:-"full"}  # full, incremental, schema-only

# Database connection settings
DB_HOST=${DB_HOST:-"postgres"}
DB_PORT=${DB_PORT:-"5432"}
DB_USER=${DB_USER:-"postgres"}
DB_PASSWORD=${DB_PASSWORD:-"password"}

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Create backup directory
create_backup_dir() {
    if [ ! -d "$BACKUP_DIR" ]; then
        mkdir -p "$BACKUP_DIR"
        log_info "Created backup directory: $BACKUP_DIR"
    fi
}

# Perform database backup
backup_database() {
    local backup_file="$BACKUP_DIR/${DATABASE_NAME}_${BACKUP_TYPE}_${TIMESTAMP}.sql"
    
    log_info "Starting $BACKUP_TYPE backup of database: $DATABASE_NAME"
    
    case $BACKUP_TYPE in
        "full")
            pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DATABASE_NAME" \
                --verbose --no-password --format=custom --compress=9 \
                --file="$backup_file.custom"
            
            # Also create a plain SQL backup for easier restoration
            pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DATABASE_NAME" \
                --verbose --no-password --format=plain \
                --file="$backup_file"
            ;;
        "schema-only")
            pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DATABASE_NAME" \
                --verbose --no-password --schema-only \
                --file="$backup_file"
            ;;
        "data-only")
            pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DATABASE_NAME" \
                --verbose --no-password --data-only \
                --file="$backup_file"
            ;;
        *)
            log_error "Invalid backup type: $BACKUP_TYPE"
            exit 1
            ;;
    esac
    
    if [ $? -eq 0 ]; then
        log_success "Backup completed: $backup_file"
        
        # Compress the backup
        gzip "$backup_file"
        log_success "Backup compressed: $backup_file.gz"
        
        # Calculate backup size
        backup_size=$(du -h "$backup_file.gz" | cut -f1)
        log_info "Backup size: $backup_size"
        
        return 0
    else
        log_error "Backup failed"
        return 1
    fi
}

# Backup test reports and logs
backup_test_artifacts() {
    log_info "Backing up test artifacts..."
    
    local artifacts_backup="$BACKUP_DIR/test_artifacts_${TIMESTAMP}.tar.gz"
    
    # Create archive of reports, logs, and screenshots
    tar -czf "$artifacts_backup" \
        -C /app \
        reports/ logs/ screenshots/ \
        2>/dev/null || true
    
    if [ -f "$artifacts_backup" ]; then
        local artifacts_size=$(du -h "$artifacts_backup" | cut -f1)
        log_success "Test artifacts backed up: $artifacts_backup ($artifacts_size)"
    else
        log_warning "No test artifacts found to backup"
    fi
}

# Clean old backups
cleanup_old_backups() {
    log_info "Cleaning up backups older than $RETENTION_DAYS days..."
    
    find "$BACKUP_DIR" -name "*.sql.gz" -type f -mtime +$RETENTION_DAYS -delete
    find "$BACKUP_DIR" -name "*.custom" -type f -mtime +$RETENTION_DAYS -delete
    find "$BACKUP_DIR" -name "test_artifacts_*.tar.gz" -type f -mtime +$RETENTION_DAYS -delete
    
    log_success "Old backups cleaned up"
}

# Verify backup integrity
verify_backup() {
    local backup_file="$1"
    
    if [ -f "$backup_file" ]; then
        log_info "Verifying backup integrity..."
        
        # Test gzip integrity
        if gzip -t "$backup_file" 2>/dev/null; then
            log_success "Backup file integrity verified"
            return 0
        else
            log_error "Backup file is corrupted"
            return 1
        fi
    else
        log_error "Backup file not found: $backup_file"
        return 1
    fi
}

# Upload backup to cloud storage (optional)
upload_to_cloud() {
    local backup_file="$1"
    
    if [ -n "$AWS_S3_BUCKET" ]; then
        log_info "Uploading backup to S3..."
        
        aws s3 cp "$backup_file" "s3://$AWS_S3_BUCKET/backups/$(basename $backup_file)" \
            --storage-class STANDARD_IA
        
        if [ $? -eq 0 ]; then
            log_success "Backup uploaded to S3"
        else
            log_error "Failed to upload backup to S3"
        fi
    fi
    
    if [ -n "$AZURE_STORAGE_ACCOUNT" ]; then
        log_info "Uploading backup to Azure Blob Storage..."
        
        az storage blob upload \
            --account-name "$AZURE_STORAGE_ACCOUNT" \
            --container-name "backups" \
            --name "$(basename $backup_file)" \
            --file "$backup_file" \
            --tier Cool
        
        if [ $? -eq 0 ]; then
            log_success "Backup uploaded to Azure"
        else
            log_error "Failed to upload backup to Azure"
        fi
    fi
}

# Send notification
send_notification() {
    local status="$1"
    local message="$2"
    
    if [ -n "$SLACK_WEBHOOK_URL" ]; then
        local color="good"
        if [ "$status" != "success" ]; then
            color="danger"
        fi
        
        curl -X POST -H 'Content-type: application/json' \
            --data "{\"attachments\":[{\"color\":\"$color\",\"text\":\"$message\"}]}" \
            "$SLACK_WEBHOOK_URL"
    fi
    
    if [ -n "$EMAIL_RECIPIENT" ]; then
        echo "$message" | mail -s "Backup Notification - $status" "$EMAIL_RECIPIENT"
    fi
}

# Main backup function
main() {
    log_info "Starting backup process..."
    log_info "Database: $DATABASE_NAME"
    log_info "Backup type: $BACKUP_TYPE"
    log_info "Timestamp: $TIMESTAMP"
    
    create_backup_dir
    
    # Perform database backup
    if backup_database; then
        backup_file="$BACKUP_DIR/${DATABASE_NAME}_${BACKUP_TYPE}_${TIMESTAMP}.sql.gz"
        
        # Verify backup
        if verify_backup "$backup_file"; then
            # Upload to cloud storage
            upload_to_cloud "$backup_file"
            
            # Backup test artifacts
            backup_test_artifacts
            
            # Cleanup old backups
            cleanup_old_backups
            
            send_notification "success" "Backup completed successfully: $(basename $backup_file)"
            log_success "Backup process completed successfully"
        else
            send_notification "error" "Backup verification failed"
            log_error "Backup verification failed"
            exit 1
        fi
    else
        send_notification "error" "Database backup failed"
        log_error "Database backup failed"
        exit 1
    fi
}

# Handle script interruption
trap 'log_error "Backup process interrupted"; exit 1' INT TERM

# Run main function
main "$@"