#!/bin/bash

# Enterprise Automation Framework Deployment Script
# This script automates the deployment process for different environments

set -euo pipefail

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
DOCKER_REGISTRY="${DOCKER_REGISTRY:-ghcr.io/your-org}"
NAMESPACE_PREFIX="${NAMESPACE_PREFIX:-automation-framework}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
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

# Help function
show_help() {
    cat << EOF
Enterprise Automation Framework Deployment Script

Usage: $0 [OPTIONS] COMMAND

Commands:
    build           Build Docker images
    test            Run tests in containers
    deploy          Deploy to Kubernetes
    rollback        Rollback deployment
    status          Check deployment status
    logs            View application logs
    cleanup         Clean up resources

Options:
    -e, --environment ENV    Target environment (dev|test|staging|prod)
    -v, --version VERSION    Image version/tag to deploy
    -n, --namespace NS       Kubernetes namespace
    -r, --registry REG       Docker registry URL
    -d, --dry-run           Show what would be done without executing
    -h, --help              Show this help message

Examples:
    $0 -e test deploy                    # Deploy to test environment
    $0 -e prod -v v1.2.3 deploy         # Deploy specific version to prod
    $0 -e staging rollback               # Rollback staging deployment
    $0 -d -e prod deploy                 # Dry run deployment to prod

Environment Variables:
    DOCKER_REGISTRY         Docker registry URL
    KUBECONFIG             Kubernetes config file path
    NAMESPACE_PREFIX       Namespace prefix for environments
EOF
}

# Parse command line arguments
parse_args() {
    ENVIRONMENT=""
    VERSION=""
    NAMESPACE=""
    REGISTRY=""
    DRY_RUN=false
    COMMAND=""

    while [[ $# -gt 0 ]]; do
        case $1 in
            -e|--environment)
                ENVIRONMENT="$2"
                shift 2
                ;;
            -v|--version)
                VERSION="$2"
                shift 2
                ;;
            -n|--namespace)
                NAMESPACE="$2"
                shift 2
                ;;
            -r|--registry)
                REGISTRY="$2"
                shift 2
                ;;
            -d|--dry-run)
                DRY_RUN=true
                shift
                ;;
            -h|--help)
                show_help
                exit 0
                ;;
            build|test|deploy|rollback|status|logs|cleanup)
                COMMAND="$1"
                shift
                ;;
            *)
                log_error "Unknown option: $1"
                show_help
                exit 1
                ;;
        esac
    done

    # Set defaults
    if [[ -z "$REGISTRY" ]]; then
        REGISTRY="$DOCKER_REGISTRY"
    fi

    if [[ -z "$NAMESPACE" && -n "$ENVIRONMENT" ]]; then
        NAMESPACE="${NAMESPACE_PREFIX}-${ENVIRONMENT}"
    fi

    if [[ -z "$VERSION" ]]; then
        VERSION="$(date +'%Y.%m.%d')-$(git rev-parse --short HEAD)"
    fi

    # Validation
    if [[ -z "$COMMAND" ]]; then
        log_error "Command is required"
        show_help
        exit 1
    fi

    if [[ "$COMMAND" != "build" && -z "$ENVIRONMENT" ]]; then
        log_error "Environment is required for command: $COMMAND"
        exit 1
    fi
}

