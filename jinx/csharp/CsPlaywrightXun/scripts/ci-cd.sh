#!/bin/bash

# Enterprise Automation Framework CI/CD Pipeline Script
# Comprehensive script for running CI/CD operations locally and in CI environments

set -euo pipefail

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
DOCKER_REGISTRY="${DOCKER_REGISTRY:-ghcr.io}"
IMAGE_PREFIX="${IMAGE_PREFIX:-enterprise-automation}"
VERSION="${VERSION:-$(date +'%Y.%m.%d')-$(git rev-parse --short HEAD)}"
ENVIRONMENT="${ENVIRONMENT:-test}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
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

log_step() {
    echo -e "${PURPLE}[STEP]${NC} $1"
}

log_debug() {
    if [[ "${DEBUG:-false}" == "true" ]]; then
        echo -e "${CYAN}[DEBUG]${NC} $1"
    fi
}

# Help function
show_help() {
    cat << EOF
Enterprise Automation Framework CI/CD Pipeline Script

Usage: $0 [OPTIONS] COMMAND

Commands:
    validate        Validate code quality and security
    test           Run all tests (unit, api, ui, integration)
    build          Build and package the application
    docker         Build and push Docker images
    deploy         Deploy to target environment
    pipeline       Run complete CI/CD pipeline
    cleanup        Clean up resources and artifacts
    report         Generate and publish reports

Test Commands:
    test:unit      Run unit tests only
    test:api       Run API tests only
    test:ui        Run UI tests only
    test:integration Run integration tests only
    test:performance Run performance tests only

Options:
    -e, --environment ENV    Target environment (dev|test|staging|prod)
    -v, --version VERSION    Version/tag for images and deployment
    -r, --registry REG       Docker registry URL
    -b, --browsers LIST      Comma-separated list of browsers for UI tests
    -p, --parallel          Enable parallel test execution
    -c, --coverage          Generate code coverage reports
    -s, --security          Run security scans
    -d, --dry-run          Show what would be done without executing
    --skip-tests           Skip test execution
    --skip-build           Skip build step
    --skip-docker          Skip Docker operations
    --debug                Enable debug logging
    -h, --help             Show this help message

Examples:
    $0 pipeline                              # Run complete pipeline
    $0 -e staging -v v1.2.3 deploy         # Deploy specific version to staging
    $0 test:ui -b chromium,firefox          # Run UI tests on specific browsers
    $0 -p -c test                           # Run all tests with parallel execution and coverage
    $0 --security validate                  # Run validation with security scans
    $0 -d pipeline                          # Dry run of complete pipeline

Environment Variables:
    CI                     Set to 'true' in CI environments
    DOCKER_REGISTRY        Docker registry URL
    GITHUB_TOKEN          GitHub token for registry authentication
    SONAR_TOKEN           SonarCloud token for code analysis
    ENVIRONMENT           Target environment
    VERSION               Build version
    DEBUG                 Enable debug logging
EOF
}

# Parse command line arguments
parse_args() {
    COMMAND=""
    BROWSERS="chromium,firefox"
    PARALLEL=false
    COVERAGE=false
    SECURITY=false
    DRY_RUN=false
    SKIP_TESTS=false
    SKIP_BUILD=false
    SKIP_DOCKER=false

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
            -r|--registry)
                DOCKER_REGISTRY="$2"
                shift 2
                ;;
            -b|--browsers)
                BROWSERS="$2"
                shift 2
                ;;
            -p|--parallel)
                PARALLEL=true
                shift
                ;;
            -c|--coverage)
                COVERAGE=true
                shift
                ;;
            -s|--security)
                SECURITY=true
                shift
                ;;
            -d|--dry-run)
                DRY_RUN=true
                shift
                ;;
            --skip-tests)
                SKIP_TESTS=true
                shift
                ;;
            --skip-build)
                SKIP_BUILD=true
                shift
                ;;
            --skip-docker)
                SKIP_DOCKER=true
                shift
                ;;
            --debug)
                DEBUG=true
                shift
                ;;
            -h|--help)
                show_help
                exit 0
                ;;
            validate|test|build|docker|deploy|pipeline|cleanup|report|test:unit|test:api|test:ui|test:integration|test:performance)
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

    if [[ -z "$COMMAND" ]]; then
        log_error "Command is required"
        show_help
        exit 1
    fi
}

