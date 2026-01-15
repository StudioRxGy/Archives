# 测试分类标记演示脚本
# 演示如何使用不同的过滤器执行测试

Write-Host "=== 企业自动化测试框架 - 测试分类标记演示 ===" -ForegroundColor Green
Write-Host ""

# 项目路径
$TestProject = "src/Tests/EnterpriseAutomationFramework.Tests.csproj"

Write-Host "1. 执行所有单元测试" -ForegroundColor Yellow
Write-Host "命令: dotnet test $TestProject --filter `"Type=Unit`"" -ForegroundColor Gray
dotnet test $TestProject --filter "Type=Unit" --verbosity minimal
Write-Host ""

Write-Host "2. 执行核心功能测试" -ForegroundColor Yellow
Write-Host "命令: dotnet test $TestProject --filter `"Category=Core`"" -ForegroundColor Gray
dotnet test $TestProject --filter "Category=Core" --verbosity minimal
Write-Host ""

Write-Host "3. 执行快速测试" -ForegroundColor Yellow
Write-Host "命令: dotnet test $TestProject --filter `"Speed=Fast`"" -ForegroundColor Gray
dotnet test $TestProject --filter "Speed=Fast" --verbosity minimal
Write-Host ""

Write-Host "4. 执行高优先级测试" -ForegroundColor Yellow
Write-Host "命令: dotnet test $TestProject --filter `"Priority=High`"" -ForegroundColor Gray
dotnet test $TestProject --filter "Priority=High" --verbosity minimal
Write-Host ""

Write-Host "5. 执行过滤器相关测试" -ForegroundColor Yellow
Write-Host "命令: dotnet test $TestProject --filter `"Tag=Filter`"" -ForegroundColor Gray
dotnet test $TestProject --filter "Tag=Filter" --verbosity minimal
Write-Host ""

Write-Host "6. 组合过滤器：单元测试且核心功能" -ForegroundColor Yellow
Write-Host "命令: dotnet test $TestProject --filter `"Type=Unit&Category=Core`"" -ForegroundColor Gray
dotnet test $TestProject --filter "Type=Unit&Category=Core" --verbosity minimal
Write-Host ""

Write-Host "7. 或条件过滤器：属性测试或过滤器测试" -ForegroundColor Yellow
Write-Host "命令: dotnet test $TestProject --filter `"(Tag=Attributes|Tag=Filter)`"" -ForegroundColor Gray
dotnet test $TestProject --filter "(Tag=Attributes|Tag=Filter)" --verbosity minimal
Write-Host ""

Write-Host "=== 演示完成 ===" -ForegroundColor Green
Write-Host ""
Write-Host "可用的测试分类标记：" -ForegroundColor Cyan
Write-Host "- Type: Unit, UI, API, Integration, E2E, Performance" -ForegroundColor White
Write-Host "- Category: Core, PageObject, Flow, Service, Configuration, Data, Browser, ApiClient, ErrorRecovery, Retry, Logging, Reporting, Fixture, DataDriven, Search, UserInterface, BusinessLogic" -ForegroundColor White
Write-Host "- Priority: Low, Medium, High, Critical" -ForegroundColor White
Write-Host "- Speed: Fast, Slow" -ForegroundColor White
Write-Host "- Suite: Smoke, Regression" -ForegroundColor White
Write-Host "- Tag: 自定义标签" -ForegroundColor White
Write-Host ""
Write-Host "更多信息请参考: docs/TestCategoryGuide.md" -ForegroundColor Cyan