# Check prerequisites
check_prerequisites() {
    local missing_tools=()

    # Check required tools
    command -v docker >/dev/null 2>&1 || missing_tools+=("docker")
    command -v kubectl >/dev/null 2>&1 || missing_tools+=("kubectl")
    command -v git >/dev/null 2>&1 || missing_tools+=("git")

    if [[ ${#missing_tools[@]} -gt 0 ]]; then
        log_error "Missing required tools: ${missing_tools[*]}"
        exit 1
    fi

    # Check Docker daemon
    if ! docker info >/dev/null 2>&1; then
        log_error "Docker daemon is not running"
        exit 1
    fi

    # Check Kubernetes connection
    if [[ "$COMMAND" != "build" ]]; then
        if ! kubectl cluster-info >/dev/null 2>&1; then
            log_error "Cannot connect to Kubernetes cluster"
            exit 1
        fi
    fi
}

# Execute command with dry-run support
execute_command() {
    local cmd="$1"
    
    if [[ "$DRY_RUN" == "true" ]]; then
        log_info "[DRY-RUN] Would execute: $cmd"
    else
        log_info "Executing: $cmd"
        eval "$cmd"
    fi
}

# Build Docker images
build_images() {
    log_info "Building Docker images..."
    
    local framework_image="${REGISTRY}/enterprise-automation-framework:${VERSION}"
    local test_runner_image="${REGISTRY}/enterprise-automation-test-runner:${VERSION}"
    
    # Build framework image
    log_info "Building framework image: $framework_image"
    execute_command "docker build -f docker/Dockerfile -t $framework_image ."
    
    # Build test runner image
    log_info "Building test runner image: $test_runner_image"
    execute_command "docker build -f docker/Dockerfile.test-runner -t $test_runner_image ."
    
    # Push images
    if [[ "$DRY_RUN" == "false" ]]; then
        log_info "Pushing images to registry..."
        execute_command "docker push $framework_image"
        execute_command "docker push $test_runner_image"
    fi
    
    log_success "Images built successfully"
}

# Run tests in containers
run_tests() {
    log_info "Running tests in containers..."
    
    local test_runner_image="${REGISTRY}/enterprise-automation-test-runner:${VERSION}"
    
    # Run unit tests
    log_info "Running unit tests..."
    execute_command "docker run --rm -e TEST_CATEGORY=Unit -e ENVIRONMENT=${ENVIRONMENT} $test_runner_image"
    
    # Run API tests
    log_info "Running API tests..."
    execute_command "docker run --rm -e TEST_CATEGORY=API -e ENVIRONMENT=${ENVIRONMENT} $test_runner_image"
    
    log_success "Tests completed successfully"
}

# Deploy to Kubernetes
deploy_to_kubernetes() {
    log_info "Deploying to Kubernetes environment: $ENVIRONMENT"
    
    # Create namespace if it doesn't exist
    execute_command "kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -"
    
    # Update image tags in deployment files
    local temp_dir=$(mktemp -d)
    cp -r k8s/* "$temp_dir/"
    
    # Replace placeholders
    find "$temp_dir" -name "*.yaml" -exec sed -i "s|IMAGE_TAG|${VERSION}|g" {} \;
    find "$temp_dir" -name "*.yaml" -exec sed -i "s|\${VERSION}|${VERSION}|g" {} \;
    find "$temp_dir" -name "*.yaml" -exec sed -i "s|\${DOCKER_REGISTRY}|${REGISTRY}|g" {} \;
    find "$temp_dir" -name "*.yaml" -exec sed -i "s|\${DEPLOYMENT_TIMESTAMP}|$(date -u +%Y-%m-%dT%H:%M:%SZ)|g" {} \;
    
    # Apply Kubernetes manifests
    log_info "Applying Kubernetes manifests..."
    execute_command "kubectl apply -f $temp_dir/namespace.yaml"
    execute_command "kubectl apply -f $temp_dir/configmap.yaml"
    execute_command "kubectl apply -f $temp_dir/secret.yaml"
    execute_command "kubectl apply -f $temp_dir/pvc.yaml"
    execute_command "kubectl apply -f $temp_dir/deployment.yaml"
    execute_command "kubectl apply -f $temp_dir/service.yaml"
    execute_command "kubectl apply -f $temp_dir/hpa.yaml"
    
    # Wait for deployment to complete
    if [[ "$DRY_RUN" == "false" ]]; then
        log_info "Waiting for deployment to complete..."
        kubectl rollout status deployment/automation-framework -n "$NAMESPACE" --timeout=600s
        kubectl rollout status deployment/test-runner -n "$NAMESPACE" --timeout=300s
    fi
    
    # Clean up temp directory
    rm -rf "$temp_dir"
    
    log_success "Deployment completed successfully"
}

# Rollback deployment
rollback_deployment() {
    log_info "Rolling back deployment in environment: $ENVIRONMENT"
    
    # Get rollout history
    kubectl rollout history deployment/automation-framework -n "$NAMESPACE"
    
    # Rollback to previous version
    execute_command "kubectl rollout undo deployment/automation-framework -n $NAMESPACE"
    execute_command "kubectl rollout undo deployment/test-runner -n $NAMESPACE"
    
    # Wait for rollback to complete
    if [[ "$DRY_RUN" == "false" ]]; then
        log_info "Waiting for rollback to complete..."
        kubectl rollout status deployment/automation-framework -n "$NAMESPACE" --timeout=600s
        kubectl rollout status deployment/test-runner -n "$NAMESPACE" --timeout=300s
    fi
    
    log_success "Rollback completed successfully"
}

# Check deployment status
check_status() {
    log_info "Checking deployment status for environment: $ENVIRONMENT"
    
    echo
    log_info "Deployments:"
    kubectl get deployments -n "$NAMESPACE" -o wide
    
    echo
    log_info "Pods:"
    kubectl get pods -n "$NAMESPACE" -o wide
    
    echo
    log_info "Services:"
    kubectl get services -n "$NAMESPACE" -o wide
    
    echo
    log_info "Ingress:"
    kubectl get ingress -n "$NAMESPACE" -o wide 2>/dev/null || log_warning "No ingress found"
    
    echo
    log_info "HPA:"
    kubectl get hpa -n "$NAMESPACE" -o wide 2>/dev/null || log_warning "No HPA found"
    
    echo
    log_info "Recent Events:"
    kubectl get events -n "$NAMESPACE" --sort-by='.lastTimestamp' | tail -10
}

# View application logs
view_logs() {
    log_info "Viewing logs for environment: $ENVIRONMENT"
    
    local deployment="${1:-automation-framework}"
    local lines="${2:-100}"
    
    log_info "Showing last $lines lines from $deployment deployment:"
    kubectl logs -n "$NAMESPACE" deployment/"$deployment" --tail="$lines" -f
}

# Cleanup resources
cleanup_resources() {
    log_info "Cleaning up resources for environment: $ENVIRONMENT"
    
    # Confirm cleanup
    if [[ "$DRY_RUN" == "false" ]]; then
        read -p "Are you sure you want to delete all resources in namespace $NAMESPACE? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            log_info "Cleanup cancelled"
            exit 0
        fi
    fi
    
    # Delete namespace (this will delete all resources)
    execute_command "kubectl delete namespace $NAMESPACE --ignore-not-found=true"
    
    log_success "Cleanup completed successfully"
}

# Main function
main() {
    parse_args "$@"
    check_prerequisites
    
    log_info "Starting deployment script..."
    log_info "Command: $COMMAND"
    log_info "Environment: $ENVIRONMENT"
    log_info "Version: $VERSION"
    log_info "Namespace: $NAMESPACE"
    log_info "Registry: $REGISTRY"
    log_info "Dry Run: $DRY_RUN"
    
    case "$COMMAND" in
        build)
            build_images
            ;;
        test)
            run_tests
            ;;
        deploy)
            deploy_to_kubernetes
            ;;
        rollback)
            rollback_deployment
            ;;
        status)
            check_status
            ;;
        logs)
            view_logs
            ;;
        cleanup)
            cleanup_resources
            ;;
        *)
            log_error "Unknown command: $COMMAND"
            exit 1
            ;;
    esac
    
    log_success "Script completed successfully!"
}

# Run main function with all arguments
main "$@"