# Check prerequisites
check_prerequisites() {
    local missing_tools=()

    # Check required tools
    command -v docker >/dev/null 2>&1 || missing_tools+=("docker")
    command -v docker-compose >/dev/null 2>&1 || missing_tools+=("docker-compose")
    command -v dotnet >/dev/null 2>&1 || missing_tools+=("dotnet")
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

    # Check .NET version
    local dotnet_version
    dotnet_version=$(dotnet --version)
    log_debug "Using .NET version: $dotnet_version"
}

# Execute command with dry-run support
execute_command() {
    local cmd="$1"
    local description="${2:-}"
    
    if [[ -n "$description" ]]; then
        log_debug "$description"
    fi
    
    if [[ "$DRY_RUN" == "true" ]]; then
        log_info "[DRY-RUN] Would execute: $cmd"
    else
        log_debug "Executing: $cmd"
        eval "$cmd"
    fi
}

# Set up environment
setup_environment() {
    log_step "Setting up environment"
    
    # Create necessary directories
    execute_command "mkdir -p reports logs screenshots coverage" "Creating output directories"
    
    # Set environment variables for Docker Compose
    export DOCKER_REGISTRY
    export VERSION
    export ENVIRONMENT
    export PARALLEL_TESTS="$PARALLEL"
    
    # Set CI-specific variables
    if [[ "${CI:-false}" == "true" ]]; then
        export CI=true
        export HEADLESS=true
    fi
    
    log_success "Environment setup completed"
}

# Validate code quality and security
validate_code() {
    log_step "Validating code quality and security"
    
    # Restore dependencies
    execute_command "dotnet restore" "Restoring NuGet packages"
    
    # Build solution
    execute_command "dotnet build --no-restore --configuration Release" "Building solution"
    
    # Code formatting check
    execute_command "dotnet format --verify-no-changes --verbosity diagnostic" "Checking code formatting"
    
    # Security scan
    if [[ "$SECURITY" == "true" ]]; then
        log_info "Running security scans..."
        
        # Run Trivy security scan
        if command -v trivy >/dev/null 2>&1; then
            execute_command "trivy fs --format json --output reports/security-scan.json ." "Running Trivy security scan"
        else
            log_warning "Trivy not found, skipping security scan"
        fi
        
        # Run SonarCloud analysis if token is available
        if [[ -n "${SONAR_TOKEN:-}" ]]; then
            execute_command "docker-compose -f docker/docker-compose.ci.yml --profile code-quality run --rm sonar-scanner" "Running SonarCloud analysis"
        else
            log_warning "SONAR_TOKEN not set, skipping SonarCloud analysis"
        fi
    fi
    
    log_success "Code validation completed"
}

# Run tests
run_tests() {
    local test_type="${1:-all}"
    log_step "Running $test_type tests"
    
    local test_args=""
    local coverage_args=""
    local parallel_args=""
    
    # Set up test arguments
    if [[ "$PARALLEL" == "true" ]]; then
        parallel_args="--parallel"
    fi
    
    if [[ "$COVERAGE" == "true" ]]; then
        coverage_args="--collect:\"XPlat Code Coverage\""
    fi
    
    case "$test_type" in
        "unit"|"test:unit")
            execute_command "docker-compose -f docker/docker-compose.ci.yml --profile unit-testing run --rm unit-test-runner" "Running unit tests"
            ;;
        "api"|"test:api")
            execute_command "docker-compose -f docker/docker-compose.ci.yml --profile api-testing run --rm api-test-runner" "Running API tests"
            ;;
        "ui"|"test:ui")
            IFS=',' read -ra BROWSER_ARRAY <<< "$BROWSERS"
            for browser in "${BROWSER_ARRAY[@]}"; do
                log_info "Running UI tests on $browser"
                execute_command "BROWSER=$browser docker-compose -f docker/docker-compose.ci.yml --profile ui-testing run --rm ui-test-$browser" "Running UI tests on $browser"
            done
            ;;
        "integration"|"test:integration")
            execute_command "docker-compose -f docker/docker-compose.ci.yml --profile integration-testing run --rm integration-test-runner" "Running integration tests"
            ;;
        "performance"|"test:performance")
            execute_command "docker-compose -f docker/docker-compose.ci.yml --profile performance-testing run --rm performance-test-runner" "Running performance tests"
            ;;
        "all"|"test")
            run_tests "unit"
            run_tests "api"
            run_tests "ui"
            run_tests "integration"
            ;;
        *)
            log_error "Unknown test type: $test_type"
            exit 1
            ;;
    esac
    
    log_success "$test_type tests completed"
}

