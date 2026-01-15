# Nunit_Cs/run_tests.ps1
# 运行NUnit测试的PowerShell脚本

param (
    [string]$TestType = "UI",  # 默认运行UI测试
    [switch]$Verbose = $false,  # 详细输出
    [switch]$Debug = $false,    # 调试模式
    [string]$Filter = "",       # 测试过滤条件
    [switch]$RerunFailed = $false, # 重新运行失败的测试
    [int]$Workers = 1,          # 并行工作线程数
    [string]$Report = "TestResults",  # 报告路径
    [switch]$CopyLogs = $true   # 将日志复制到项目Logs目录
)

# 脚本开始时间
$scriptStartTime = Get-Date

# 输出横幅
Write-Host "`n========================================================="
Write-Host "            NUnit C# 测试运行工具"
Write-Host "========================================================="
Write-Host "开始执行时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host "脚本运行目录: $PSScriptRoot" 
Write-Host "操作系统版本: $([System.Environment]::OSVersion.VersionString)"
Write-Host ".NET 版本: $([System.Environment]::Version)"
Write-Host "用户名: $([System.Environment]::UserName)"
Write-Host "计算机名: $([System.Environment]::MachineName)"
Write-Host "=========================================================`n"

# 创建结果目录
$reportsDir = Join-Path $PSScriptRoot "Reports"
$resultsDir = Join-Path $reportsDir $Report
$logDir = Join-Path $PSScriptRoot "Logs"
$binLogsDir = Join-Path $PSScriptRoot "bin\Debug\net8.0\Logs"
$testLogsDir = Join-Path $logDir "TestRuns"

