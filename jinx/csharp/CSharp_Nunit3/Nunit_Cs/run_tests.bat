@echo off
REM Nunit_Cs/run_tests.bat
REM 运行NUnit测试的批处理脚本

setlocal EnableDelayedExpansion

REM 设置默认参数
set "TEST_TYPE=UI"
set "VERBOSE=false"
set "DEBUG=false"
set "FILTER="
set "RERUN_FAILED=false"
set "WORKERS=1"
set "REPORT=TestResults"

REM 解析命令行参数
:parse_args
if "%~1"=="" goto run_tests
if /i "%~1"=="-t" (
    set "TEST_TYPE=%~2"
    shift
    shift
    goto parse_args
)
if /i "%~1"=="-v" (
    set "VERBOSE=true"
    shift
    goto parse_args
)
if /i "%~1"=="-d" (
    set "DEBUG=true"
    shift
    goto parse_args
)
if /i "%~1"=="-f" (
    set "FILTER=%~2"
    shift
    shift
    goto parse_args
)
if /i "%~1"=="-r" (
    set "RERUN_FAILED=true"
    shift
    goto parse_args
)
if /i "%~1"=="-w" (
    set "WORKERS=%~2"
    shift
    shift
    goto parse_args
)
if /i "%~1"=="-o" (
    set "REPORT=%~2"
    shift
    shift
    goto parse_args
)
shift
goto parse_args

:run_tests
echo.
echo =========================================================
echo             NUnit C# 测试运行工具
echo =========================================================

REM 创建结果目录
set "REPORTS_DIR=%~dp0Reports"
set "RESULTS_DIR=%REPORTS_DIR%\%REPORT%"
if not exist "%RESULTS_DIR%" mkdir "%RESULTS_DIR%"

REM 构建命令行参数
set "DOTNET_ARGS=test"

REM 添加runsettings文件
set "DOTNET_ARGS=%DOTNET_ARGS% --settings:NUnit.runsettings"

REM 添加结果目录
set "DOTNET_ARGS=%DOTNET_ARGS% --results-directory:%RESULTS_DIR%"

REM 添加测试过滤器
if not "%FILTER%"=="" (
    set "DOTNET_ARGS=%DOTNET_ARGS% --filter:%FILTER%"
) else (
    if /i "%TEST_TYPE%"=="UI" (
        set "DOTNET_ARGS=%DOTNET_ARGS% --filter:FullyQualifiedName~Nunit_Cs.TestCase.UI"
    ) else if /i "%TEST_TYPE%"=="API" (
        set "DOTNET_ARGS=%DOTNET_ARGS% --filter:FullyQualifiedName~Nunit_Cs.TestCase.API"
    )
)

REM 添加详细输出
if /i "%VERBOSE%"=="true" (
    set "DOTNET_ARGS=%DOTNET_ARGS% --verbosity:detailed"
)

REM 添加日志记录
set "DOTNET_ARGS=%DOTNET_ARGS% --logger:console;verbosity=normal"
set "DOTNET_ARGS=%DOTNET_ARGS% --logger:trx;LogFileName=testresults.trx"
set "DOTNET_ARGS=%DOTNET_ARGS% --logger:html;LogFileName=testresults.html"

REM 在调试模式下添加额外参数
if /i "%DEBUG%"=="true" (
    set "DOTNET_ARGS=%DOTNET_ARGS% --collect:"XPlat Code Coverage""
)

REM 输出将要执行的命令
echo 执行命令: dotnet %DOTNET_ARGS%
echo.
echo 开始执行测试...
echo.

REM 记录开始时间
set "START_TIME=%TIME%"

REM 执行测试
dotnet %DOTNET_ARGS%
set "EXIT_CODE=%ERRORLEVEL%"

REM 记录结束时间
set "END_TIME=%TIME%"

REM 重新运行失败的测试
if /i "%RERUN_FAILED%"=="true" if not "%EXIT_CODE%"=="0" (
    echo.
    echo 重新运行失败的测试...
    set "DOTNET_ARGS=%DOTNET_ARGS% --filter:TestOutcome!=Passed"
    dotnet %DOTNET_ARGS%
    set "EXIT_CODE=%ERRORLEVEL%"
)

REM 计算耗时（简化版本，不精确）
for /f "tokens=1-4 delims=:.," %%a in ("%START_TIME%") do (
    set /a "start=(((%%a*60)+1%%b %% 100)*60+1%%c %% 100)*100+1%%d %% 100"
)
for /f "tokens=1-4 delims=:.," %%a in ("%END_TIME%") do (
    set /a "end=(((%%a*60)+1%%b %% 100)*60+1%%c %% 100)*100+1%%d %% 100"
)
set /a elapsed=end-start
set /a hh=elapsed/(60*60*100), rest=elapsed%%(60*60*100), mm=rest/(60*100), rest%%=60*100, ss=rest/100, hs=rest%%100
if %hh% lss 10 set hh=0%hh%
if %mm% lss 10 set mm=0%mm%
if %ss% lss 10 set ss=0%ss%
if %hs% lss 10 set hs=0%hs%
set "DURATION=%hh%:%mm%:%ss%.%hs%"

REM 输出测试结果
echo.
echo =========================================================
if "%EXIT_CODE%"=="0" (
    echo 测试完成! 结果代码: %EXIT_CODE% - 成功
) else (
    echo 测试完成! 结果代码: %EXIT_CODE% - 失败
)
echo 总耗时: %DURATION%
echo 测试报告位置: %RESULTS_DIR%
echo =========================================================
echo.

REM 等待用户按键（如果是双击运行）
if not defined PROMPT pause

exit /b %EXIT_CODE% 