# Build and package
build_package() {
    log_step "Building and packaging application"
    
    # Restore dependencies
    execute_command "dotnet restore" "Restoring dependencies"
    
    # Build solution
    execute_command "dotnet build --no-restore --configuration Release -p:Version=$VERSION" "Building solution"
    
    # Publish framework
    execute_command "dotnet publish CsPlaywrightXun/CsPlaywrightXun.csproj --no-build --configuration Release --output ./publish/framework -p:Version=$VERSION" "Publishing framework"
    
    # Create NuGet package
    execute_command "dotnet pack CsPlaywrightXun/CsPlaywrightXun.csproj --no-build --configuration Release --output ./packages -p:PackageVersion=$VERSION" "Creating NuGet package"
    
    log_success "Build and packaging completed"
}

# Build Docker images
build_docker_images() {
    log_step "Building Docker images"
    
    local framework_image="${DOCKER_REGISTRY}/${IMAGE_PREFIX}-framework:${VERSION}"
    local test_runner_image="${DOCKER_REGISTRY}/${IMAGE_PREFIX}-test-runner:${VERSION}"
    
    # Build framework image
    execute_command "docker build -f docker/Dockerfile -t $framework_image --build-arg VERSION=$VERSION ." "Building framework image"
    execute_command "docker tag $framework_image ${DOCKER_REGISTRY}/${IMAGE_PREFIX}-framework:latest" "Tagging framework image as latest"
    
    # Build test runner image
    execute_command "docker build -f docker/Dockerfile.test-runner -t $test_runner_image --build-arg VERSION=$VERSION ." "Building test runner image"
    execute_command "docker tag $test_runner_image ${DOCKER_REGISTRY}/${IMAGE_PREFIX}-test-runner:latest" "Tagging test runner image as latest"
    
    # Push images if not in dry-run mode and in CI
    if [[ "$DRY_RUN" == "false" && "${CI:-false}" == "true" ]]; then
        log_info "Pushing Docker images to registry..."
        execute_command "docker push $framework_image" "Pushing framework image"
        execute_command "docker push ${DOCKER_REGISTRY}/${IMAGE_PREFIX}-framework:latest" "Pushing framework latest tag"
        execute_command "docker push $test_runner_image" "Pushing test runner image"
        execute_command "docker push ${DOCKER_REGISTRY}/${IMAGE_PREFIX}-test-runner:latest" "Pushing test runner latest tag"
    fi
    
    log_success "Docker images built successfully"
}

# Deploy to environment
deploy_application() {
    log_step "Deploying to $ENVIRONMENT environment"
    
    # Use the deployment script
    local deploy_script="$SCRIPT_DIR/deploy.sh"
    if [[ -f "$deploy_script" ]]; then
        local deploy_args="-e $ENVIRONMENT -v $VERSION"
        if [[ "$DRY_RUN" == "true" ]]; then
            deploy_args="$deploy_args -d"
        fi
        execute_command "$deploy_script $deploy_args deploy" "Running deployment script"
    else
        log_warning "Deployment script not found, using basic deployment"
        
        # Basic Kubernetes deployment
        if command -v kubectl >/dev/null 2>&1; then
            execute_command "kubectl set image deployment/automation-framework automation-framework=${DOCKER_REGISTRY}/${IMAGE_PREFIX}-framework:${VERSION} -n automation-framework-${ENVIRONMENT}" "Updating Kubernetes deployment"
        else
            log_warning "kubectl not found, skipping Kubernetes deployment"
        fi
    fi
    
    log_success "Deployment to $ENVIRONMENT completed"
}

