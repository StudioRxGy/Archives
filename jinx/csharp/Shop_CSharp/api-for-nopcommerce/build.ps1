# nopCommerce API Plugin 构建脚本
# 适用于 Windows PowerShell

Write-Host "=== nopCommerce API Plugin 构建脚本 ===" -ForegroundColor Green
Write-Host ""

# 检查.NET SDK版本
Write-Host "检查.NET SDK版本..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
Write-Host "检测到 .NET SDK 版本: $dotnetVersion" -ForegroundColor Green

# 检查nopCommerce源代码目录
Write-Host "检查nopCommerce源代码目录..." -ForegroundColor Yellow
$nopCommercePath = "..\nopCommerce"
if (Test-Path $nopCommercePath) {
    Write-Host "nopCommerce源代码目录存在: $nopCommercePath" -ForegroundColor Green
} else {
    Write-Host "警告: nopCommerce源代码目录不存在: $nopCommercePath" -ForegroundColor Red
    Write-Host "请确保nopCommerce源代码在同一目录级别" -ForegroundColor Yellow
}

Write-Host ""

# 清理之前的构建
Write-Host "清理之前的构建..." -ForegroundColor Yellow
if (Test-Path "bin") {
    Remove-Item -Recurse -Force "bin" -ErrorAction SilentlyContinue
}
if (Test-Path "obj") {
    Remove-Item -Recurse -Force "obj" -ErrorAction SilentlyContinue
}
Write-Host "清理完成" -ForegroundColor Green

Write-Host ""

# 恢复NuGet包
Write-Host "恢复NuGet包..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "包恢复成功" -ForegroundColor Green
} else {
    Write-Host "包恢复失败" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 构建项目
Write-Host "构建项目 (Debug)..." -ForegroundColor Yellow
dotnet build --configuration Debug
if ($LASTEXITCODE -eq 0) {
    Write-Host "构建成功" -ForegroundColor Green
} else {
    Write-Host "构建失败" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 运行测试
Write-Host "运行测试 (Debug)..." -ForegroundColor Yellow
dotnet test --configuration Debug --no-build
if ($LASTEXITCODE -eq 0) {
    Write-Host "测试通过" -ForegroundColor Green
} else {
    Write-Host "测试失败" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 显示构建结果
Write-Host "=== 构建完成 ===" -ForegroundColor Green
Write-Host "输出目录: bin\Debug" -ForegroundColor Cyan
Write-Host "插件DLL: bin\Debug\net9.0\Nop.Plugin.Api.dll" -ForegroundColor Cyan

Write-Host ""
Write-Host "下一步:" -ForegroundColor Yellow
Write-Host "1. 将插件DLL复制到nopCommerce的Plugins目录" -ForegroundColor White
Write-Host "2. 在nopCommerce管理面板中安装插件" -ForegroundColor White
Write-Host "3. 配置API设置和权限" -ForegroundColor White

Write-Host ""
Write-Host "构建脚本执行完成!" -ForegroundColor Green 