# 确保所有目录存在
$dirsToCreate = @($resultsDir, $logDir, $testLogsDir, $binLogsDir)
foreach ($dir in $dirsToCreate) {
    if (-not (Test-Path $dir)) {
        try {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Write-Host "✓ 创建目录成功: $dir" -ForegroundColor Green
        }
        catch {
            Write-Host "✗ 创建目录失败: $dir - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# 日志文件路径
$timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
$logFile = Join-Path $testLogsDir "TestRun_$timestamp.log"
$summaryFile = Join-Path $testLogsDir "TestSummary_$timestamp.txt"
$todayLogFile = Join-Path $logDir (Get-Date -Format "yyyy-MM-dd.log")

# 记录测试运行标记到日期日志
$testRunMarker = @"
==================================================
开始执行测试: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
测试类型: $TestType
运行模式: $(if($Verbose){"详细"}else{"正常"})$(if($Debug){" (调试)"}else{""})
==================================================
"@

# 输出日志文件路径
Write-Host "`n日志文件将保存到:" -ForegroundColor Cyan
Write-Host "- 运行日志: $logFile" -ForegroundColor Cyan
Write-Host "- 摘要报告: $summaryFile" -ForegroundColor Cyan
Write-Host "- 日期日志: $todayLogFile" -ForegroundColor Cyan

# 确保日志文件目录存在并写入初始内容
try {
    # 写入到日期日志
    Add-Content -Path $todayLogFile -Value $testRunMarker -Encoding utf8
    
    # 初始化测试运行日志
    @"
======================================================
      NUnit C# 自动化测试运行日志
======================================================
运行时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
测试类型: $TestType
运行环境: $([System.Environment]::OSVersion.VersionString)
.NET环境: $([System.Environment]::Version)
用户名称: $([System.Environment]::UserName)
计算机名: $([System.Environment]::MachineName)
工作目录: $PSScriptRoot
======================================================

"@ | Out-File -FilePath $logFile -Encoding utf8
    Write-Host "✓ 日志初始化成功" -ForegroundColor Green
}
catch {
    Write-Host "✗ 日志初始化失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 函数: 将信息同时输出到控制台和日志文件
function Write-Log {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] $Message"
    
    # 输出到控制台
    Write-Host $logEntry -ForegroundColor $Color
    
    # 输出到日志文件
    Add-Content -Path $logFile -Value $logEntry -Encoding UTF8
    
    # 输出到日期日志
    Add-Content -Path $todayLogFile -Value $logEntry -Encoding UTF8
}

# 构建命令行参数
$dotnetArgs = @("test")

# 添加runsettings文件
$dotnetArgs += "--settings:NUnit.runsettings"

# 添加结果目录
$dotnetArgs += "--results-directory:$resultsDir"

# 添加测试过滤器
if ($Filter) {
    $dotnetArgs += "--filter:$Filter"
    Write-Log "使用自定义过滤器: $Filter" "Cyan"
}
elseif ($TestType -eq "UI") {
    $dotnetArgs += "--filter:FullyQualifiedName~Nunit_Cs.TestCase.UI"
    Write-Log "运行UI测试" "Cyan"
}
elseif ($TestType -eq "API") {
    $dotnetArgs += "--filter:FullyQualifiedName~Nunit_Cs.TestCase.API"
    Write-Log "运行API测试" "Cyan"
}

# 添加详细输出
if ($Verbose) {
    $dotnetArgs += "--verbosity:detailed"
    Write-Log "启用详细输出" "Cyan"
}

# 添加日志记录
$dotnetArgs += "--logger:console;verbosity=normal"
$dotnetArgs += "--logger:trx;LogFileName=testresults.trx"
$dotnetArgs += "--logger:html;LogFileName=testresults.html"

# 在调试模式下添加额外参数
if ($Debug) {
    $dotnetArgs += "--collect:""XPlat Code Coverage"""
    Write-Log "启用调试模式" "Cyan"
}

# 输出将要执行的命令
Write-Log "执行命令: dotnet $($dotnetArgs -join ' ')" "Cyan"
Write-Log "`n开始执行测试..." "Green"

# 执行测试
$startTime = Get-Date
$processOutput = & dotnet $dotnetArgs *>&1
$exitCode = $LASTEXITCODE
$endTime = Get-Date
$duration = New-TimeSpan -Start $startTime -End $endTime

# 记录过程输出到日志文件
$processOutput | ForEach-Object {
    $line = $_
    if ($line -match "fail|error|exception|Fail|Error|Exception") {
        Write-Host $line -ForegroundColor Red
    } elseif ($line -match "pass|success|Passed|Success") {
        Write-Host $line -ForegroundColor Green
    } else {
        Write-Host $line
    }
    Add-Content -Path $logFile -Value $line -Encoding UTF8
}

# 重新运行失败的测试
if ($RerunFailed -and $exitCode -ne 0) {
    Write-Log "`n重新运行失败的测试..." "Yellow"
    $dotnetArgs += "--filter:TestOutcome!=Passed"
    $rerunOutput = & dotnet $dotnetArgs *>&1
    $exitCode = $LASTEXITCODE
    
    # 记录重新运行的输出到日志文件
    $rerunOutput | ForEach-Object {
        $line = $_
        if ($line -match "fail|error|exception|Fail|Error|Exception") {
            Write-Host $line -ForegroundColor Red
        } elseif ($line -match "pass|success|Passed|Success") {
            Write-Host $line -ForegroundColor Green
        } else {
            Write-Host $line
        }
        Add-Content -Path $logFile -Value $line -Encoding UTF8
    }
}

# 计算总运行时间
$totalDuration = New-TimeSpan -Start $scriptStartTime -End (Get-Date)

# 输出测试结果
Write-Log "`n=========================================================" "White"

if ($exitCode -eq 0) {
    Write-Log "测试完成! 结果代码: $exitCode" "Green"
} else {
    Write-Log "测试完成! 结果代码: $exitCode" "Red"
}

Write-Log "测试耗时: $($duration.TotalSeconds.ToString('0.00')) 秒" "White"
Write-Log "总耗时: $($totalDuration.TotalSeconds.ToString('0.00')) 秒" "White"
Write-Log "测试报告位置: $resultsDir" "White"
Write-Log "日志文件位置: $logFile" "White"
Write-Log "======================================================" "White"

# 检查生成的日志文件
$allLogDirs = @($logDir, $binLogsDir)

foreach ($dir in $allLogDirs) {
    if (Test-Path $dir) {
        $testLogFiles = Get-ChildItem -Path $dir -Filter "*.log" -Recurse | Sort-Object LastWriteTime -Descending
        if ($testLogFiles.Count -gt 0) {
            Write-Log "在 $dir 中找到的日志文件:" "Cyan"
            foreach ($logFile in $testLogFiles | Select-Object -First 5) {
                Write-Log "- $($logFile.Name) ($(Get-Date $logFile.LastWriteTime -Format 'yyyy-MM-dd HH:mm:ss'))" "Yellow"
            }
        } else {
            Write-Log "在 $dir 中未找到日志文件" "Yellow"
        }
    } else {
        Write-Log "日志目录不存在: $dir" "Red"
    }
}

# 如果二进制输出目录中有日志，但项目根目录没有，则复制
if ($CopyLogs -and (Test-Path $binLogsDir)) {
    $binLogs = Get-ChildItem -Path $binLogsDir -Filter "*.log" -Recurse
    if ($binLogs.Count -gt 0) {
        Write-Log "复制日志文件从bin目录到项目Logs目录..." "Cyan"
        foreach ($log in $binLogs) {
            $destPath = Join-Path $logDir $log.Name
            if (-not (Test-Path $destPath) -or ((Get-Item $log).LastWriteTime -gt (Get-Item $destPath).LastWriteTime)) {
                try {
                    Copy-Item -Path $log.FullName -Destination $destPath -Force
                    Write-Log "✓ 已复制: $($log.Name)" "Green"
                }
                catch {
                    Write-Log "✗ 复制失败: $($log.Name) - $($_.Exception.Message)" "Red"
                }
            }
        }
    }
}

# 检查TestLogs目录
$testLogsPath = Join-Path $PSScriptRoot "bin\Debug\net8.0\Logs\TestLogs"
if (Test-Path $testLogsPath) {
    $individualTestLogs = Get-ChildItem -Path $testLogsPath -Filter "Test_*.log" | Sort-Object LastWriteTime -Descending
    if ($individualTestLogs.Count -gt 0) {
        Write-Log "找到单个测试日志文件:" "Cyan"
        foreach ($testLog in $individualTestLogs | Select-Object -First 5) {
            Write-Log "- $($testLog.Name) ($(Get-Date $testLog.LastWriteTime -Format 'yyyy-MM-dd HH:mm:ss'))" "Yellow"
        }
        
        # 复制单个测试日志
        $projectTestLogsDir = Join-Path $logDir "TestLogs"
        if (-not (Test-Path $projectTestLogsDir)) {
            New-Item -ItemType Directory -Path $projectTestLogsDir -Force | Out-Null
        }
        
        Write-Log "复制测试日志到: $projectTestLogsDir" "Cyan"
        foreach ($testLog in $individualTestLogs) {
            $destPath = Join-Path $projectTestLogsDir $testLog.Name
            try {
                Copy-Item -Path $testLog.FullName -Destination $destPath -Force
            }
            catch {
                Write-Log "✗ 复制测试日志失败: $($testLog.Name)" "Red"
            }
        }
    }
}

# 生成摘要文件
@"
======================================================
      NUnit C# 自动化测试运行摘要
======================================================
开始时间: $([String]::Format("{0:yyyy-MM-dd HH:mm:ss}", $scriptStartTime))
结束时间: $([String]::Format("{0:yyyy-MM-dd HH:mm:ss}", (Get-Date)))
测试耗时: $($duration.TotalSeconds.ToString('0.00')) 秒
总耗时: $($totalDuration.TotalSeconds.ToString('0.00')) 秒
测试类型: $TestType
结果代码: $exitCode
测试报告: $resultsDir
日志文件: $logFile
======================================================
"@ | Out-File -FilePath $summaryFile -Encoding utf8

# 创建测试结果备份链接
$testResultsLink = Join-Path $logDir "最新测试报告.html"
$htmlReport = Join-Path $resultsDir "testresults.html"
if (Test-Path $htmlReport) {
    Write-Host "HTML测试报告已生成: $htmlReport" -ForegroundColor Green
    
    # 复制最新报告到Logs目录
    try {
        Copy-Item -Path $htmlReport -Destination (Join-Path $logDir "testresults_$timestamp.html") -Force
        Write-Host "✓ 已备份测试报告到 Logs 目录" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ 备份测试报告失败: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # 如果是在Windows环境中运行，可以自动打开报告
    if ($IsWindows -or (-not $IsLinux -and -not $IsMacOS)) {
        Write-Host "正在打开测试报告..." -ForegroundColor Cyan
        try {
            Invoke-Item $htmlReport -ErrorAction SilentlyContinue
        } catch {
            Write-Host "无法自动打开报告，请手动打开: $htmlReport" -ForegroundColor Yellow
        }
    }
}

# 记录测试完成标记到日期日志
$testEndMarker = @"
==================================================
测试执行完成: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
执行结果: $(if($exitCode -eq 0){"成功"}else{"失败"})
总耗时: $($totalDuration.TotalSeconds.ToString('0.00')) 秒
==================================================
"@
Add-Content -Path $todayLogFile -Value $testEndMarker -Encoding utf8

Exit $exitCode 