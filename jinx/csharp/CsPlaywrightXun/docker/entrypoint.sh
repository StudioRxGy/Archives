#!/bin/bash
# Docker 容器入口点脚本
# 用于启动企业自动化测试框架

set -e

# 颜色输出函数
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

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

# 显示启动信息
log_info "Starting Enterprise Automation Framework..."
log_info "Environment: ${DOTNET_ENVIRONMENT:-Production}"
log_info "Headless Mode: ${HEADLESS:-true}"

# 检查必要的目录
log_info "Checking required directories..."
for dir in "/app/reports" "/app/logs" "/app/screenshots" "/app/TestResults"; do
    if [ ! -d "$dir" ]; then
        log_warning "Creating missing directory: $dir"
        mkdir -p "$dir"
    fi
done

# 设置权限
log_info "Setting up permissions..."
chmod -R 755 /app/reports /app/logs /app/screenshots /app/TestResults 2>/dev/null || true

# 启动虚拟显示器 (如果需要)
if [ "${HEADLESS}" = "false" ] || [ "${DISPLAY}" = ":99" ]; then
    log_info "Starting virtual display server..."
    Xvfb :99 -screen 0 1920x1080x24 &
    XVFB_PID=$!
    
    # 等待显示器启动
    sleep 2
    
    # 验证显示器是否正常启动
    if ps -p $XVFB_PID > /dev/null; then
        log_success "Virtual display server started successfully (PID: $XVFB_PID)"
    else
        log_error "Failed to start virtual display server"
        exit 1
    fi
fi

# 检查 .NET 环境
log_info "Checking .NET environment..."
if dotnet --version > /dev/null 2>&1; then
    DOTNET_VERSION=$(dotnet --version)
    log_success ".NET SDK version: $DOTNET_VERSION"
else
    log_error ".NET SDK not found or not working properly"
    exit 1
fi

# 检查 Node.js 环境 (Playwright 需要)
log_info "Checking Node.js environment..."
if node --version > /dev/null 2>&1; then
    NODE_VERSION=$(node --version)
    log_success "Node.js version: $NODE_VERSION"
else
    log_warning "Node.js not found - some Playwright features may not work"
fi

# 环境变量验证
log_info "Validating environment variables..."
if [ -z "${DOTNET_ENVIRONMENT}" ]; then
    log_warning "DOTNET_ENVIRONMENT not set, using default: Production"
    export DOTNET_ENVIRONMENT=Production
fi

# 根据运行模式执行不同的操作
case "${1:-run}" in
    "test")
        log_info "Running in TEST mode..."
        shift
        
        # 默认测试参数
        TEST_ARGS="--configuration Release --logger trx --results-directory /app/TestResults"
        
        # 如果提供了额外参数，使用它们
        if [ $# -gt 0 ]; then
            TEST_ARGS="$@"
        fi
        
        log_info "Test arguments: $TEST_ARGS"
        
        # 运行测试
        exec dotnet test $TEST_ARGS
        ;;
        
    "test-ui")
        log_info "Running UI tests only..."
        exec dotnet test --configuration Release --logger trx --results-directory /app/TestResults --filter "Category=UI"
        ;;
        
    "test-api")
        log_info "Running API tests only..."
        exec dotnet test --configuration Release --logger trx --results-directory /app/TestResults --filter "Category=API"
        ;;
        
    "test-integration")
        log_info "Running integration tests only..."
        exec dotnet test --configuration Release --logger trx --results-directory /app/TestResults --filter "Category=Integration"
        ;;
        
    "generate-report")
        log_info "Generating test reports..."
        if [ -d "/app/TestResults" ] && [ "$(ls -A /app/TestResults)" ]; then
            dotnet run --project /app/scripts/ReportGenerator -- \
                --input "/app/TestResults/**/*.trx" \
                --output "/app/reports/index.html" \
                --format html
            log_success "Test report generated at /app/reports/index.html"
        else
            log_warning "No test results found to generate report"
        fi
        ;;
        
    "health-check")
        log_info "Performing health check..."
        
        # 检查应用程序是否响应
        if curl -f http://localhost:8080/health > /dev/null 2>&1; then
            log_success "Application health check passed"
            exit 0
        else
            log_error "Application health check failed"
            exit 1
        fi
        ;;
        
    "run"|"start")
        log_info "Starting application in normal mode..."
        shift
        
        # 如果没有提供参数，使用默认的应用程序启动命令
        if [ $# -eq 0 ]; then
            exec dotnet CsPlaywrightXun.dll
        else
            exec "$@"
        fi
        ;;
        
    "bash"|"shell")
        log_info "Starting interactive shell..."
        exec /bin/bash
        ;;
        
    "help"|"--help"|"-h")
        echo "Enterprise Automation Framework Docker Container"
        echo ""
        echo "Usage: docker run [OPTIONS] IMAGE [COMMAND] [ARGS...]"
        echo ""
        echo "Commands:"
        echo "  run, start          Start the application (default)"
        echo "  test               Run all tests"
        echo "  test-ui            Run UI tests only"
        echo "  test-api           Run API tests only"
        echo "  test-integration   Run integration tests only"
        echo "  generate-report    Generate test reports from existing results"
        echo "  health-check       Perform application health check"
        echo "  bash, shell        Start interactive shell"
        echo "  help               Show this help message"
        echo ""
        echo "Environment Variables:"
        echo "  DOTNET_ENVIRONMENT    Set environment (Development, Test, Staging, Production)"
        echo "  HEADLESS             Run in headless mode (true/false)"
        echo "  DISPLAY              X11 display for UI tests (default: :99)"
        echo ""
        echo "Examples:"
        echo "  docker run image test --filter \"Category=UI\""
        echo "  docker run -e HEADLESS=false image test-ui"
        echo "  docker run image generate-report"
        ;;
        
    *)
        log_info "Executing custom command: $@"
        exec "$@"
        ;;
esac

# 清理函数
cleanup() {
    log_info "Cleaning up..."
    
    # 停止虚拟显示器
    if [ ! -z "$XVFB_PID" ]; then
        log_info "Stopping virtual display server..."
        kill $XVFB_PID 2>/dev/null || true
    fi
    
    log_success "Cleanup completed"
}

# 注册清理函数
trap cleanup EXIT INT TERM

# 如果脚本到达这里，说明没有匹配的命令
log_error "Unknown command or execution path"
exit 1