# Generate and publish reports
generate_reports() {
    log_step "Generating and publishing reports"
    
    # Start report aggregator
    execute_command "docker-compose -f docker/docker-compose.ci.yml --profile reporting run --rm report-aggregator" "Aggregating test reports"
    
    # Generate coverage report if coverage was collected
    if [[ "$COVERAGE" == "true" ]]; then
        if command -v reportgenerator >/dev/null 2>&1; then
            execute_command "reportgenerator -reports:coverage/**/*.xml -targetdir:reports/coverage -reporttypes:Html" "Generating coverage report"
        else
            log_warning "ReportGenerator not found, skipping coverage report generation"
        fi
    fi
    
    # Start report server for local viewing
    if [[ "${CI:-false}" != "true" ]]; then
        log_info "Starting report server at http://localhost:8081"
        execute_command "docker-compose -f docker/docker-compose.ci.yml --profile reporting up -d report-server-ci" "Starting report server"
    fi
    
    log_success "Reports generated and published"
}

# Clean up resources
cleanup_resources() {
    log_step "Cleaning up resources"
    
    # Stop and remove containers
    execute_command "docker-compose -f docker/docker-compose.ci.yml down -v" "Stopping CI containers"
    
    # Clean up Docker images (keep last 3 versions)
    if [[ "${CI:-false}" == "true" ]]; then
        execute_command "docker image prune -f" "Pruning unused Docker images"
    fi
    
    # Clean up old test results (keep last 10 runs)
    if [[ -d "reports" ]]; then
        execute_command "find reports -name '*.trx' -mtime +10 -delete" "Cleaning old test results"
    fi
    
    log_success "Cleanup completed"
}

# Run complete CI/CD pipeline
run_pipeline() {
    log_step "Running complete CI/CD pipeline"
    
    local start_time=$(date +%s)
    
    # Setup
    setup_environment
    
    # Validation
    if [[ "$SKIP_TESTS" != "true" ]]; then
        validate_code
    fi
    
    # Testing
    if [[ "$SKIP_TESTS" != "true" ]]; then
        run_tests "all"
    fi
    
    # Build
    if [[ "$SKIP_BUILD" != "true" ]]; then
        build_package
    fi
    
    # Docker
    if [[ "$SKIP_DOCKER" != "true" ]]; then
        build_docker_images
    fi
    
    # Deploy (only for main branch or manual trigger)
    if [[ "${GITHUB_REF:-}" == "refs/heads/main" || "${CI:-false}" != "true" ]]; then
        deploy_application
    fi
    
    # Reports
    generate_reports
    
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    
    log_success "Pipeline completed successfully in ${duration}s"
}

# Main function
main() {
    parse_args "$@"
    check_prerequisites
    
    log_info "Starting CI/CD pipeline..."
    log_info "Command: $COMMAND"
    log_info "Environment: $ENVIRONMENT"
    log_info "Version: $VERSION"
    log_info "Registry: $DOCKER_REGISTRY"
    log_info "Dry Run: $DRY_RUN"
    
    case "$COMMAND" in
        validate)
            setup_environment
            validate_code
            ;;
        test|test:unit|test:api|test:ui|test:integration|test:performance)
            setup_environment
            run_tests "$COMMAND"
            ;;
        build)
            setup_environment
            build_package
            ;;
        docker)
            setup_environment
            build_docker_images
            ;;
        deploy)
            setup_environment
            deploy_application
            ;;
        pipeline)
            run_pipeline
            ;;
        report)
            generate_reports
            ;;
        cleanup)
            cleanup_resources
            ;;
        *)
            log_error "Unknown command: $COMMAND"
            exit 1
            ;;
    esac
    
    log_success "Operation completed successfully!"
}

# Trap for cleanup on exit
trap cleanup_resources EXIT

# Run main function with all arguments
main "$@"